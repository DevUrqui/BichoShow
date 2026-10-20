using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Menú del juego: Empezar el show · Vestuario · Opciones · Salir.
    /// Cualquier jugador unido lo maneja con el stick (arriba/abajo para elegir, izquierda/
    /// derecha para cambiar un valor) y A / START para aceptar. Las opciones se guardan.
    /// Mientras el menú está abierto, los bichos no se mueven (el stick es para el menú).
    /// </summary>
    public class MenuPrincipal : MonoBehaviour
    {
        enum Pantalla { Principal, Opciones, Vestuario }

        static readonly string[] NombresHabilidad = { "Torpes", "Normales", "Hábiles" };
        static readonly float[] ValoresHabilidad = { 0.25f, 0.55f, 0.85f };
        static readonly int[] Duraciones = { 60, 90, 120 };

        Pantalla pantalla = Pantalla.Principal;
        int seleccion;
        bool bots = true;
        int nivelHabilidad = 1;
        int nivelDuracion = 1;

        int jugadoresAntes;
        float ignorarHasta;
        readonly Dictionary<PlayerInput, Vector2> stickAnterior = new Dictionary<PlayerInput, Vector2>();

        void Start()
        {
            bots = PlayerPrefs.GetInt("BichoShow.Bots", 1) == 1;
            nivelHabilidad = Mathf.Clamp(PlayerPrefs.GetInt("BichoShow.Habilidad", 1), 0, 2);
            nivelDuracion = Mathf.Clamp(PlayerPrefs.GetInt("BichoShow.Duracion", 1), 0, 2);

            ControladorBicho.EntradaBloqueada = true;
            if (GestorJugadores.Instancia != null) GestorJugadores.Instancia.PermitirUnirse(true);
            Musica.Poner(Musica.Tema.Plato);
            Sonidos.Precargar();
        }


        int NumeroOpciones => pantalla == Pantalla.Principal ? 4 : 6;
        const int OpcionVolver = 5;

        void Update()
        {
            ControladorBicho.EntradaBloqueada = true;

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
                    case 1: EntrarVestuario(); break;
                    case 2: pantalla = Pantalla.Opciones; seleccion = 0; break;
                    case 3: Salir(); break;
                }
            }
            else
            {
                if (seleccion == OpcionVolver) { pantalla = Pantalla.Principal; seleccion = 2; }
                else CambiarValor(+1);
            }
        }

        // ---------------------------------------------------------------------
        // Vestuario: cada jugador elige con su mando, a la vez que los demás
        // ---------------------------------------------------------------------

        readonly Dictionary<PlayerInput, int> filaVestuario = new Dictionary<PlayerInput, int>();
        readonly HashSet<PlayerInput> listos = new HashSet<PlayerInput>();
        float salirVestuarioEn = -1f;

        void EntrarVestuario()
        {
            pantalla = Pantalla.Vestuario;
            filaVestuario.Clear();
            listos.Clear();
            salirVestuarioEn = -1f;
        }

        void SalirVestuario()
        {
            pantalla = Pantalla.Principal;
            seleccion = 1;
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

                if (!listo)
                {
                    if (stick.y > 0.6f && antes.y <= 0.6f) { fila = (fila + 1) % 2; Sonidos.Reproducir(Sonido.MenuMover); }
                    if (stick.y < -0.6f && antes.y >= -0.6f) { fila = (fila + 1) % 2; Sonidos.Reproducir(Sonido.MenuMover); }
                    if (stick.x > 0.6f && antes.x <= 0.6f) CambiarPrenda(entrada, fila, +1);
                    if (stick.x < -0.6f && antes.x >= -0.6f) CambiarPrenda(entrada, fila, -1);
                    filaVestuario[entrada] = fila;
                }

                var saltar = entrada.actions.FindAction("Saltar");
                var empezar = entrada.actions.FindAction("Empezar");
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

        void CambiarPrenda(PlayerInput entrada, int fila, int sentido)
        {
            if (!entrada.TryGetComponent(out Vestuario v)) return;
            int gorro = v.Gorro, complemento = v.Complemento;
            if (fila == 0) gorro = (gorro + sentido + Vestuario.Gorros.Length) % Vestuario.Gorros.Length;
            else complemento = (complemento + sentido + Vestuario.Complementos.Length) % Vestuario.Complementos.Length;
            v.Poner(gorro, complemento);
            Vestuario.Guardar(entrada.playerIndex, gorro, complemento);
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
            float alto = px(250f);
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
                    FilaPrenda(new Rect(panel.x + px(14f), panel.y + px(68f), panel.width - px(28f), px(56f)), "Gorro",
                               Vestuario.NombresGorros[v.Gorro], !listo && fila == 0);
                    FilaPrenda(new Rect(panel.x + px(14f), panel.y + px(130f), panel.width - px(28f), px(56f)), "Cara",
                               Vestuario.NombresComplementos[v.Complemento], !listo && fila == 1);
                }
                var boton = new Rect(panel.x + panel.width * 0.2f, panel.yMax - px(56f), panel.width * 0.6f, px(46f));
                EstiloShow.Panel(boton, listo ? EstiloShow.Verde : EstiloShow.Morado.WithAlpha(0.9f), boton.height * 0.5f, -1f, false);
                EstiloShow.Texto(boton, listo ? "¡LISTO!" : "A = listo", tam(26f), Color.white);
                x += ancho + hueco;
            }

            EstiloShow.Ayuda(new Rect(0, h - px(86f), w, px(50f)),
                ("Stick", EstiloShow.GlifoTecla, "elegir y cambiar"), ("A", EstiloShow.GlifoA, "listo"), ("START", EstiloShow.GlifoTecla, "volver"));
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

            // --- Opciones (columna a la izquierda: el escenario con los bichos queda a la vista) ---
            float x = w * 0.05f, ancho = px(pantalla == Pantalla.Principal ? 560f : 780f);
            float alto = px(pantalla == Pantalla.Principal ? 84f : 74f), paso = alto + px(pantalla == Pantalla.Principal ? 18f : 16f);
            float y = h * 0.36f;
            if (pantalla == Pantalla.Principal)
            {
                string[] opciones = { "¡Empezar el show!", "Vestuario", "Opciones", "Salir" };
                for (int i = 0; i < opciones.Length; i++)
                {
                    EstiloShow.Boton(new Rect(x, y, ancho, alto), opciones[i], i == seleccion, tam(46f));
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
                EstiloShow.Texto(new Rect(ficha.x, ficha.y - px(6f), ficha.width, ficha.height * 0.7f), $"J{i + 1}", tam(44f), Color.white);
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
