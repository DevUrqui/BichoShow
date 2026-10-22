using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Menú del juego: Empezar el show · Jugar online · Vestuario · Ranking · Opciones · Salir.
    /// Online: partida rápida (te junta con quien esté buscando, sin código), crear una sala o
    /// unirse con código.
    /// Cualquier jugador unido lo maneja con el stick (arriba/abajo para elegir, izquierda/
    /// derecha para cambiar un valor) y A / START para aceptar. Las opciones se guardan.
    /// Mientras el menú está abierto, los bichos no se mueven (el stick es para el menú), salvo
    /// en la sala online: ahí todos corren por el plató mientras llegan los demás.
    /// </summary>
    public class MenuPrincipal : MonoBehaviour
    {
        enum Pantalla { Principal, Opciones, Vestuario, Online, Codigo, Sala, SalaMenu, Ranking }

        static readonly string[] NombresHabilidad = { "Torpes", "Normales", "Hábiles" };
        static readonly float[] ValoresHabilidad = { 0.25f, 0.55f, 0.85f };
        static readonly int[] Duraciones = { 60, 90, 120 };
        /// <summary>Partida rápida: lo que se espera a que llegue gente antes de empezar (y si se llena).</summary>
        const float EsperaRapida = 40f, EsperaLlena = 5f;

        Pantalla pantalla = Pantalla.Principal;
        int seleccion;
        bool bots = true;
        int nivelHabilidad = 1;
        int nivelDuracion = 1;

        int jugadoresAntes;
        float ignorarHasta;
        float salidaRapida = -1f; // partida rápida (anfitrión): cuándo empieza el show
        readonly Dictionary<PlayerInput, Vector2> stickAnterior = new Dictionary<PlayerInput, Vector2>();

        void Start()
        {
            bots = PlayerPrefs.GetInt("BichoShow.Bots", 1) == 1;
            nivelHabilidad = Mathf.Clamp(PlayerPrefs.GetInt("BichoShow.Habilidad", 1), 0, 2);
            nivelDuracion = Mathf.Clamp(PlayerPrefs.GetInt("BichoShow.Duracion", 1), 0, 2);

            ControladorBicho.EntradaBloqueada = true;
            // En una sala online el anfitrión no admite jugadores nuevos de aquí (les tocaría el número de uno de fuera).
            if (GestorJugadores.Instancia != null) GestorJugadores.Instancia.PermitirUnirse(!RedShow.EsAnfitrion);
            Musica.Poner(Musica.Tema.Plato);
            Sonidos.Precargar();
            if (Keyboard.current != null) Keyboard.current.onTextInput += AlEscribir;
        }

        void OnDestroy()
        {
            MandoRemoto.Pausado = false;
            if (Keyboard.current != null) Keyboard.current.onTextInput -= AlEscribir;
        }


        int NumeroOpciones => pantalla switch
        {
            Pantalla.Principal => 6,
            Pantalla.Online => 4,
            Pantalla.SalaMenu => RedShow.EsAnfitrion ? 3 : 2,
            _ => 6,
        };
        const int OpcionVolver = 5;

        void Update()
        {
            // Partida online: mientras haya sala, su pantalla (y al cerrarse, vuelta a la de online).
            bool enSala = pantalla == Pantalla.Sala || pantalla == Pantalla.SalaMenu;
            if (RedShow.EnLinea && !enSala) { pantalla = Pantalla.Sala; enSala = true; }
            else if (!RedShow.EnLinea && enSala) { pantalla = Pantalla.Online; seleccion = 0; enSala = false; }
            ControladorBicho.EntradaBloqueada = pantalla != Pantalla.Sala;
            MandoRemoto.Pausado = RedShow.EsInvitado && pantalla != Pantalla.Sala; // el stick es para el menú
            CuentaRapida();

            // Buscando partida: START, X o Esc cancelan.
            if (RedShow.Buscando)
            {
                bool cancelar = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
                foreach (var entrada in PlayerInput.all)
                {
                    if (entrada.actions == null) continue;
                    var empezar = entrada.actions.FindAction("Empezar");
                    var lanzarse = entrada.actions.FindAction("Lanzarse");
                    cancelar |= (empezar != null && empezar.WasPressedThisFrame()) || (lanzarse != null && lanzarse.WasPressedThisFrame());
                }
                if (cancelar) { RedShow.CancelarBusqueda(); Sonidos.Reproducir(Sonido.MenuMover); }
                return;
            }

            // Quien acaba de unirse ha pulsado un botón: que esa pulsación no elija nada.
            if (PlayerInput.all.Count != jugadoresAntes)
            {
                jugadoresAntes = PlayerInput.all.Count;
                // Quien se une saluda con un "¡hurra!".
                foreach (var b in ControladorBicho.Todos)
                    if (!b.EsBot && b.TryGetComponent(out AnimadorBicho a)) a.Celebrar(2.5f);
                ignorarHasta = Time.unscaledTime + 0.4f;
            }
            if (Time.unscaledTime < ignorarHasta || PlayerInput.all.Count == 0) return;
            if (pantalla == Pantalla.Vestuario) { ActualizarVestuario(); return; }
            if (pantalla == Pantalla.Ranking) { ActualizarRanking(); return; }
            if (pantalla == Pantalla.Codigo) { ActualizarCodigo(); return; }
            if (pantalla == Pantalla.Sala) { ActualizarSala(); return; }
            if (RedShow.Ocupado) return; // conectando: esperar

            foreach (var entrada in PlayerInput.all)
            {
                if (entrada.actions == null) continue;
                var mover = entrada.actions.FindAction("Mover");
                Vector2 stick = mover != null ? mover.ReadValue<Vector2>() : Vector2.zero;
                stickAnterior.TryGetValue(entrada, out Vector2 antes);
                stickAnterior[entrada] = stick;

                if (stick.y > 0.6f && antes.y <= 0.6f) { seleccion = (seleccion + NumeroOpciones - 1) % NumeroOpciones; Sonidos.Reproducir(Sonido.MenuMover); }
                if (stick.y < -0.6f && antes.y >= -0.6f) { seleccion = (seleccion + 1) % NumeroOpciones; Sonidos.Reproducir(Sonido.MenuMover); }
                if (stick.x > 0.6f && antes.x <= 0.6f) CambiarValor(+1);
                if (stick.x < -0.6f && antes.x >= -0.6f) CambiarValor(-1);

                var saltar = entrada.actions.FindAction("Saltar");
                var empezar = entrada.actions.FindAction("Empezar");
                if ((saltar != null && saltar.WasPressedThisFrame()) || (empezar != null && empezar.WasPressedThisFrame()))
                {
                    Aceptar();
                    return;
                }
            }
        }

        void CambiarValor(int sentido)
        {
            if (pantalla != Pantalla.Opciones) return;
            switch (seleccion)
            {
                case 0: bots = !bots; break;
                case 1: nivelHabilidad = (nivelHabilidad + sentido + 3) % 3; break;
                case 2: nivelDuracion = (nivelDuracion + sentido + 3) % 3; break;
                case 3: Sonidos.Volumen = PasoVolumen(Sonidos.Volumen, sentido); break;
                case 4: Musica.Volumen = PasoVolumen(Musica.Volumen, sentido); break;
                default: return;
            }
            Guardar();
            Sonidos.Reproducir(Sonido.MenuMover, null, 1f, 1.3f, 0f);
        }

        /// <summary>Sube o baja el volumen de 0.2 en 0.2 (dando la vuelta: de 0 a 1 y de 1 a 0).</summary>
        static float PasoVolumen(float actual, int sentido)
        {
            int paso = Mathf.RoundToInt(actual * 5f) + sentido;
            if (paso > 5) paso = 0;
            if (paso < 0) paso = 5;
            return paso / 5f;
        }

        void Aceptar()
        {
            Sonidos.Reproducir(Sonido.MenuAceptar);
            if (pantalla == Pantalla.Principal)
            {
                switch (seleccion)
                {
                    case 0: EmpezarShow(); break;
                    case 1: pantalla = Pantalla.Online; seleccion = 0; RedShow.LimpiarAviso(); break;
                    case 2: EntrarVestuario(); break;
                    case 3: EntrarRanking(); break;
                    case 4: pantalla = Pantalla.Opciones; seleccion = 0; break;
                    case 5: Salir(); break;
                }
            }
            else if (pantalla == Pantalla.Online)
            {
                switch (seleccion)
                {
                    case 0: RedShow.BuscarPartida(); break;
                    case 1: RedShow.CrearSala(); break;
                    case 2: EntrarCodigo(); break;
                    case 3: pantalla = Pantalla.Principal; seleccion = 1; break;
                }
            }
            else if (pantalla == Pantalla.SalaMenu)
            {
                int opcion = RedShow.EsAnfitrion ? seleccion : seleccion + 1; // el invitado no tiene "Empezar"
                if (opcion == 0) EmpezarShow();
                else if (opcion == 1) pantalla = Pantalla.Sala;
                else RedShow.Salir();
            }
            else
            {
                if (seleccion == OpcionVolver) { pantalla = Pantalla.Principal; seleccion = 4; }
                else CambiarValor(+1);
            }
        }

        // ---------------------------------------------------------------------
        // Vestuario: cada jugador elige con su mando, a la vez que los demás
        // ---------------------------------------------------------------------

        readonly Dictionary<PlayerInput, int> filaVestuario = new Dictionary<PlayerInput, int>();
        readonly HashSet<PlayerInput> listos = new HashSet<PlayerInput>();
        float salirVestuarioEn = -1f;
        // Para enseñar el bicho por todos lados: lo que lo ha girado cada jugador (stick derecho)
        // y la vuelta entera que da al cambiar de bicho.
        readonly Dictionary<PlayerInput, float> giroVestuario = new Dictionary<PlayerInput, float>();
        readonly Dictionary<PlayerInput, float> vueltaPendiente = new Dictionary<PlayerInput, float>();

        void EntrarVestuario()
        {
            pantalla = Pantalla.Vestuario;
            filaVestuario.Clear();
            listos.Clear();
            salirVestuarioEn = -1f;
            editandoNombre.Clear();
            giroVestuario.Clear();
            vueltaPendiente.Clear();
        }

        void SalirVestuario()
        {
            foreach (var e in new List<PlayerInput>(editandoNombre.Keys)) TerminarNombre(e);
            foreach (var entrada in PlayerInput.all)
                if (entrada.TryGetComponent(out AnimadorBicho a)) a.giroExhibicion = 0f;
            pantalla = Pantalla.Principal;
            seleccion = 2;
            Sonidos.Reproducir(Sonido.MenuAceptar);
        }

        void ActualizarVestuario()
        {
            if (salirVestuarioEn > 0f)
            {
                if (Time.unscaledTime >= salirVestuarioEn) SalirVestuario();
                return;
            }

            foreach (var entrada in PlayerInput.all)
            {
                if (entrada.actions == null) continue;
                var mover = entrada.actions.FindAction("Mover");
                Vector2 stick = mover != null ? mover.ReadValue<Vector2>() : Vector2.zero;
                stickAnterior.TryGetValue(entrada, out Vector2 antes);
                stickAnterior[entrada] = stick;
                filaVestuario.TryGetValue(entrada, out int fila);
                bool listo = listos.Contains(entrada);
                GirarEnVestuario(entrada);

                // Escribiendo el nombre: el mando (o el teclado) es para eso.
                if (editandoNombre.TryGetValue(entrada, out var edicion))
                {
                    ActualizarNombre(entrada, edicion, stick, antes);
                    continue;
                }

                if (!listo)
                {
                    if (stick.y > 0.6f && antes.y <= 0.6f) { fila = (fila + 4) % 5; Sonidos.Reproducir(Sonido.MenuMover); }
                    if (stick.y < -0.6f && antes.y >= -0.6f) { fila = (fila + 1) % 5; Sonidos.Reproducir(Sonido.MenuMover); }
                    if (stick.x > 0.6f && antes.x <= 0.6f) CambiarPrenda(entrada, fila, +1);
                    if (stick.x < -0.6f && antes.x >= -0.6f) CambiarPrenda(entrada, fila, -1);
                    filaVestuario[entrada] = fila;
                }

                var saltar = entrada.actions.FindAction("Saltar");
                var empezar = entrada.actions.FindAction("Empezar");
                if (!listo && fila == 0 && saltar != null && saltar.WasPressedThisFrame())
                {
                    EmpezarNombre(entrada); // A en "Nombre": a escribirlo
                    continue;
                }
                if (saltar != null && saltar.WasPressedThisFrame())
                {
                    // A: "¡listo!" (o volver a cambiar algo).
                    if (listo) listos.Remove(entrada); else listos.Add(entrada);
                    Sonidos.Reproducir(listo ? Sonido.MenuMover : Sonido.MenuAceptar);
                    if (!listo && entrada.TryGetComponent(out AnimadorBicho a)) a.Celebrar(1.5f);
                }
                if (empezar != null && empezar.WasPressedThisFrame())
                {
                    SalirVestuario();
                    return;
                }
            }

            if (listos.Count >= PlayerInput.all.Count && PlayerInput.all.Count > 0)
                salirVestuarioEn = Time.unscaledTime + 0.8f; // un momento para ver el "¡listo!" de todos
        }

        /// <summary>
        /// El bicho se balancea un poco a los lados, da una vuelta entera al cambiar de bicho y se
        /// puede girar con el stick derecho (o el ratón): así se ve también la espalda (la concha,
        /// el abdomen de la abeja, la capa…).
        /// </summary>
        void GirarEnVestuario(PlayerInput entrada)
        {
            if (!entrada.TryGetComponent(out AnimadorBicho a)) return;
            float dt = Time.unscaledDeltaTime;
            giroVestuario.TryGetValue(entrada, out float giro);
            vueltaPendiente.TryGetValue(entrada, out float vuelta);
            var mirar = entrada.actions.FindAction("Mirar");
            float x = mirar != null ? Mathf.Clamp(mirar.ReadValue<Vector2>().x, -1f, 1f) : 0f;
            if (Mathf.Abs(x) > 0.2f) giro += x * 220f * dt;
            if (vuelta > 0f)
            {
                float paso = Mathf.Min(vuelta, 540f * dt);
                vuelta -= paso;
                giro += paso;
            }
            giro = Mathf.Repeat(giro, 360f);
            giroVestuario[entrada] = giro;
            vueltaPendiente[entrada] = vuelta;
            a.giroExhibicion = giro + Mathf.Sin(Time.unscaledTime * 0.9f + entrada.playerIndex * 1.7f) * 35f;
        }

        void CambiarPrenda(PlayerInput entrada, int fila, int sentido)
        {
            if (fila == 0)
            {
                EmpezarNombre(entrada); // el nombre se escribe aparte
                return;
            }
            fila--;
            if (!entrada.TryGetComponent(out Vestuario v)) return;
            if (fila == 0)
            {
                // El bicho: Bichete, Zumbi, Concha…
                int n = PersonajeBicho.Especies.Length;
                int especie = (v.Especie + sentido + n) % n;
                v.PonerEspecie(especie);
                Vestuario.GuardarEspecie(entrada.playerIndex, especie);
                vueltaPendiente[entrada] = 360f; // una vuelta para enseñarlo entero
                Sonidos.Reproducir(Sonido.Aparecer, entrada.transform.position, 0.9f, 1f);
                return;
            }
            fila--;
            int gorro = v.Gorro, complemento = v.Complemento, cuerpo = v.Cuerpo;
            if (fila == 0) gorro = (gorro + sentido + Vestuario.Gorros.Length) % Vestuario.Gorros.Length;
            else if (fila == 1) complemento = (complemento + sentido + Vestuario.Complementos.Length) % Vestuario.Complementos.Length;
            else cuerpo = (cuerpo + sentido + Vestuario.Cuerpos.Length) % Vestuario.Cuerpos.Length;
            v.Poner(gorro, complemento, cuerpo);
            Vestuario.Guardar(entrada.playerIndex, gorro, complemento, cuerpo);
            Sonidos.Reproducir(Sonido.Aparecer, entrada.transform.position, 0.8f, 1.2f);
            if (entrada.TryGetComponent(out JugoBicho jugo)) jugo.Aplastar(); // ¡boing! al cambiarse
        }

        void DibujarVestuario(float w, float h)
        {
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));

            var jugadores = new List<PlayerInput>(PlayerInput.all);
            jugadores.Sort((a, b) => a.playerIndex.CompareTo(b.playerIndex));
            float hueco = px(20f);
            float ancho = Mathf.Min(px(430f), (w * 0.94f - hueco * (jugadores.Count - 1)) / Mathf.Max(1, jugadores.Count));
            float alto = px(436f);
            float x = w * 0.5f - (jugadores.Count * ancho + (jugadores.Count - 1) * hueco) * 0.5f;
            float y = h - alto - px(120f);

            EstiloShow.Texto(new Rect(0, y - px(90f), w, px(80f)), "VESTUARIO", tam(72f), EstiloShow.Oro);

            foreach (var j in jugadores)
            {
                int i = j.playerIndex;
                Color color = GestorJugadores.Instancia != null ? GestorJugadores.Instancia.ColorDe(i) : Color.white;
                bool listo = listos.Contains(j);
                filaVestuario.TryGetValue(j, out int fila);
                var panel = new Rect(x, y, ancho, alto);
                EstiloShow.Panel(panel, Color.Lerp(color, EstiloShow.MoradoOscuro, 0.45f).WithAlpha(0.94f), px(30f));
                EstiloShow.Texto(new Rect(panel.x, panel.y + px(8f), panel.width, px(54f)), $"J{i + 1}", tam(46f), Color.white);

                j.TryGetComponent(out Vestuario v);
                if (v == null)
                {
                    EstiloShow.Texto(new Rect(panel.x + px(16f), panel.y + px(70f), panel.width - px(32f), px(120f)),
                        "Aplica a Bichete (menú BichoShow) para usar el vestuario", tam(22f), Color.white, TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto, true);
                }
                else
                {
                    var filaNombre = new Rect(panel.x + px(14f), panel.y + px(68f), panel.width - px(28f), px(56f));
                    if (editandoNombre.TryGetValue(j, out var edicion))
                    {
                        EstiloShow.Panel(filaNombre, EstiloShow.Rosa, filaNombre.height * 0.5f, -1f, false);
                        EstiloShow.Texto(filaNombre, NombreConCursor(edicion), tam(28f), Color.white);
                    }
                    else
                    {
                        string guardado = NombresJugadores.Guardado(i);
                        FilaPrenda(filaNombre, "Nombre", guardado.Length > 0 ? guardado : "(escríbelo)", !listo && fila == 0);
                    }
                    FilaPrenda(new Rect(panel.x + px(14f), panel.y + px(130f), panel.width - px(28f), px(56f)), "Bicho",
                               PersonajeBicho.Nombres[Mathf.Clamp(v.Especie, 0, PersonajeBicho.Nombres.Length - 1)], !listo && fila == 1);
                    FilaPrenda(new Rect(panel.x + px(14f), panel.y + px(192f), panel.width - px(28f), px(56f)), "Gorro",
                               Vestuario.NombresGorros[v.Gorro], !listo && fila == 2);
                    FilaPrenda(new Rect(panel.x + px(14f), panel.y + px(254f), panel.width - px(28f), px(56f)), "Cara",
                               Vestuario.NombresComplementos[v.Complemento], !listo && fila == 3);
                    FilaPrenda(new Rect(panel.x + px(14f), panel.y + px(316f), panel.width - px(28f), px(56f)), "Cuerpo",
                               Vestuario.NombresCuerpos[v.Cuerpo], !listo && fila == 4);
                }
                var boton = new Rect(panel.x + panel.width * 0.2f, panel.yMax - px(56f), panel.width * 0.6f, px(46f));
                EstiloShow.Panel(boton, listo ? EstiloShow.Verde : EstiloShow.Morado.WithAlpha(0.9f), boton.height * 0.5f, -1f, false);
                bool escribiendo = editandoNombre.ContainsKey(j);
                string textoBoton = listo ? "¡LISTO!"
                                  : escribiendo ? (j.currentControlScheme == "Teclado" ? "Escribe · Enter = ok" : "↑↓ letra · X borra · A ok")
                                  : fila == 0 ? "A = escribir" : "A = listo";
                EstiloShow.Texto(boton, textoBoton, tam(escribiendo ? 20f : 26f), Color.white);
                x += ancho + hueco;
            }

            EstiloShow.Ayuda(new Rect(0, h - px(86f), w, px(50f)),
                ("Stick", EstiloShow.GlifoTecla, "elegir y cambiar"), ("Stick der.", EstiloShow.GlifoTecla, "girar"), ("A", EstiloShow.GlifoA, "listo"), ("START", EstiloShow.GlifoTecla, "volver"));
        }

        static void FilaPrenda(Rect r, string etiqueta, string valor, bool elegida)
        {
            int tamano = Mathf.RoundToInt(r.height * 0.46f);
            EstiloShow.Panel(r, elegida ? EstiloShow.Oro : EstiloShow.MoradoOscuro.WithAlpha(0.6f), r.height * 0.5f, -1f, false);
            float m = r.height * 0.4f;
            EstiloShow.Texto(new Rect(r.x + m, r.y, r.width * 0.35f, r.height), etiqueta, Mathf.RoundToInt(tamano * 0.8f), Color.white,
                             TextAnchor.MiddleLeft, EstiloShow.Fuente.Texto);
            string texto = elegida ? $"<  {valor}  >" : valor;
            EstiloShow.Texto(new Rect(r.x + r.width * 0.3f, r.y, r.width * 0.7f - m * 0.5f, r.height), texto, tamano, Color.white);
        }

        // ---------------------------------------------------------------------
        // Vestuario: el nombre (como en las recreativas, o con el teclado)
        // ---------------------------------------------------------------------

        const string LetrasNombre = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZabcdefghijklmnñopqrstuvwxyz0123456789 -";

        class EdicionNombre
        {
            public readonly char[] letras = new char[NombresJugadores.Largo];
            public int cursor;
            public string Texto
            {
                get
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (char c in letras) { if (c == '\0') break; sb.Append(c); }
                    return sb.ToString();
                }
            }
        }

        readonly Dictionary<PlayerInput, EdicionNombre> editandoNombre = new Dictionary<PlayerInput, EdicionNombre>();

        void EmpezarNombre(PlayerInput entrada)
        {
            var e = new EdicionNombre();
            string actual = NombresJugadores.Guardado(entrada.playerIndex);
            for (int i = 0; i < actual.Length && i < e.letras.Length; i++) e.letras[i] = actual[i];
            e.cursor = Mathf.Min(actual.Length, e.letras.Length - 1);
            editandoNombre[entrada] = e;
            // Mientras se escribe, una tecla no debe unir a un jugador nuevo.
            if (GestorJugadores.Instancia != null) GestorJugadores.Instancia.PermitirUnirse(false);
            Sonidos.Reproducir(Sonido.MenuAceptar);
        }

        void TerminarNombre(PlayerInput entrada)
        {
            if (!editandoNombre.TryGetValue(entrada, out var e)) return;
            NombresJugadores.Guardar(entrada.playerIndex, e.Texto);
            editandoNombre.Remove(entrada);
            if (editandoNombre.Count == 0 && GestorJugadores.Instancia != null && !RedShow.EsAnfitrion)
                GestorJugadores.Instancia.PermitirUnirse(true);
            Sonidos.Reproducir(Sonido.MenuAceptar);
            if (entrada.TryGetComponent(out AnimadorBicho a)) a.Celebrar(1.2f);
        }

        static void PonerLetraNombre(EdicionNombre e, char c)
        {
            if (e.letras[e.cursor] != '\0' && e.letras[e.letras.Length - 1] == '\0')
            {
                // Escribiendo en medio: lo de detrás se corre.
                for (int i = e.letras.Length - 1; i > e.cursor; i--) e.letras[i] = e.letras[i - 1];
            }
            e.letras[e.cursor] = c;
            if (e.cursor < e.letras.Length - 1) e.cursor++;
            Sonidos.Reproducir(Sonido.MenuMover, null, 0.8f, 1.2f, 0f);
        }

        static void BorrarLetraNombre(EdicionNombre e)
        {
            if (e.letras[e.cursor] == '\0' && e.cursor > 0) e.cursor--;
            for (int i = e.cursor; i < e.letras.Length - 1; i++) e.letras[i] = e.letras[i + 1];
            e.letras[e.letras.Length - 1] = '\0';
            Sonidos.Reproducir(Sonido.MenuMover, null, 0.8f, 0.8f, 0f);
        }

        /// <summary>Con el mando: arriba/abajo cambia la letra, izquierda/derecha se mueve, X borra, A guarda.</summary>
        void ActualizarNombre(PlayerInput entrada, EdicionNombre e, Vector2 stick, Vector2 antes)
        {
            if (entrada.currentControlScheme == "Teclado")
            {
                // El teclado escribe (AlEscribir); aquí, borrar, moverse y terminar.
                var teclado = Keyboard.current;
                if (teclado == null) return;
                if (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame || teclado.escapeKey.wasPressedThisFrame) { TerminarNombre(entrada); return; }
                if (teclado.backspaceKey.wasPressedThisFrame) BorrarLetraNombre(e);
                if (teclado.leftArrowKey.wasPressedThisFrame && e.cursor > 0) e.cursor--;
                if (teclado.rightArrowKey.wasPressedThisFrame && e.cursor < e.letras.Length - 1 && e.letras[e.cursor] != '\0') e.cursor++;
                return;
            }
            int letra = e.letras[e.cursor] == '\0' ? -1 : LetrasNombre.IndexOf(e.letras[e.cursor]);
            if (stick.y > 0.6f && antes.y <= 0.6f) { e.letras[e.cursor] = LetrasNombre[(letra + 1) % LetrasNombre.Length]; Sonidos.Reproducir(Sonido.MenuMover); }
            if (stick.y < -0.6f && antes.y >= -0.6f) { e.letras[e.cursor] = LetrasNombre[(letra <= 0 ? LetrasNombre.Length : letra) - 1]; Sonidos.Reproducir(Sonido.MenuMover); }
            if (stick.x > 0.6f && antes.x <= 0.6f && e.cursor < e.letras.Length - 1 && e.letras[e.cursor] != '\0') { e.cursor++; Sonidos.Reproducir(Sonido.MenuMover); }
            if (stick.x < -0.6f && antes.x >= -0.6f && e.cursor > 0) { e.cursor--; Sonidos.Reproducir(Sonido.MenuMover); }
            var saltar = entrada.actions.FindAction("Saltar");
            var lanzarse = entrada.actions.FindAction("Lanzarse");
            var empezar = entrada.actions.FindAction("Empezar");
            if ((saltar != null && saltar.WasPressedThisFrame()) || (empezar != null && empezar.WasPressedThisFrame())) { TerminarNombre(entrada); return; }
            if (lanzarse != null && lanzarse.WasPressedThisFrame()) BorrarLetraNombre(e);
        }

        /// <summary>El nombre mientras se escribe: la letra del cursor, entre corchetes.</summary>
        static string NombreConCursor(EdicionNombre e)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < e.letras.Length; i++)
            {
                char c = e.letras[i];
                if (i == e.cursor) { sb.Append('[').Append(c == '\0' ? '_' : c).Append(']'); if (c == '\0') break; continue; }
                if (c == '\0') break;
                sb.Append(c);
            }
            return sb.ToString();
        }

        // ---------------------------------------------------------------------
        // Ranking de tiempos: el de este ordenador y el mundial, carrera a carrera
        // ---------------------------------------------------------------------

        int carreraRanking;

        void EntrarRanking()
        {
            pantalla = Pantalla.Ranking;
            RankingOnline.Pedir(RankingTiempos.Carreras[carreraRanking].id);
        }

        void ActualizarRanking()
        {
            foreach (var entrada in PlayerInput.all)
            {
                if (entrada.actions == null) continue;
                var mover = entrada.actions.FindAction("Mover");
                Vector2 stick = mover != null ? mover.ReadValue<Vector2>() : Vector2.zero;
                stickAnterior.TryGetValue(entrada, out Vector2 antes);
                stickAnterior[entrada] = stick;
                int n = RankingTiempos.Carreras.Length;
                int sentido = stick.x > 0.6f && antes.x <= 0.6f ? 1 : stick.x < -0.6f && antes.x >= -0.6f ? -1 : 0;
                if (sentido != 0)
                {
                    carreraRanking = (carreraRanking + sentido + n) % n;
                    RankingOnline.Pedir(RankingTiempos.Carreras[carreraRanking].id);
                    Sonidos.Reproducir(Sonido.MenuMover);
                }
                foreach (var nombre in new[] { "Saltar", "Empezar", "Lanzarse" })
                {
                    var accion = entrada.actions.FindAction(nombre);
                    if (accion != null && accion.WasPressedThisFrame())
                    {
                        pantalla = Pantalla.Principal;
                        seleccion = 3;
                        Sonidos.Reproducir(Sonido.MenuAceptar);
                        return;
                    }
                }
            }
        }

        void DibujarRanking(float w, float h)
        {
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));
            var (id, nombreCarrera) = RankingTiempos.Carreras[carreraRanking];

            float y = h * 0.33f;
            var cabecera = new Rect(w * 0.5f - px(520f), y, px(1040f), px(76f));
            EstiloShow.Panel(cabecera, EstiloShow.Morado.WithAlpha(0.95f), cabecera.height * 0.5f);
            EstiloShow.Texto(cabecera, $"<   RANKING · {nombreCarrera}   >", tam(40f), EstiloShow.Oro);
            y += px(96f);

            float anchoTabla = px(560f), hueco = px(30f);
            float x0 = w * 0.5f - anchoTabla - hueco * 0.5f;
            var locales = RankingTiempos.Locales(id);
            var filasLocales = new List<(int, string, float, string)>();
            for (int i = 0; i < locales.Count; i++) filasLocales.Add((i + 1, locales[i].nombre, locales[i].segundos, locales[i].bicho));
            Tabla(new Rect(x0, y, anchoTabla, h - y - px(110f)), "En este ordenador", filasLocales, filasLocales.Count == 0 ? "Aún no hay tiempos. ¡Corre esta carrera!" : "");

            var mundiales = RankingOnline.Filas(id);
            var filasMundiales = new List<(int, string, float, string)>();
            foreach (var f in mundiales) filasMundiales.Add((f.puesto, f.nombre, f.segundos, f.bicho));
            Tabla(new Rect(x0 + anchoTabla + hueco, y, anchoTabla, h - y - px(110f)), "Mundial", filasMundiales, RankingOnline.Estado(id));

            EstiloShow.Ayuda(new Rect(0, h - px(86f), w, px(50f)),
                ("Stick", EstiloShow.GlifoTecla, "otra carrera"), ("A", EstiloShow.GlifoA, "volver"));
        }

        static void Tabla(Rect r, string titulo, List<(int puesto, string nombre, float segundos, string bicho)> filas, string aviso)
        {
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));
            float altoFila = Mathf.Min(px(46f), (r.height - px(70f)) / RankingTiempos.Puestos);
            float alto = px(66f) + altoFila * Mathf.Max(1, Mathf.Min(filas.Count, RankingTiempos.Puestos)) + px(16f);
            var panel = new Rect(r.x, r.y, r.width, alto);
            EstiloShow.Panel(panel, EstiloShow.MoradoOscuro.WithAlpha(0.92f), px(26f));
            EstiloShow.Texto(new Rect(r.x, r.y + px(8f), r.width, px(50f)), titulo, tam(34f), Color.white);
            float y = r.y + px(66f);
            if (filas.Count == 0)
            {
                EstiloShow.Texto(new Rect(r.x + px(16f), y, r.width - px(32f), altoFila), aviso, tam(24f), new Color(1f, 1f, 1f, 0.85f),
                                 TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto);
                return;
            }
            Color[] medallas = { EstiloShow.Oro, new Color(0.82f, 0.85f, 0.9f), new Color(0.95f, 0.6f, 0.35f) };
            foreach (var (puesto, nombre, segundos, bicho) in filas)
            {
                if (puesto > RankingTiempos.Puestos) break;
                var fila = new Rect(r.x + px(14f), y, r.width - px(28f), altoFila - px(4f));
                if (puesto <= 3) EstiloShow.Panel(fila, medallas[puesto - 1].WithAlpha(0.35f), fila.height * 0.5f, -1f, false);
                int t = tam(26f);
                EstiloShow.Texto(new Rect(fila.x + px(10f), fila.y, px(56f), fila.height), $"{puesto}º", t, puesto <= 3 ? medallas[puesto - 1] : Color.white, TextAnchor.MiddleLeft);
                EstiloShow.Texto(new Rect(fila.x + px(70f), fila.y, fila.width * 0.42f, fila.height), nombre, t, Color.white, TextAnchor.MiddleLeft, EstiloShow.Fuente.Texto);
                EstiloShow.Texto(new Rect(fila.x + fila.width * 0.52f, fila.y, fila.width * 0.24f, fila.height), bicho, tam(20f), new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleLeft, EstiloShow.Fuente.Texto);
                EstiloShow.Texto(new Rect(fila.x + fila.width * 0.72f, fila.y, fila.width * 0.26f, fila.height), RankingTiempos.Formato(segundos), t, EstiloShow.Oro, TextAnchor.MiddleRight);
                y += altoFila;
            }
        }

        // ---------------------------------------------------------------------
        // Jugar online: crear sala · escribir el código · la sala
        // ---------------------------------------------------------------------

        const string Letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        readonly char[] codigo = new char[RedShow.LargoCodigo];
        int cursor;

        void EntrarCodigo()
        {
            pantalla = Pantalla.Codigo;
            System.Array.Clear(codigo, 0, codigo.Length);
            cursor = 0;
            // Si se cortó la conexión, el código de la última sala ya está puesto: A y dentro.
            string ultimo = RedShow.UltimoCodigo;
            if (!string.IsNullOrEmpty(ultimo) && ultimo.Length == codigo.Length)
            {
                for (int i = 0; i < codigo.Length; i++) codigo[i] = char.ToUpperInvariant(ultimo[i]);
                cursor = codigo.Length - 1;
            }
            else RedShow.LimpiarAviso();
            // Mientras se escribe, una tecla no debe unir a un jugador nuevo.
            if (GestorJugadores.Instancia != null) GestorJugadores.Instancia.PermitirUnirse(false);
        }

        void SalirCodigo()
        {
            pantalla = Pantalla.Online;
            seleccion = 2;
            if (GestorJugadores.Instancia != null && !RedShow.EsAnfitrion) GestorJugadores.Instancia.PermitirUnirse(true);
            Sonidos.Reproducir(Sonido.MenuMover);
        }

        bool CodigoCompleto
        {
            get
            {
                foreach (char c in codigo) if (c == '\0') return false;
                return true;
            }
        }

        void PonerLetra(char c)
        {
            codigo[cursor] = c;
            if (cursor < codigo.Length - 1) cursor++;
            Sonidos.Reproducir(Sonido.MenuMover, null, 0.8f, 1.2f, 0f);
        }

        void BorrarLetra()
        {
            if (codigo[cursor] == '\0' && cursor > 0) cursor--;
            codigo[cursor] = '\0';
            Sonidos.Reproducir(Sonido.MenuMover, null, 0.8f, 0.8f, 0f);
        }

        void IntentarUnirse()
        {
            if (!CodigoCompleto)
            {
                Sonidos.Reproducir(Sonido.MenuMover, null, 1f, 0.6f, 0f);
                return;
            }
            Sonidos.Reproducir(Sonido.MenuAceptar);
            RedShow.Unirse(new string(codigo));
        }

        /// <summary>Lo que se teclea en el teclado (cualquier distribución) va al código.</summary>
        void AlEscribir(char c)
        {
            if (pantalla == Pantalla.Vestuario)
            {
                foreach (var par in editandoNombre)
                    if (par.Key.currentControlScheme == "Teclado" && NombresJugadores.Valido(c)) { PonerLetraNombre(par.Value, c); break; }
                return;
            }
            if (pantalla != Pantalla.Codigo || RedShow.Ocupado) return;
            c = char.ToUpperInvariant(c);
            if (Letras.IndexOf(c) >= 0) PonerLetra(c);
        }

        void ActualizarCodigo()
        {
            if (RedShow.Ocupado) return;
            var teclado = Keyboard.current;
            if (teclado != null)
            {
                if (teclado.escapeKey.wasPressedThisFrame) { SalirCodigo(); return; }
                if (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame) { IntentarUnirse(); return; }
                if (teclado.backspaceKey.wasPressedThisFrame) BorrarLetra();
                if (teclado.leftArrowKey.wasPressedThisFrame && cursor > 0) cursor--;
                if (teclado.rightArrowKey.wasPressedThisFrame && cursor < codigo.Length - 1 && codigo[cursor] != '\0') cursor++;
                if (teclado.ctrlKey.isPressed && teclado.vKey.wasPressedThisFrame) Pegar();
            }

            // Con el mando, como en las recreativas: arriba/abajo cambia la letra, izquierda/derecha se mueve.
            foreach (var entrada in PlayerInput.all)
            {
                if (entrada.actions == null || entrada.currentControlScheme == "Teclado") continue; // el teclado escribe
                var mover = entrada.actions.FindAction("Mover");
                Vector2 stick = mover != null ? mover.ReadValue<Vector2>() : Vector2.zero;
                stickAnterior.TryGetValue(entrada, out Vector2 antes);
                stickAnterior[entrada] = stick;

                int letra = codigo[cursor] == '\0' ? -1 : Letras.IndexOf(codigo[cursor]);
                if (stick.y > 0.6f && antes.y <= 0.6f) { codigo[cursor] = Letras[(letra + 1) % Letras.Length]; Sonidos.Reproducir(Sonido.MenuMover); }
                if (stick.y < -0.6f && antes.y >= -0.6f) { codigo[cursor] = Letras[(letra <= 0 ? Letras.Length : letra) - 1]; Sonidos.Reproducir(Sonido.MenuMover); }
                if (stick.x > 0.6f && antes.x <= 0.6f && cursor < codigo.Length - 1 && codigo[cursor] != '\0') { cursor++; Sonidos.Reproducir(Sonido.MenuMover); }
                if (stick.x < -0.6f && antes.x >= -0.6f && cursor > 0) { cursor--; Sonidos.Reproducir(Sonido.MenuMover); }

                var saltar = entrada.actions.FindAction("Saltar");
                var lanzarse = entrada.actions.FindAction("Lanzarse");
                var empezar = entrada.actions.FindAction("Empezar");
                if (saltar != null && saltar.WasPressedThisFrame()) { IntentarUnirse(); return; }
                if (lanzarse != null && lanzarse.WasPressedThisFrame()) BorrarLetra();
                if (empezar != null && empezar.WasPressedThisFrame()) { SalirCodigo(); return; }
            }
        }

        void Pegar()
        {
            string texto;
            try { texto = GUIUtility.systemCopyBuffer; } catch { return; }
            if (string.IsNullOrEmpty(texto)) return;
            System.Array.Clear(codigo, 0, codigo.Length);
            int n = 0;
            foreach (char letra in texto.ToUpperInvariant())
            {
                if (Letras.IndexOf(letra) < 0) continue;
                codigo[n++] = letra;
                if (n == codigo.Length) break;
            }
            cursor = Mathf.Min(n, codigo.Length - 1);
            Sonidos.Reproducir(Sonido.Aparecer, null, 0.8f, 1.2f);
        }

        /// <summary>
        /// Partida rápida, en el anfitrión: cuenta atrás hasta el show (se avisa a todos). Si se
        /// llena, se acorta. Al acabar, empieza solo, con bots en los huecos.
        /// </summary>
        void CuentaRapida()
        {
            bool enSala = pantalla == Pantalla.Sala || pantalla == Pantalla.SalaMenu;
            if (!RedShow.PartidaRapida || !RedShow.EsAnfitrion || !enSala) { salidaRapida = -1f; return; }
            float ahora = Time.unscaledTime;
            if (salidaRapida < 0f)
            {
                // Recién creada o de vuelta de un show: se vuelve a abrir a gente nueva y a contar.
                salidaRapida = ahora + EsperaRapida;
                RedShow.CerrarAGenteNueva(false);
                RedShow.AnunciarSalida(EsperaRapida);
            }
            else if (FichasSala().Count >= RedShow.MaxJugadores && salidaRapida - ahora > EsperaLlena)
            {
                salidaRapida = ahora + EsperaLlena;
                RedShow.AnunciarSalida(EsperaLlena);
            }
            if (ahora >= salidaRapida)
            {
                salidaRapida = float.MaxValue; // ya está, hasta que se vuelva al menú
                EmpezarShow();
            }
        }

        /// <summary>En la sala los bichos corren; START abre el menú de la sala.</summary>
        void ActualizarSala()
        {
            foreach (var entrada in PlayerInput.all)
            {
                var empezar = entrada.actions != null ? entrada.actions.FindAction("Empezar") : null;
                if (empezar != null && empezar.WasPressedThisFrame())
                {
                    pantalla = Pantalla.SalaMenu;
                    seleccion = 0;
                    Sonidos.Reproducir(Sonido.MenuAceptar);
                    return;
                }
            }
        }

        struct FichaSala
        {
            public int indice;
            public Color color;
            public string detalle;
        }

        static List<FichaSala> FichasSala()
        {
            var lista = new List<FichaSala>();
            if (RedShow.EsInvitado)
            {
                foreach (var m in RedShow.Marionetas)
                    if (m != null && !m.EsBot)
                        lista.Add(new FichaSala { indice = m.Indice, color = m.ColorJugador, detalle = RedShow.EsMia(m) ? "¡Tú!" : "Online" });
            }
            else if (GestorJugadores.Instancia != null)
            {
                foreach (var b in GestorJugadores.Instancia.Jugadores)
                    if (b != null && !b.EsBot)
                        lista.Add(new FichaSala { indice = b.IndiceJugador, color = b.ColorJugador, detalle = b.EsRemoto ? "Online" : "¡Tú! · anfitrión" });
            }
            lista.Sort((a, b) => a.indice.CompareTo(b.indice));
            return lista;
        }

        void DibujarOnline(float w, float h)
        {
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));
            float x = w * 0.05f, y = h * 0.36f;

            if (pantalla == Pantalla.Online)
            {
                float ancho = px(600f), alto = px(74f), paso = alto + px(16f);
                EstiloShow.Texto(new Rect(x, y - px(84f), ancho * 1.5f, px(70f)), "JUGAR ONLINE", tam(60f), Color.white, TextAnchor.MiddleLeft);
                string[] opciones = { "Partida rápida", "Crear una sala", "Unirse con código", "Volver" };
                for (int i = 0; i < opciones.Length; i++)
                {
                    EstiloShow.Boton(new Rect(x, y, ancho, alto), opciones[i], i == seleccion && !RedShow.Ocupado, tam(42f));
                    y += paso;
                }
                EstiloShow.Texto(new Rect(x, y + px(4f), ancho * 1.3f, px(90f)),
                    "Partida rápida: te junta con quien esté buscando, sin código. O crea una sala y pasa el código a tus amigos.",
                    tam(26f), Color.white, TextAnchor.UpperLeft, EstiloShow.Fuente.Texto, true);
                DibujarEstadoRed(new Rect(x, y + px(110f), ancho * 1.3f, px(70f)), RedShow.Buscando ? "Buscando partida (START para cancelar)" : "Abriendo el plató");
                EstiloShow.Ayuda(new Rect(0, h - px(86f), w, px(50f)),
                    ("Stick", EstiloShow.GlifoTecla, "elegir"), ("A", EstiloShow.GlifoA, "aceptar"));
                return;
            }

            if (pantalla == Pantalla.Codigo)
            {
                EstiloShow.Texto(new Rect(0, h * 0.33f, w, px(70f)), "CÓDIGO DE LA SALA", tam(60f), Color.white);
                float lado = px(112f), hueco = px(18f);
                float total = codigo.Length * lado + (codigo.Length - 1) * hueco;
                float cx = w * 0.5f - total * 0.5f, cy = h * 0.33f + px(96f);
                for (int i = 0; i < codigo.Length; i++)
                {
                    bool elegida = i == cursor && !RedShow.Ocupado;
                    var casilla = new Rect(cx + i * (lado + hueco), cy, lado, lado * 1.15f);
                    if (elegida) casilla = EstiloShow.Escalar(casilla, 1.06f + Mathf.Sin(Time.unscaledTime * 6f) * 0.02f);
                    EstiloShow.Panel(casilla, elegida ? EstiloShow.Oro : EstiloShow.Morado.WithAlpha(0.92f), px(22f));
                    string letra = codigo[i] == '\0' ? (elegida && Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f ? "_" : "") : codigo[i].ToString();
                    EstiloShow.Texto(casilla, letra, tam(76f), Color.white);
                }
                DibujarEstadoRed(new Rect(w * 0.5f - px(520f), cy + lado * 1.15f + px(30f), px(1040f), px(64f)), "Entrando en la sala");
                EstiloShow.Ayuda(new Rect(0, h - px(150f), w, px(50f)),
                    ("Teclado", EstiloShow.GlifoTecla, "escribe · Ctrl+V pega"), ("Enter", EstiloShow.GlifoTecla, "entrar"), ("Esc", EstiloShow.GlifoTecla, "volver"));
                EstiloShow.Ayuda(new Rect(0, h - px(86f), w, px(50f)),
                    ("Stick", EstiloShow.GlifoTecla, "arriba/abajo letra"), ("A", EstiloShow.GlifoA, "entrar"),
                    ("X", EstiloShow.GlifoX, "borrar"), ("START", EstiloShow.GlifoTecla, "volver"));
                return;
            }

            // --- La sala (Sala y SalaMenu) ---
            var tarjeta = new Rect(x, y - px(30f), px(620f), px(250f));
            EstiloShow.Panel(tarjeta, EstiloShow.MoradoOscuro.WithAlpha(0.92f), px(34f));
            if (RedShow.PartidaRapida)
            {
                // Partida rápida: sin código; la cuenta atrás hasta el show.
                EstiloShow.Texto(new Rect(tarjeta.x, tarjeta.y + px(14f), tarjeta.width, px(44f)), "PARTIDA RÁPIDA", tam(30f), Color.white);
                float? queda = RedShow.SegundosParaSalida;
                string cuenta = queda.HasValue ? Mathf.CeilToInt(queda.Value).ToString() : "...";
                float latido = queda.HasValue && queda.Value < 6f ? 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.08f : 1f;
                EstiloShow.Texto(EstiloShow.Escalar(new Rect(tarjeta.x, tarjeta.y + px(56f), tarjeta.width, px(110f)), latido), cuenta, tam(92f), EstiloShow.Oro);
                EstiloShow.Texto(new Rect(tarjeta.x + px(20f), tarjeta.y + px(172f), tarjeta.width - px(40f), px(60f)),
                                 "Buscando rivales... El show empieza solo (con bots en los huecos).", tam(25f),
                                 Color.white, TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto, true);
            }
            else
            {
                EstiloShow.Texto(new Rect(tarjeta.x, tarjeta.y + px(14f), tarjeta.width, px(44f)), "CÓDIGO DE LA SALA", tam(30f), Color.white);
                string codigoSala = RedShow.Codigo ?? "";
                EstiloShow.Texto(new Rect(tarjeta.x, tarjeta.y + px(56f), tarjeta.width, px(110f)), string.Join(" ", codigoSala.ToCharArray()), tam(92f), EstiloShow.Oro);
                string subtitulo = RedShow.EsAnfitrion
                    ? "¡Ya está copiado! Pégalo en el chat de tus amigos"
                    : "Estás en la sala de un amigo";
                EstiloShow.Texto(new Rect(tarjeta.x + px(20f), tarjeta.y + px(172f), tarjeta.width - px(40f), px(60f)), subtitulo, tam(25f),
                                 Color.white, TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto, true);
            }

            var fichas = FichasSala();
            float infoY = tarjeta.yMax + px(24f);
            EstiloShow.Texto(new Rect(x, infoY, px(900f), px(44f)), $"{fichas.Count} de {RedShow.MaxJugadores} jugadores", tam(36f), Color.white, TextAnchor.MiddleLeft);
            EstiloShow.Texto(new Rect(x, infoY + px(46f), px(900f), px(80f)),
                RedShow.PartidaRapida
                    ? "¡Corred por el plató mientras llega gente! Si se llena, empieza enseguida."
                    : RedShow.EsAnfitrion
                        ? "¡Corred por el plató mientras llegan todos! Cuando estéis, START y ¡Empezar el show!"
                        : "¡Corre por el plató! El anfitrión empezará el show cuando estéis todos.",
                tam(25f), Color.white, TextAnchor.UpperLeft, EstiloShow.Fuente.Texto, true);

            // Fichas de los jugadores
            float anchoFicha = px(250f), altoFicha = px(78f), separacion = px(18f);
            float fx = w * 0.5f - (fichas.Count * anchoFicha + (fichas.Count - 1) * separacion) * 0.5f;
            float fy = h - px(200f);
            foreach (var f in fichas)
            {
                var ficha = new Rect(fx, fy + Mathf.Sin(Time.unscaledTime * 3f + f.indice) * px(4f), anchoFicha, altoFicha);
                EstiloShow.Panel(ficha, Color.Lerp(f.color, EstiloShow.MoradoOscuro, 0.25f).WithAlpha(0.95f), altoFicha * 0.5f);
                EstiloShow.Texto(new Rect(ficha.x, ficha.y - px(6f), ficha.width, ficha.height * 0.7f), $"J{f.indice + 1}", tam(44f), Color.white);
                EstiloShow.Texto(new Rect(ficha.x, ficha.y + ficha.height * 0.48f, ficha.width, ficha.height * 0.45f), f.detalle, tam(22f),
                                 Color.white, TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto);
                fx += anchoFicha + separacion;
            }

            if (pantalla == Pantalla.SalaMenu)
            {
                // Menú de la sala, encima de todo.
                int n = RedShow.EsAnfitrion ? 3 : 2;
                var fondo = new Rect(w * 0.5f - px(380f), h * 0.4f, px(760f), px(120f + 90f * n));
                EstiloShow.Panel(fondo, EstiloShow.Morado.WithAlpha(0.97f), px(40f));
                EstiloShow.Texto(new Rect(fondo.x, fondo.y + px(16f), fondo.width, px(60f)), RedShow.PartidaRapida ? "PARTIDA RÁPIDA" : "SALA", tam(52f), Color.white);
                string[] opciones = RedShow.PartidaRapida
                    ? (RedShow.EsAnfitrion ? new[] { "¡Empezar ya!", "Seguir esperando", "Salir (se cierra para todos)" } : new[] { "Seguir esperando", "Salir" })
                    : RedShow.EsAnfitrion
                        ? new[] { "¡Empezar el show!", "Seguir en la sala", "Cerrar la sala" }
                        : new[] { "Seguir en la sala", "Salir de la sala" };
                float by = fondo.y + px(96f);
                for (int i = 0; i < opciones.Length; i++)
                {
                    EstiloShow.Boton(new Rect(fondo.x + px(70f), by, fondo.width - px(140f), px(74f)), opciones[i], i == seleccion && !RedShow.Ocupado, tam(40f));
                    by += px(90f);
                }
                EstiloShow.Ayuda(new Rect(0, h - px(86f), w, px(50f)),
                    ("Stick", EstiloShow.GlifoTecla, "elegir"), ("A", EstiloShow.GlifoA, "aceptar"));
            }
            else
            {
                EstiloShow.Ayuda(new Rect(0, h - px(86f), w, px(50f)),
                    ("Stick", EstiloShow.GlifoTecla, "correr"), ("A", EstiloShow.GlifoA, "saltar"),
                    ("START", EstiloShow.GlifoTecla, RedShow.EsAnfitrion && !RedShow.PartidaRapida ? "empezar el show" : "menú"));
            }
        }

        /// <summary>"Conectando..." mientras se espera, o el último aviso de la red (en rosa) si algo ha fallado.</summary>
        static void DibujarEstadoRed(Rect r, string esperando)
        {
            if (RedShow.Ocupado)
            {
                int puntos = 1 + (int)(Time.unscaledTime * 3f) % 3;
                EstiloShow.Panel(r, EstiloShow.Cian.WithAlpha(0.9f), r.height * 0.5f);
                EstiloShow.Texto(r, esperando + new string('.', puntos), Mathf.RoundToInt(r.height * 0.45f), EstiloShow.MoradoOscuro);
            }
            else if (!string.IsNullOrEmpty(RedShow.Aviso))
            {
                EstiloShow.Panel(r, EstiloShow.Rosa.WithAlpha(0.95f), r.height * 0.4f);
                EstiloShow.Texto(new Rect(r.x + r.height * 0.3f, r.y, r.width - r.height * 0.6f, r.height), RedShow.Aviso,
                                 Mathf.RoundToInt(r.height * 0.36f), Color.white, TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto, true);
            }
        }

        void EmpezarShow()
        {
            var show = ShowBicho.Instancia;
            if (show == null)
            {
                Debug.LogError("Bicho Show: no hay ShowBicho en la escena del menú.");
                return;
            }
            Guardar();
            ControladorBicho.EntradaBloqueada = false;
            if (RedShow.PartidaRapida)
            {
                // Partida rápida: nadie entra a mitad del show, y siempre con bots en los huecos.
                salidaRapida = float.MaxValue;
                RedShow.CerrarAGenteNueva(true);
                RedShow.AnunciarSalida(-1f);
                show.EmpezarShow(true, ValoresHabilidad[nivelHabilidad], Duraciones[nivelDuracion]);
                return;
            }
            show.EmpezarShow(bots, ValoresHabilidad[nivelHabilidad], Duraciones[nivelDuracion]);
        }

        void Guardar()
        {
            PlayerPrefs.SetInt("BichoShow.Bots", bots ? 1 : 0);
            PlayerPrefs.SetInt("BichoShow.Habilidad", nivelHabilidad);
            PlayerPrefs.SetInt("BichoShow.Duracion", nivelDuracion);
            PlayerPrefs.SetFloat(Sonidos.ClaveVolumen, Sonidos.Volumen);
            PlayerPrefs.SetFloat(Musica.ClaveVolumen, Musica.Volumen);
            PlayerPrefs.Save();
        }

        static void Salir()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------------
        // Pantalla
        // ---------------------------------------------------------------------

        void OnGUI()
        {
            float w = Screen.width, h = Screen.height;
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));

            // --- Título con letras que rebotan y cinta debajo ---
            Color[] coloresTitulo = { EstiloShow.Oro, EstiloShow.Rosa, EstiloShow.Cian, EstiloShow.Verde, EstiloShow.Lila };
            EstiloShow.TituloRebote(new Rect(0, h * 0.04f, w, px(190f)), "BICHO SHOW", tam(170f), coloresTitulo);
            var cinta = new Rect(w * 0.5f - px(430f), h * 0.04f + px(185f), px(860f), px(58f));
            EstiloShow.Panel(cinta, EstiloShow.Rosa.WithAlpha(0.95f), cinta.height * 0.5f);
            EstiloShow.Texto(cinta, "El concurso de bichos más torpe de la tele", tam(34f), Color.white, TextAnchor.MiddleCenter, EstiloShow.Fuente.Titulo);

            if (PlayerInput.all.Count == 0)
            {
                // Nadie unido aún: un cartel grande que late.
                var cartel = EstiloShow.Escalar(new Rect(w * 0.5f - px(520f), h * 0.5f, px(1040f), px(110f)), 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.02f);
                EstiloShow.Panel(cartel, EstiloShow.Morado.WithAlpha(0.92f), cartel.height * 0.5f);
                EstiloShow.Texto(cartel, "¡Pulsa cualquier botón para unirte!", tam(56f), EstiloShow.Oro);
                EstiloShow.Ayuda(new Rect(0, h * 0.5f + px(140f), w, px(52f)),
                    ("A", EstiloShow.GlifoA, "en el mando"), ("Espacio", EstiloShow.GlifoTecla, "en el teclado"));
                return;
            }

            if (pantalla == Pantalla.Vestuario)
            {
                DibujarVestuario(w, h);
                return;
            }
            if (pantalla == Pantalla.Ranking)
            {
                DibujarRanking(w, h);
                return;
            }
            if (pantalla == Pantalla.Online || pantalla == Pantalla.Codigo || pantalla == Pantalla.Sala || pantalla == Pantalla.SalaMenu)
            {
                DibujarOnline(w, h);
                return;
            }

            // --- Opciones (columna a la izquierda: el escenario con los bichos queda a la vista) ---
            float x = w * 0.05f, ancho = px(pantalla == Pantalla.Principal ? 560f : 780f);
            float alto = px(74f), paso = alto + px(pantalla == Pantalla.Principal ? 14f : 16f);
            float y = h * 0.36f;
            if (pantalla == Pantalla.Principal)
            {
                string[] opciones = { "¡Empezar el show!", "Jugar online", "Vestuario", "Ranking", "Opciones", "Salir" };
                for (int i = 0; i < opciones.Length; i++)
                {
                    EstiloShow.Boton(new Rect(x, y, ancho, alto), opciones[i], i == seleccion, tam(42f));
                    y += paso;
                }
            }
            else
            {
                EstiloShow.Texto(new Rect(x, y - px(84f), ancho, px(70f)), "OPCIONES", tam(60f), Color.white, TextAnchor.MiddleLeft);
                Fila(new Rect(x, y, ancho, alto), 0, "Bots en los huecos", bots ? "Sí" : "No", null); y += paso;
                Fila(new Rect(x, y, ancho, alto), 1, "Habilidad de los bots", NombresHabilidad[nivelHabilidad], null); y += paso;
                Fila(new Rect(x, y, ancho, alto), 2, "Duración de cada ronda", $"{Duraciones[nivelDuracion]} s", null); y += paso;
                Fila(new Rect(x, y, ancho, alto), 3, "Efectos", null, Sonidos.Volumen); y += paso;
                Fila(new Rect(x, y, ancho, alto), 4, "Música", null, Musica.Volumen); y += paso;
                EstiloShow.Boton(new Rect(x, y, ancho * 0.5f, alto), "Volver", seleccion == OpcionVolver, tam(40f));
            }

            // --- Jugadores unidos: una ficha de su color cada uno ---
            var jugadores = new List<PlayerInput>(PlayerInput.all);
            jugadores.Sort((a, b) => a.playerIndex.CompareTo(b.playerIndex));
            float anchoFicha = px(250f), altoFicha = px(78f), hueco = px(18f);
            float fx = w * 0.5f - (jugadores.Count * anchoFicha + (jugadores.Count - 1) * hueco) * 0.5f;
            float fy = h - px(200f);
            foreach (var j in jugadores)
            {
                int i = j.playerIndex;
                Color color = GestorJugadores.Instancia != null ? GestorJugadores.Instancia.ColorDe(i) : Color.white;
                var ficha = new Rect(fx, fy + Mathf.Sin(Time.unscaledTime * 3f + i) * px(4f), anchoFicha, altoFicha);
                EstiloShow.Panel(ficha, Color.Lerp(color, EstiloShow.MoradoOscuro, 0.25f).WithAlpha(0.95f), altoFicha * 0.5f);
                string mando = j.currentControlScheme == "Teclado" ? "Teclado" : "Mando";
                EstiloShow.Texto(new Rect(ficha.x, ficha.y - px(6f), ficha.width, ficha.height * 0.7f), NombresJugadores.DeLocal(i), tam(40f), Color.white);
                EstiloShow.Texto(new Rect(ficha.x, ficha.y + ficha.height * 0.48f, ficha.width, ficha.height * 0.45f),
                                 $"{mando} · equipo {(i % 2 == 0 ? "Rojo" : "Azul")}", tam(22f), Color.white, TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto);
                fx += anchoFicha + hueco;
            }
            if (jugadores.Count < 4)
                EstiloShow.Texto(new Rect(0, fy - px(52f), w, px(44f)), "¿Más jugadores? ¡Pulsad un botón en otro mando!", tam(28f),
                                 new Color(1f, 1f, 1f, 0.9f), TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto);

            // --- Ayuda de botones ---
            EstiloShow.Ayuda(new Rect(0, h - px(86f), w, px(50f)),
                ("Stick", EstiloShow.GlifoTecla, pantalla == Pantalla.Principal ? "elegir" : "elegir y cambiar"),
                ("A", EstiloShow.GlifoA, "aceptar"),
                ("START", EstiloShow.GlifoTecla, "empezar"));
        }

        /// <summary>Una fila de opciones: nombre a la izquierda y su valor (o una barra) a la derecha.</summary>
        void Fila(Rect r, int indice, string nombre, string valor, float? barra)
        {
            bool elegida = seleccion == indice;
            if (elegida) r = EstiloShow.Escalar(r, 1.03f + Mathf.Sin(Time.unscaledTime * 6f) * 0.01f);
            EstiloShow.Panel(r, elegida ? EstiloShow.Oro : EstiloShow.Morado.WithAlpha(0.88f), r.height * 0.5f);

            float m = r.height * 0.45f;
            int tamano = Mathf.RoundToInt(r.height * 0.46f);
            EstiloShow.Texto(new Rect(r.x + m, r.y, r.width * 0.55f, r.height), nombre, tamano, Color.white, TextAnchor.MiddleLeft);

            var zonaValor = new Rect(r.x + r.width * 0.56f, r.y, r.width * 0.44f - m, r.height);
            string flechaIzq = elegida ? "<" : "", flechaDer = elegida ? ">" : "";
            EstiloShow.Texto(new Rect(zonaValor.x - m * 0.2f, r.y, m, r.height), flechaIzq, tamano, Color.white);
            EstiloShow.Texto(new Rect(zonaValor.xMax - m * 0.8f, r.y, m, r.height), flechaDer, tamano, Color.white);
            var centroValor = new Rect(zonaValor.x + m * 0.6f, r.y, zonaValor.width - m * 1.2f, r.height);
            if (barra.HasValue)
            {
                var b = new Rect(centroValor.x, r.center.y - r.height * 0.17f, centroValor.width, r.height * 0.34f);
                EstiloShow.Barra(b, barra.Value, barra.Value <= 0.001f ? new Color(0.6f, 0.6f, 0.65f) : EstiloShow.Verde);
                if (barra.Value <= 0.001f)
                    EstiloShow.Texto(centroValor, "sin sonido", Mathf.RoundToInt(tamano * 0.8f), Color.white, TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto);
            }
            else
            {
                EstiloShow.Texto(centroValor, valor, tamano, elegida ? Color.white : EstiloShow.Oro);
            }
        }
    }
}
