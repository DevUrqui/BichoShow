using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BichoShow
{
    /// <summary>
    /// El concurso entero: encadena las pruebas, reparte estrellas a cada jugador, decide
    /// quién sabotea la siguiente ronda (el último en estrellas) y corona al ganador.
    /// Vive en la escena del menú y sobrevive a los cambios de escena.
    ///
    /// Estrellas por ronda: equipo ganador 3, perdedor 1, empate 2. El saboteador no juega
    /// con su equipo: gana estrellas según sus puntos de sabotaje. La final vale el doble.
    /// </summary>
    public class ShowBicho : MonoBehaviour
    {
        [System.Serializable]
        public class Prueba
        {
            public string escena = "Mudanza";
            public string nombre = "Mudanza frágil";
            [Tooltip("Las estrellas de esta ronda se multiplican por esto (la final vale doble).")]
            public int multiplicador = 1;
            [Tooltip("Va siempre al final del show (no entra en el sorteo).")]
            public bool esFinal;
        }

        public enum EstadoShow { Inactivo, Cargando, Presentando, EnRonda, Resultado, Marcador, Final }

        public static ShowBicho Instancia { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos() => Instancia = null;

        [Tooltip("Todas las pruebas posibles. Cada show elige unas cuantas al azar y acaba con una de las finales.")]
        public Prueba[] pruebas = PruebasPorDefecto();
        [Tooltip("Pruebas (sin contar la final) que se juegan en cada show.")]
        public int pruebasPorShow = 3;

        /// <summary>Las pruebas de este show, en orden (las elige EmpezarShow).</summary>
        readonly List<Prueba> programa = new List<Prueba>();
        public string escenaMenu = "Menu";
        [Tooltip("Segundos del cartel de presentación antes de cada ronda (al menos 4,5: hay que leer de qué va).")]
        public float duracionPresentacion = 4.5f;

        float DuracionPresentacion => Mathf.Max(duracionPresentacion, 4.5f);
        [Tooltip("Segundos que se ve el resultado de la ronda antes del marcador.")]
        public float duracionResultado = 3.5f;

        public EstadoShow Estado { get; private set; } = EstadoShow.Inactivo;
        public int RondaActual { get; private set; }

        /// <summary>El show tapa la pantalla (marcador, podio…): la ronda no debe dibujar la suya encima.</summary>
        public bool TapaLaPantalla => Estado == EstadoShow.Marcador || Estado == EstadoShow.Final || Estado == EstadoShow.Cargando;

        // Opciones del show (las pone el menú)
        bool conBots = true;
        float habilidad = 0.55f;
        float duracion = 90f;

        readonly int[] estrellas = new int[4];
        readonly int[] estrellasRonda = new int[4];
        readonly bool[] participa = new bool[4];
        readonly bool[] esHumano = new bool[4];
        int saboteadorSiguiente = -1;
        string resumenSabotaje = "";
        RondaBase ronda;
        float inicioEstado;

        void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject); // ya hay un show vivo (volvemos al menú)
                return;
            }
            Instancia = this;
            DontDestroyOnLoad(gameObject);
            AnadirPruebasNuevas();
        }

        /// <summary>
        /// La lista de pruebas se guarda en la escena del menú: las que se han añadido después
        /// (y están en el código) se suman aquí, sin tener que rehacer el menú.
        /// </summary>
        void AnadirPruebasNuevas()
        {
            var lista = new List<Prueba>(pruebas ?? System.Array.Empty<Prueba>());
            foreach (var p in PruebasPorDefecto())
                if (!lista.Exists(x => x.escena == p.escena)) lista.Add(p);
            pruebas = lista.ToArray();
        }

        /// <summary>Todas las pruebas que hay en el juego (instancias nuevas cada vez).</summary>
        static Prueba[] PruebasPorDefecto() => new[]
        {
            new Prueba { escena = "Mudanza", nombre = "Mudanza frágil" },
            new Prueba { escena = "Patata", nombre = "Patata caliente" },
            new Prueba { escena = "Corona", nombre = "La Corona" },
            new Prueba { escena = "Lluvia", nombre = "Lluvia de muebles" },
            new Prueba { escena = "Pintura", nombre = "Pintabichos" },
            new Prueba { escena = "Costura", nombre = "Costura a la fuga" },
            new Prueba { escena = "Despensa", nombre = "Despensa Deslizante" },
            new Prueba { escena = "Voley", nombre = "Voley atados" },
            new Prueba { escena = "Cortacesped", nombre = "El Cortacésped Mecha" },
            new Prueba { escena = "Final", nombre = "LA GRAN FINAL: Juntos o nada", multiplicador = 2, esFinal = true },
            new Prueba { escena = "Cumbre", nombre = "LA GRAN FINAL: La Cumbre del Girasol", multiplicador = 2, esFinal = true },
        };

        void OnEnable()
        {
            SceneManager.sceneLoaded += AlCargarEscena;
            RondaBase.AlTerminarAlguna += AlTerminarRonda;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            RondaBase.AlTerminarAlguna -= AlTerminarRonda;
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        // ---------------------------------------------------------------------
        // Flujo
        // ---------------------------------------------------------------------

        public void EmpezarShow(bool bots, float habilidadBots, float duracionRondas)
        {
            conBots = bots;
            habilidad = habilidadBots;
            duracion = duracionRondas;
            for (int i = 0; i < 4; i++) { estrellas[i] = 0; estrellasRonda[i] = 0; }
            ElegirPrograma();
            if (programa.Count == 0)
            {
                Debug.LogError("Bicho Show: no hay ninguna prueba en la lista del build. Crea las escenas desde el menú BichoShow.");
                return;
            }
            Estadisticas.Texto($"EMPIEZA EL SHOW · bots: {(bots ? "sí" : "no")} · habilidad de los bots: {habilidadBots:0.00} · rondas de {duracionRondas:0} s · humanos: {(GestorJugadores.Instancia != null ? GestorJugadores.Instancia.NumeroHumanos : PlayerInput.all.Count)}{(RedShow.EnLinea ? " (online)" : "")} · " +
                               $"pruebas: {string.Join(", ", programa.Select(p => p.nombre))}");
            saboteadorSiguiente = -1;
            RondaActual = 0;
            CargarRonda();
        }

        /// <summary>
        /// Sortea las pruebas de este show (solo las que estén en el build, sin repetir) y pone
        /// una final a sorteo al acabar. Así cada partida es distinta.
        /// </summary>
        void ElegirPrograma()
        {
            programa.Clear();
            var bolsa = pruebas.Where(p => !p.esFinal && Application.CanStreamedLevelBeLoaded(p.escena)).ToList();
            for (int i = 0; i < pruebasPorShow && bolsa.Count > 0; i++)
            {
                int k = Random.Range(0, bolsa.Count);
                programa.Add(bolsa[k]);
                bolsa.RemoveAt(k);
            }
            // Una sola gran final, a sorteo entre las que haya.
            var finales = pruebas.Where(p => p.esFinal && Application.CanStreamedLevelBeLoaded(p.escena)).ToList();
            if (finales.Count > 0) programa.Add(finales[Random.Range(0, finales.Count)]);
        }

        void CargarRonda()
        {
            var gestor = GestorJugadores.Instancia;
            if (gestor != null)
            {
                gestor.PrepararCambioEscena();
                gestor.PermitirUnirse(false);
            }
            Estado = EstadoShow.Cargando;
            ControladorBicho.EntradaBloqueada = true;
            SceneManager.LoadScene(programa[RondaActual].escena);
        }

        void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (Estado != EstadoShow.Cargando) return;

            ronda = FindAnyObjectByType<RondaBase>();
            if (ronda == null)
            {
                Debug.LogError($"Bicho Show: la escena '{escena.name}' no tiene ninguna ronda. Vuelvo al menú.");
                VolverAlMenu();
                return;
            }

            var forzados = new List<int>();
            if (saboteadorSiguiente >= 0) forzados.Add(saboteadorSiguiente);
            ronda.ConfigurarDesdeShow(conBots, habilidad, duracion, forzados);

            Estado = EstadoShow.Presentando;
            inicioEstado = Time.time;
            Sonidos.Reproducir(Sonido.Logro);
        }

        void Update()
        {
            switch (Estado)
            {
                case EstadoShow.Presentando:
                    if (Time.time - inicioEstado > DuracionPresentacion && ronda != null)
                    {
                        ControladorBicho.EntradaBloqueada = false;
                        ronda.IniciarDesdeShow();
                        Estado = EstadoShow.EnRonda;
                    }
                    break;

                case EstadoShow.Resultado:
                    if (Time.time - inicioEstado > duracionResultado)
                    {
                        Estado = RondaActual + 1 < programa.Count ? EstadoShow.Marcador : EstadoShow.Final;
                        inicioEstado = Time.time;
                        ControladorBicho.EntradaBloqueada = true;
                        if (Estado == EstadoShow.Final)
                        {
                            Sonidos.Reproducir(Sonido.Victoria);
                            Sonidos.Reproducir(Sonido.Aplausos);
                        }
                        else
                        {
                            Sonidos.Reproducir(Sonido.Logro);
                        }
                    }
                    break;

                case EstadoShow.Marcador:
                    if (Time.time - inicioEstado > 1f && AlguienConfirma())
                    {
                        Sonidos.Reproducir(Sonido.MenuAceptar);
                        RondaActual++;
                        CargarRonda();
                    }
                    break;

                case EstadoShow.Final:
                    if (Time.time - inicioEstado > 2f && AlguienConfirma())
                    {
                        Sonidos.Reproducir(Sonido.MenuAceptar);
                        VolverAlMenu();
                    }
                    break;
            }
        }

        static bool AlguienConfirma()
        {
            // Los jugadores de una partida online también pueden darle a seguir.
            if (EntradaRemota.AlgunoPulso(EntradaRemota.Boton.Saltar) || EntradaRemota.AlgunoPulso(EntradaRemota.Boton.Empezar))
                return true;
            foreach (var entrada in PlayerInput.all)
            {
                if (entrada.actions == null) continue;
                var saltar = entrada.actions.FindAction("Saltar");
                var empezar = entrada.actions.FindAction("Empezar");
                if ((saltar != null && saltar.WasPressedThisFrame()) || (empezar != null && empezar.WasPressedThisFrame()))
                    return true;
            }
            return false;
        }

        public void VolverAlMenu()
        {
            Estado = EstadoShow.Inactivo;
            ronda = null;
            var gestor = GestorJugadores.Instancia;
            if (gestor != null) gestor.PrepararCambioEscena();
            ControladorBicho.EntradaBloqueada = false;
            SceneManager.LoadScene(escenaMenu);
        }

        // ---------------------------------------------------------------------
        // Estrellas
        // ---------------------------------------------------------------------

        void AlTerminarRonda(RondaBase terminada)
        {
            if (terminada != ronda || Estado != EstadoShow.EnRonda) return;

            ActualizarParticipantes();
            int mult = Mathf.Max(1, programa[RondaActual].multiplicador);
            int p0 = ronda.PuntosEquipo(0), p1 = ronda.PuntosEquipo(1);
            int ganador = p0 == p1 ? -1 : p0 > p1 ? 0 : 1;

            // Saboteadores humanos de esta ronda (no juegan con su equipo).
            var sabotaje = new Dictionary<int, int>();
            foreach (var s in ronda.Saboteadores)
                if (s != null && s.IndiceJugador >= 0) sabotaje[s.IndiceJugador] = s.Puntos;

            resumenSabotaje = "";
            for (int i = 0; i < 4; i++)
            {
                estrellasRonda[i] = 0;
                if (!participa[i]) continue;

                int ganadas;
                if (sabotaje.TryGetValue(i, out int puntosSabotaje))
                {
                    ganadas = puntosSabotaje >= 60 ? 3 : puntosSabotaje >= 25 ? 2 : puntosSabotaje > 0 ? 1 : 0;
                    resumenSabotaje = $"{NombreDe(i)} saboteó: {puntosSabotaje} puntos";
                }
                else if (ronda.Individual)
                {
                    // Todos contra todos: 3 estrellas el primero, 2 el segundo, 1 el tercero (empates: las mismas).
                    int mejores = 0;
                    for (int j = 0; j < 4; j++)
                        if (j != i && participa[j] && !sabotaje.ContainsKey(j) && ronda.PuntosJugador(j) > ronda.PuntosJugador(i))
                            mejores++;
                    ganadas = Mathf.Max(0, 3 - mejores);
                }
                else
                {
                    ganadas = ganador < 0 ? 2 : (i % 2 == ganador ? 3 : 1);
                }

                estrellasRonda[i] = ganadas * mult;
                estrellas[i] += estrellasRonda[i];
            }

            saboteadorSiguiente = ElegirSaboteadorSiguiente();
            var tabla = new List<string>();
            for (int i = 0; i < 4; i++) if (participa[i]) tabla.Add($"{NombreDe(i)} +{estrellasRonda[i]} (total {estrellas[i]})");
            Estadisticas.Texto($"Estrellas tras {programa[RondaActual].nombre}: {string.Join(" · ", tabla)}" + (string.IsNullOrEmpty(resumenSabotaje) ? "" : $" · {resumenSabotaje}"));
            Estado = EstadoShow.Resultado;
            inicioEstado = Time.time;
        }

        /// <summary>Quién está en el show: los humanos y, si hay bots, los huecos hasta 4.</summary>
        void ActualizarParticipantes()
        {
            for (int i = 0; i < 4; i++) { esHumano[i] = false; participa[i] = conBots; }
            foreach (var entrada in PlayerInput.all)
            {
                var bicho = entrada.GetComponent<ControladorBicho>();
                int i = bicho != null ? bicho.IndiceJugador : entrada.playerIndex;
                if (i < 0 || i >= 4) continue;
                esHumano[i] = true;
                participa[i] = true;
            }
            if (GestorJugadores.Instancia != null)
                foreach (var remoto in GestorJugadores.Instancia.Remotos)
                {
                    if (remoto == null) continue;
                    int i = remoto.IndiceJugador;
                    if (i < 0 || i >= 4) continue;
                    esHumano[i] = true;
                    participa[i] = true;
                }
        }

        /// <summary>
        /// El humano con menos estrellas sabotea la siguiente ronda (si hay al menos dos humanos;
        /// con uno solo, nunca le quitamos el bicho). Empates: el que menos ganó esta ronda.
        /// </summary>
        int ElegirSaboteadorSiguiente()
        {
            if (RondaActual + 1 >= programa.Count) return -1;
            if (RolSabotaje(programa[RondaActual + 1].escena) == null) return -1; // esa prueba no se puede sabotear
            var humanos = Enumerable.Range(0, 4).Where(i => esHumano[i]).ToList();
            if (humanos.Count < 2) return -1;
            return humanos.OrderBy(i => estrellas[i]).ThenBy(i => estrellasRonda[i]).ThenBy(_ => Random.value).First();
        }

        /// <summary>
        /// Qué hace el que va último en esa prueba: "SABOTEADOR" (activa trampas), "VILLANO" (maneja
        /// al malo de la prueba) o null si no se puede sabotear (entonces juega normal).
        /// </summary>
        public static string RolSabotaje(string escena) => escena switch
        {
            "Mudanza" or "Costura" or "Final" or "Arena" => "SABOTEADOR",
            "Cortacesped" => "VILLANO",
            _ => null,
        };

        string RolSiguiente => RondaActual + 1 < programa.Count ? RolSabotaje(programa[RondaActual + 1].escena) ?? "SABOTEADOR" : "SABOTEADOR";

        /// <summary>El nombre que ha escrito el jugador (o J1, J2…); los bots, "Bot N".</summary>
        string NombreDe(int i)
        {
            if (!esHumano[i]) return $"Bot {i + 1}";
            foreach (var b in ControladorBicho.Todos)
                if (b != null && b.IndiceJugador == i && !b.EsBot) return NombresJugadores.De(b);
            return $"J{i + 1}";
        }

        Color ColorDe(int i) => GestorJugadores.Instancia != null ? GestorJugadores.Instancia.ColorDe(i) : Color.white;

        // ---------------------------------------------------------------------
        // Pantallas del show
        // ---------------------------------------------------------------------

        void OnGUI()
        {
            if (Estado == EstadoShow.Inactivo || Estado == EstadoShow.EnRonda || Estado == EstadoShow.Resultado) return;
            float w = Screen.width, h = Screen.height;

            switch (Estado)
            {
                case EstadoShow.Cargando:
                case EstadoShow.Presentando:
                    DibujarPresentacion(w, h);
                    break;

                case EstadoShow.Marcador:
                {
                    EstiloShow.FondoPlato(0.95f);
                    EstiloShow.Confeti(24, 0.5f);
                    EstiloShow.TituloRebote(new Rect(0, h * 0.02f, w, Px(130f)), "MARCADOR", Tam(110f),
                                            new[] { EstiloShow.Oro, EstiloShow.Rosa, EstiloShow.Cian, EstiloShow.Verde, EstiloShow.Lila }, 0.7f);
                    var cinta = new Rect(w * 0.5f - Px(240f), h * 0.02f + Px(130f), Px(480f), Px(52f));
                    EstiloShow.Panel(cinta, EstiloShow.Rosa, cinta.height * 0.5f);
                    EstiloShow.Texto(cinta, $"TRAS LA RONDA {RondaActual + 1} DE {programa.Count}", Tam(30f), Color.white);
                    DibujarPuntosRonda(w * 0.5f, cinta.yMax + Px(26f), RondaActual);
                    DibujarTabla(w, h, h * 0.29f);

                    // La siguiente prueba, en una tarjeta.
                    var siguiente = programa[RondaActual + 1];
                    var cartel = new Rect(w * 0.5f - Px(600f), h - Px(250f), Px(1200f), Px(78f));
                    EstiloShow.Panel(cartel, EstiloShow.MoradoOscuro.WithAlpha(0.92f), cartel.height * 0.5f);
                    var etiqueta = new Rect(cartel.x + Px(14f), cartel.y + Px(12f), Px(210f), cartel.height - Px(24f));
                    EstiloShow.Panel(etiqueta, EstiloShow.Cian, etiqueta.height * 0.5f, -1f, false);
                    EstiloShow.Texto(etiqueta, "SIGUIENTE", Tam(28f), Color.white);
                    bool multiplica = siguiente.multiplicador > 1;
                    EstiloShow.Texto(new Rect(etiqueta.xMax + Px(16f), cartel.y, cartel.width - etiqueta.width - Px(multiplica ? 290f : 50f), cartel.height),
                                     siguiente.nombre, Tam(38f), Color.white);
                    if (multiplica)
                    {
                        var chip = new Rect(cartel.xMax - Px(250f), cartel.y + Px(12f), Px(236f), cartel.height - Px(24f));
                        EstiloShow.Panel(chip, EstiloShow.Oro, chip.height * 0.5f, -1f, false);
                        EstiloShow.Texto(chip, $"¡ESTRELLAS x{siguiente.multiplicador}!", Tam(26f), Color.white);
                    }
                    if (saboteadorSiguiente >= 0)
                    {
                        string texto = $"{NombreDe(saboteadorSiguiente)} va último y será {RolSiguiente}";
                        float ancho = EstiloShow.Ancho(texto, Tam(30f)) + Px(60f);
                        var pildora = new Rect(w * 0.5f - ancho * 0.5f, h - Px(162f), ancho, Px(50f));
                        EstiloShow.Panel(pildora, new Color(0.45f, 0.2f, 0.7f, 0.95f), pildora.height * 0.5f, -1f, false);
                        EstiloShow.Texto(pildora, texto, Tam(30f), EstiloShow.Lila);
                    }
                    if (Time.time - inicioEstado > 1f)
                        EstiloShow.Ayuda(new Rect(0, h - Px(98f), w, Px(54f)), ("A", EstiloShow.GlifoA, "seguir"));
                    break;
                }

                case EstadoShow.Final:
                {
                    int ganador = Enumerable.Range(0, 4).Where(i => participa[i]).OrderByDescending(i => estrellas[i]).FirstOrDefault();
                    bool empate = Enumerable.Range(0, 4).Count(i => participa[i] && estrellas[i] == estrellas[ganador]) > 1;
                    Color colorGanador = empate ? EstiloShow.Oro : ColorDe(ganador);
                    EstiloShow.FondoPlato(0.95f, new Vector2(w * 0.5f, h * 0.13f), colorGanador);
                    EstiloShow.Confeti(80, 0.9f);

                    string titulo = empate ? "¡EMPATE EN LO MÁS ALTO!" : $"¡GANA {NombreDe(ganador).ToUpper()}!";
                    EstiloShow.TituloRebote(new Rect(0, h * 0.03f, w, Px(170f)), titulo, Ajustar(titulo, Tam(140f), w * 0.8f),
                                            new[] { colorGanador, EstiloShow.Oro, Color.white });
                    // Estrellas girando a los lados del título
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float tamano = Px(110f) * (1f + Mathf.Sin(Time.unscaledTime * 4f + s) * 0.08f);
                        var r = new Rect(w * 0.5f + s * Mathf.Min(Px(700f), w * 0.44f) - tamano * 0.5f, h * 0.03f + Px(85f) - tamano * 0.5f, tamano, tamano);
                        EstiloShow.Estrella(r, EstiloShow.Oro, Time.unscaledTime * 40f * s);
                    }
                    EstiloShow.Texto(new Rect(0, h * 0.03f + Px(165f), w, Px(50f)), "¡FIN DEL BICHO SHOW!", Tam(40f), Color.white);
                    DibujarTabla(w, h, h * 0.29f);
                    if (Time.time - inicioEstado > 2f)
                        EstiloShow.Ayuda(new Rect(0, h - Px(98f), w, Px(54f)), ("A", EstiloShow.GlifoA, "volver al menú"));
                    break;
                }
            }
        }

        /// <summary>Un punto por prueba del show: las jugadas en verde, la actual dorada y latiendo; la final, una estrella.</summary>
        void DibujarPuntosRonda(float centroX, float centroY, int actual)
        {
            float punto = Px(26f), separacion = Px(40f);
            float px0 = centroX - (programa.Count - 1) * separacion * 0.5f;
            for (int i = 0; i < programa.Count; i++)
            {
                var c = new Vector2(px0 + i * separacion, centroY);
                bool esta = i == actual;
                float lado = esta ? punto * (1.25f + Mathf.Sin(Time.unscaledTime * 6f) * 0.08f) : punto;
                var r = new Rect(c.x - lado * 0.5f, c.y - lado * 0.5f, lado, lado);
                if (programa[i].esFinal)
                    EstiloShow.Estrella(EstiloShow.Escalar(r, 1.3f), esta || i < actual ? EstiloShow.Oro : EstiloShow.Morado, esta ? Time.unscaledTime * 80f : 0f);
                else
                    EstiloShow.Panel(r, i < actual ? EstiloShow.Verde : esta ? EstiloShow.Oro : EstiloShow.Morado, lado * 0.5f, Px(2f), false);
            }
        }

        /// <summary>
        /// Cartel de la siguiente prueba mientras carga y se presenta la ronda: qué ronda es (con un
        /// punto por prueba del show), el nombre, el tipo de prueba, de qué va y un consejo, las
        /// estrellas extra y el saboteador, y una barra que se llena. Con confeti cayendo.
        /// </summary>
        void DibujarPresentacion(float w, float h)
        {
            EstiloShow.FondoPlato(0.97f);
            EstiloShow.Confeti(46, 0.75f);
            var prueba = programa[RondaActual];

            // Cinta de la ronda y un punto por prueba del show (las jugadas en verde; la final, estrella).
            var cinta = new Rect(w * 0.5f - Px(260f), h * 0.075f, Px(520f), Px(74f));
            EstiloShow.Panel(cinta, prueba.esFinal ? EstiloShow.Oro : EstiloShow.Rosa, cinta.height * 0.5f);
            EstiloShow.Texto(cinta, prueba.esFinal ? "¡LA GRAN FINAL!" : $"RONDA {RondaActual + 1} DE {programa.Count}", Tam(44f), Color.white);
            DibujarPuntosRonda(w * 0.5f, cinta.yMax + Px(30f), RondaActual);

            // El nombre de la prueba.
            string nombre = prueba.nombre.ToUpper();
            float yTitulo = cinta.yMax + Px(56f);
            EstiloShow.TituloRebote(new Rect(0, yTitulo, w, Px(170f)), nombre, Ajustar(nombre, Tam(132f), w * 0.9f),
                                    new[] { Color.white, EstiloShow.Oro, EstiloShow.Cian, EstiloShow.Rosa });
            float y = yTitulo + Px(176f);

            // Tipo de prueba (en cuanto está cargada).
            if (ronda != null)
            {
                string tipo = ronda is RondaRuta ? "CARRERA" : ronda.Individual ? "TODOS CONTRA TODOS" : prueba.esFinal ? "POR PAREJAS" : "POR EQUIPOS";
                float anchoTipo = Px(36f) + EstiloShow.Ancho(tipo, Tam(28f));
                var chip = new Rect(w * 0.5f - anchoTipo * 0.5f - Px(18f), y, anchoTipo + Px(36f), Px(46f));
                EstiloShow.Panel(chip, EstiloShow.Cian, chip.height * 0.5f, -1f, false);
                EstiloShow.Texto(chip, tipo, Tam(28f), Color.white);
            }
            y += Px(62f);

            // Tarjeta: de qué va y un consejo.
            var tarjeta = new Rect(w * 0.5f - Px(640f), y, Px(1280f), Px(226f));
            EstiloShow.Panel(tarjeta, EstiloShow.MoradoOscuro.WithAlpha(0.88f), Px(34f));
            if (ronda != null)
            {
                EstiloShow.Texto(new Rect(tarjeta.x + Px(40f), tarjeta.y + Px(14f), tarjeta.width - Px(80f), Px(128f)), ronda.TextoDescripcion, Tam(29f),
                                 Color.white, TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto, true);
                string consejo = ronda.TextoConsejo;
                if (!string.IsNullOrEmpty(consejo))
                {
                    var etiqueta = new Rect(tarjeta.x + Px(40f), tarjeta.yMax - Px(64f), Px(170f), Px(46f));
                    EstiloShow.Panel(etiqueta, EstiloShow.Oro, etiqueta.height * 0.5f, -1f, false);
                    EstiloShow.Texto(etiqueta, "CONSEJO", Tam(26f), Color.white);
                    EstiloShow.Texto(new Rect(etiqueta.xMax + Px(18f), etiqueta.y, tarjeta.xMax - etiqueta.xMax - Px(58f), etiqueta.height), consejo, Tam(25f),
                                     EstiloShow.Oro, TextAnchor.MiddleLeft, EstiloShow.Fuente.Texto);
                }
            }
            else
            {
                EstiloShow.Texto(tarjeta, "Montando el plató…", Tam(36f), new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto);
            }
            y = tarjeta.yMax + Px(18f);

            if (prueba.multiplicador > 1)
            {
                var r = EstiloShow.Escalar(new Rect(w * 0.5f - Px(300f), y, Px(600f), Px(70f)), 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.03f);
                EstiloShow.Panel(r, EstiloShow.Oro, r.height * 0.5f);
                EstiloShow.Texto(r, $"¡ESTRELLAS x{prueba.multiplicador}!", Tam(44f), Color.white);
                for (int s = -1; s <= 1; s += 2)
                {
                    float lado = Px(76f);
                    EstiloShow.Estrella(new Rect(r.center.x + s * (r.width * 0.5f + lado * 0.45f) - lado * 0.5f, r.center.y - lado * 0.5f, lado, lado),
                                        EstiloShow.Oro, Time.unscaledTime * 60f * s);
                }
                y += Px(84f);
            }
            if (saboteadorSiguiente >= 0)
            {
                var r = new Rect(w * 0.5f - Px(560f), y, Px(1120f), Px(66f));
                EstiloShow.Panel(r, new Color(0.45f, 0.2f, 0.7f, 0.95f), r.height * 0.5f);
                string texto = RolSabotaje(prueba.escena) == "VILLANO"
                    ? $"{NombreDe(saboteadorSiguiente)} va último: ¡esta ronda es el VILLANO!"
                    : $"{NombreDe(saboteadorSiguiente)} va último: ¡esta ronda SABOTEA!";
                EstiloShow.Texto(r, texto, Tam(36f), EstiloShow.Lila);
            }

            // Barra que se llena hasta que empieza la prueba
            float progreso = Estado == EstadoShow.Presentando ? Mathf.Clamp01((Time.time - inicioEstado) / Mathf.Max(0.1f, DuracionPresentacion)) : 0f;
            var barra = new Rect(w * 0.5f - Px(320f), h - Px(110f), Px(640f), Px(32f));
            EstiloShow.BarraBrillo(barra, progreso, EstiloShow.Verde);
            EstiloShow.Texto(new Rect(0, barra.y - Px(54f), w, Px(46f)), Estado == EstadoShow.Cargando ? "Cargando…" : progreso < 1f ? "¡Preparados…!" : "¡YA!", Tam(38f), Color.white);
        }

        /// <summary>Tabla de estrellas: una fila por jugador que entra deslizándose, con su puesto, barra y estrellas.</summary>
        void DibujarTabla(float w, float h, float y)
        {
            if (!string.IsNullOrEmpty(resumenSabotaje))
                EstiloShow.Texto(new Rect(0, y - Px(52f), w, Px(44f)), resumenSabotaje, Tam(30f), EstiloShow.Lila, TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto);

            var orden = Enumerable.Range(0, 4).Where(i => participa[i]).OrderByDescending(i => estrellas[i]).ToList();
            int maximo = Mathf.Max(1, orden.Count > 0 ? estrellas[orden[0]] : 1);
            Color[] medallas = { EstiloShow.Oro, new Color(0.82f, 0.86f, 0.92f), new Color(0.88f, 0.56f, 0.3f), EstiloShow.Morado };
            float anchoFila = Mathf.Min(Px(1150f), w * 0.9f), altoFila = Px(100f);
            float t = Time.time - inicioEstado;

            int puesto = 0;
            foreach (int i in orden)
            {
                // Entrada deslizando desde la derecha, una detrás de otra
                float k = Mathf.Clamp01((t - 0.15f * puesto) / 0.45f);
                k = 1f - Mathf.Pow(1f - k, 3f);
                var fila = new Rect(w * 0.5f - anchoFila * 0.5f + (1f - k) * w, y + puesto * (altoFila + Px(16f)), anchoFila, altoFila);
                Color color = ColorDe(i);
                EstiloShow.Panel(fila, Color.Lerp(color, EstiloShow.MoradoOscuro, 0.35f).WithAlpha(0.95f), altoFila * 0.3f);

                // Medalla con el puesto
                float d = altoFila * 0.78f;
                var medalla = new Rect(fila.x + altoFila * 0.15f, fila.center.y - d * 0.5f, d, d);
                EstiloShow.Panel(medalla, medallas[Mathf.Min(puesto, 3)], d * 0.5f, -1f, false);
                EstiloShow.Texto(medalla, $"{puesto + 1}º", Tam(40f), Color.white);

                // Nombre, barra y estrellas
                float xNombre = medalla.xMax + Px(22f);
                EstiloShow.Texto(new Rect(xNombre, fila.y, fila.width * 0.22f, fila.height), NombreDe(i), Tam(50f), Color.white, TextAnchor.MiddleLeft);
                var barra = new Rect(fila.x + fila.width * 0.36f, fila.center.y - fila.height * 0.16f, fila.width * 0.34f, fila.height * 0.32f);
                EstiloShow.Barra(barra, estrellas[i] / (float)maximo * k, color);
                float lado = altoFila * 0.62f;
                var estrella = new Rect(fila.x + fila.width * 0.73f, fila.center.y - lado * 0.5f, lado, lado);
                EstiloShow.Estrella(estrella, EstiloShow.Oro, Mathf.Sin(Time.unscaledTime * 2f + i) * 10f);
                EstiloShow.Texto(new Rect(estrella.xMax + Px(10f), fila.y, Px(120f), fila.height), estrellas[i].ToString(), Tam(56f), EstiloShow.Oro, TextAnchor.MiddleLeft);

                // Lo ganado en esta ronda, en una pastilla verde que salta al aparecer
                if (estrellasRonda[i] > 0)
                {
                    float salto = 1f + 0.4f * Mathf.Exp(-Mathf.Max(0f, t - 0.6f - 0.15f * puesto) * 6f);
                    var pastilla = EstiloShow.Escalar(new Rect(fila.xMax - Px(130f), fila.center.y - Px(28f), Px(108f), Px(56f)), salto);
                    EstiloShow.Panel(pastilla, EstiloShow.Verde, pastilla.height * 0.5f, -1f, false);
                    EstiloShow.Texto(pastilla, $"+{estrellasRonda[i]}", Tam(36f), Color.white);
                }
                puesto++;
            }
        }

        static float Px(float v) => EstiloShow.Px(v);
        static int Tam(float v) => Mathf.RoundToInt(EstiloShow.Px(v));

        /// <summary>Reduce el tamaño de un texto si no cabe en el ancho dado.</summary>
        static int Ajustar(string texto, int tamano, float anchoMaximo)
        {
            float ancho = EstiloShow.Ancho(texto, tamano);
            return ancho > anchoMaximo ? Mathf.FloorToInt(tamano * anchoMaximo / ancho) : tamano;
        }
    }
}
