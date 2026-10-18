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
        }

        public enum EstadoShow { Inactivo, Cargando, Presentando, EnRonda, Resultado, Marcador, Final }

        public static ShowBicho Instancia { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos() => Instancia = null;

        public Prueba[] pruebas =
        {
            new Prueba { escena = "Mudanza", nombre = "Mudanza frágil", multiplicador = 1 },
            new Prueba { escena = "Voley", nombre = "Voley atados", multiplicador = 1 },
            new Prueba { escena = "Final", nombre = "LA GRAN FINAL: Juntos o nada", multiplicador = 2 },
        };
        public string escenaMenu = "Menu";
        [Tooltip("Segundos del cartel de presentación antes de cada ronda.")]
        public float duracionPresentacion = 2.5f;
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
        GUIStyle estiloGrande, estiloMedio, estiloPequeno;

        void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject); // ya hay un show vivo (volvemos al menú)
                return;
            }
            Instancia = this;
            DontDestroyOnLoad(gameObject);
        }

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
            Estadisticas.Texto($"EMPIEZA EL SHOW · bots: {(bots ? "sí" : "no")} · habilidad de los bots: {habilidadBots:0.00} · rondas de {duracionRondas:0} s · humanos: {PlayerInput.all.Count}");
            saboteadorSiguiente = -1;
            RondaActual = 0;
            CargarRonda();
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
            SceneManager.LoadScene(pruebas[RondaActual].escena);
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
                    if (Time.time - inicioEstado > duracionPresentacion && ronda != null)
                    {
                        ControladorBicho.EntradaBloqueada = false;
                        ronda.IniciarDesdeShow();
                        Estado = EstadoShow.EnRonda;
                    }
                    break;

                case EstadoShow.Resultado:
                    if (Time.time - inicioEstado > duracionResultado)
                    {
                        Estado = RondaActual + 1 < pruebas.Length ? EstadoShow.Marcador : EstadoShow.Final;
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
            int mult = Mathf.Max(1, pruebas[RondaActual].multiplicador);
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
            Estadisticas.Texto($"Estrellas tras {pruebas[RondaActual].nombre}: {string.Join(" · ", tabla)}" + (string.IsNullOrEmpty(resumenSabotaje) ? "" : $" · {resumenSabotaje}"));
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
        }

        /// <summary>
        /// El humano con menos estrellas sabotea la siguiente ronda (si hay al menos dos humanos;
        /// con uno solo, nunca le quitamos el bicho). Empates: el que menos ganó esta ronda.
        /// </summary>
        int ElegirSaboteadorSiguiente()
        {
            if (RondaActual + 1 >= pruebas.Length) return -1;
            var humanos = Enumerable.Range(0, 4).Where(i => esHumano[i]).ToList();
            if (humanos.Count < 2) return -1;
            return humanos.OrderBy(i => estrellas[i]).ThenBy(i => estrellasRonda[i]).ThenBy(_ => Random.value).First();
        }

        string NombreDe(int i) => esHumano[i] ? $"J{i + 1}" : $"Bot {i + 1}";

        Color ColorDe(int i) => GestorJugadores.Instancia != null ? GestorJugadores.Instancia.ColorDe(i) : Color.white;

        // ---------------------------------------------------------------------
        // Pantallas del show
        // ---------------------------------------------------------------------

        void OnGUI()
        {
            if (Estado == EstadoShow.Inactivo || Estado == EstadoShow.EnRonda || Estado == EstadoShow.Resultado) return;
            PrepararEstilos();
            float w = Screen.width, h = Screen.height;

            switch (Estado)
            {
                case EstadoShow.Cargando:
                case EstadoShow.Presentando:
                    Fondo(new Rect(0, h * 0.25f, w, h * 0.42f));
                    var prueba = pruebas[RondaActual];
                    RondaBase.Texto(new Rect(0, h * 0.27f, w, h * 0.07f), $"RONDA {RondaActual + 1} DE {pruebas.Length}", estiloMedio, new Color(1f, 0.9f, 0.4f));
                    RondaBase.Texto(new Rect(0, h * 0.34f, w, h * 0.13f), prueba.nombre.ToUpper(), estiloGrande, Color.white);
                    float y = h * 0.49f;
                    if (prueba.multiplicador > 1)
                    {
                        RondaBase.Texto(new Rect(0, y, w, h * 0.06f), $"¡ESTRELLAS x{prueba.multiplicador}!", estiloMedio, RondaBase.Parpadeo(Color.yellow));
                        y += h * 0.07f;
                    }
                    if (saboteadorSiguiente >= 0)
                        RondaBase.Texto(new Rect(0, y, w, h * 0.06f), $"{NombreDe(saboteadorSiguiente)} va último: ¡esta ronda SABOTEA!", estiloMedio, new Color(0.85f, 0.6f, 1f));
                    break;

                case EstadoShow.Marcador:
                    DibujarTabla(w, h, $"MARCADOR TRAS LA RONDA {RondaActual + 1}");
                    var siguiente = pruebas[RondaActual + 1];
                    RondaBase.Texto(new Rect(0, h * 0.76f, w, h * 0.05f), $"Siguiente: {siguiente.nombre}" + (siguiente.multiplicador > 1 ? $"  (¡estrellas x{siguiente.multiplicador}!)" : ""), estiloMedio, Color.white);
                    if (saboteadorSiguiente >= 0)
                        RondaBase.Texto(new Rect(0, h * 0.81f, w, h * 0.05f), $"{NombreDe(saboteadorSiguiente)} va último y será SABOTEADOR", estiloPequeno, new Color(0.85f, 0.6f, 1f));
                    RondaBase.Texto(new Rect(0, h * 0.88f, w, h * 0.06f), "A / START para seguir", estiloMedio, RondaBase.Parpadeo(Color.yellow));
                    break;

                case EstadoShow.Final:
                    int ganador = Enumerable.Range(0, 4).Where(i => participa[i]).OrderByDescending(i => estrellas[i]).FirstOrDefault();
                    bool empate = Enumerable.Range(0, 4).Count(i => participa[i] && estrellas[i] == estrellas[ganador]) > 1;
                    DibujarTabla(w, h, "¡FIN DEL BICHO SHOW!");
                    RondaBase.Texto(new Rect(0, h * 0.74f, w, h * 0.1f),
                        empate ? "¡EMPATE EN LO MÁS ALTO!" : $"¡GANA {NombreDe(ganador).ToUpper()}!",
                        estiloGrande, empate ? Color.white : ColorDe(ganador));
                    RondaBase.Texto(new Rect(0, h * 0.88f, w, h * 0.06f), "A / START para volver al menú", estiloMedio, RondaBase.Parpadeo(Color.yellow));
                    break;
            }
        }

        void DibujarTabla(float w, float h, string titulo)
        {
            Fondo(new Rect(w * 0.18f, h * 0.06f, w * 0.64f, h * 0.66f));
            RondaBase.Texto(new Rect(0, h * 0.08f, w, h * 0.09f), titulo, estiloGrande, Color.white);
            if (!string.IsNullOrEmpty(resumenSabotaje))
                RondaBase.Texto(new Rect(0, h * 0.18f, w, h * 0.04f), resumenSabotaje, estiloPequeno, new Color(0.85f, 0.6f, 1f));

            var orden = Enumerable.Range(0, 4).Where(i => participa[i]).OrderByDescending(i => estrellas[i]).ToList();
            int maximo = Mathf.Max(1, orden.Count > 0 ? estrellas[orden[0]] : 1);
            float y = h * 0.25f;
            int puesto = 1;
            foreach (int i in orden)
            {
                var fila = new Rect(w * 0.22f, y, w * 0.56f, h * 0.1f);
                Color color = ColorDe(i);

                estiloMedio.alignment = TextAnchor.MiddleLeft;
                RondaBase.Texto(new Rect(fila.x, fila.y, fila.width * 0.3f, fila.height), $"{puesto}º  {NombreDe(i)}", estiloMedio, color);
                estiloMedio.alignment = TextAnchor.MiddleCenter;

                // Barra de estrellas totales
                var barra = new Rect(fila.x + fila.width * 0.3f, fila.y + fila.height * 0.3f, fila.width * 0.45f, fila.height * 0.4f);
                RondaBase.Barra(barra, estrellas[i] / (float)maximo, color);

                estiloPequeno.alignment = TextAnchor.MiddleLeft;
                string extra = estrellasRonda[i] > 0 ? $"  (+{estrellasRonda[i]})" : "";
                RondaBase.Texto(new Rect(fila.x + fila.width * 0.77f, fila.y, fila.width * 0.25f, fila.height), $"{estrellas[i]} estrellas{extra}", estiloPequeno, Color.white);
                estiloPequeno.alignment = TextAnchor.MiddleCenter;

                y += h * 0.11f;
                puesto++;
            }
        }

        static void Fondo(Rect r)
        {
            var antes = GUI.color;
            GUI.color = new Color(0.08f, 0.05f, 0.14f, 0.82f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = antes;
        }

        void PrepararEstilos()
        {
            int tamano = Mathf.Max(12, Screen.height / 28);
            if (estiloGrande == null)
            {
                estiloGrande = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
                estiloMedio = new GUIStyle(estiloGrande);
                estiloPequeno = new GUIStyle(estiloGrande) { fontStyle = FontStyle.Normal };
            }
            estiloGrande.fontSize = Mathf.RoundToInt(tamano * 2.2f);
            estiloMedio.fontSize = Mathf.RoundToInt(tamano * 1.2f);
            estiloPequeno.fontSize = Mathf.RoundToInt(tamano * 0.85f);
        }
    }
}
