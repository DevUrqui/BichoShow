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
        GUIStyle estiloTitulo, estiloOpcion, estiloPequeno;

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

        static string Barrita(float volumen)
        {
            int llenas = Mathf.RoundToInt(volumen * 5f);
            return llenas == 0 ? "sin sonido" : new string('|', llenas * 2) + new string('.', (5 - llenas) * 2);
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
            PrepararEstilos();
            float w = Screen.width, h = Screen.height;

            RondaBase.Texto(new Rect(0, h * 0.05f, w, h * 0.16f), "BICHO SHOW", estiloTitulo, new Color(1f, 0.85f, 0.3f));
            RondaBase.Texto(new Rect(0, h * 0.2f, w, h * 0.05f), "El concurso de bichos más torpe de la tele", estiloPequeno, Color.white);

            if (PlayerInput.all.Count == 0)
            {
                RondaBase.Texto(new Rect(0, h * 0.45f, w, h * 0.08f), "Pulsa cualquier botón del mando (o una tecla) para unirte", estiloOpcion, RondaBase.Parpadeo(Color.white));
                return;
            }

            string[] opciones = pantalla == Pantalla.Principal
                ? new[] { "Empezar el show", "Opciones", "Salir" }
                : new[]
                {
                    $"Bots en los huecos:  < {(bots ? "Sí" : "No")} >",
                    $"Habilidad de los bots:  < {NombresHabilidad[nivelHabilidad]} >",
                    $"Duración de cada ronda:  < {Duraciones[nivelDuracion]} s >",
                    $"Volumen de efectos:  < {Barrita(Sonidos.Volumen)} >",
                    $"Volumen de la música:  < {Barrita(Musica.Volumen)} >",
                    "Volver",
                };

            float y = h * 0.34f;
            for (int i = 0; i < opciones.Length; i++)
            {
                bool elegida = i == seleccion;
                var fila = new Rect(w * 0.3f, y, w * 0.4f, h * 0.075f);
                if (elegida)
                {
                    var antes = GUI.color;
                    GUI.color = new Color(1f, 0.85f, 0.3f, 0.25f);
                    GUI.DrawTexture(fila, Texture2D.whiteTexture);
                    GUI.color = antes;
                }
                RondaBase.Texto(fila, (elegida ? "> " : "") + opciones[i], estiloOpcion, elegida ? new Color(1f, 0.9f, 0.4f) : Color.white);
                y += h * 0.085f;
            }

            // Jugadores unidos
            string lista = "";
            foreach (var entrada in PlayerInput.all) lista += $"   J{entrada.playerIndex + 1} ({entrada.currentControlScheme})";
            RondaBase.Texto(new Rect(0, h * 0.78f, w, h * 0.05f), $"Jugando:{lista}", estiloPequeno, Color.white);
            RondaBase.Texto(new Rect(0, h * 0.83f, w, h * 0.05f), "Más jugadores: pulsad un botón en otro mando   ·   1 y 3 = equipo Rojo, 2 y 4 = equipo Azul", estiloPequeno, new Color(1f, 1f, 1f, 0.75f));
            RondaBase.Texto(new Rect(0, h * 0.9f, w, h * 0.05f), "Stick: elegir   ·   A / START: aceptar   ·   izquierda / derecha: cambiar opción", estiloPequeno, new Color(1f, 1f, 1f, 0.6f));
        }

        void PrepararEstilos()
        {
            int tamano = Mathf.Max(12, Screen.height / 28);
            if (estiloTitulo == null)
            {
                estiloTitulo = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                estiloOpcion = new GUIStyle(estiloTitulo);
                estiloPequeno = new GUIStyle(estiloTitulo) { fontStyle = FontStyle.Normal };
            }
            estiloTitulo.fontSize = tamano * 4;
            estiloOpcion.fontSize = Mathf.RoundToInt(tamano * 1.3f);
            estiloPequeno.fontSize = Mathf.RoundToInt(tamano * 0.8f);
        }
    }
}
