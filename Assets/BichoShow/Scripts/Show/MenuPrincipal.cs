using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Menú mínimo del juego: Empezar el show · Opciones · Salir.
    /// Cualquier jugador unido lo maneja con el stick (arriba/abajo para elegir, izquierda/
    /// derecha para cambiar un valor) y A / START para aceptar. Las opciones se guardan.
    /// Mientras el menú está abierto, los bichos no se mueven (el stick es para el menú).
    /// </summary>
    public class MenuPrincipal : MonoBehaviour
    {
        enum Pantalla { Principal, Opciones }

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


        int NumeroOpciones => pantalla == Pantalla.Principal ? 3 : 6;
        const int OpcionVolver = 5;

        void Update()
        {
            ControladorBicho.EntradaBloqueada = true;

            // Quien acaba de unirse ha pulsado un botón: que esa pulsación no elija nada.
            if (PlayerInput.all.Count != jugadoresAntes)
            {
                jugadoresAntes = PlayerInput.all.Count;
                ignorarHasta = Time.unscaledTime + 0.4f;
            }
            if (Time.unscaledTime < ignorarHasta || PlayerInput.all.Count == 0) return;

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
                    case 1: pantalla = Pantalla.Opciones; seleccion = 0; break;
                    case 2: Salir(); break;
                }
            }
            else
            {
                if (seleccion == OpcionVolver) { pantalla = Pantalla.Principal; seleccion = 1; }
                else CambiarValor(+1);
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

            // --- Opciones (columna a la izquierda: el escenario con los bichos queda a la vista) ---
            float x = w * 0.05f, ancho = px(pantalla == Pantalla.Principal ? 560f : 780f);
            float alto = px(pantalla == Pantalla.Principal ? 92f : 74f), paso = alto + px(pantalla == Pantalla.Principal ? 26f : 16f);
            float y = h * 0.36f;
            if (pantalla == Pantalla.Principal)
            {
                string[] opciones = { "¡Empezar el show!", "Opciones", "Salir" };
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
