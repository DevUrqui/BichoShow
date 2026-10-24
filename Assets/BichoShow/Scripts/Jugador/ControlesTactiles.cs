using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using Toque = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace BichoShow
{
    /// <summary>
    /// Controles táctiles para el móvil: un mando virtual (un Gamepad del Input System) que se
    /// maneja con los dedos. El juego lo trata como un mando más: se une con A, maneja los menús
    /// y juega (también de saboteador o villano).
    ///  - Mitad izquierda de la pantalla: el stick aparece donde pones el dedo.
    ///  - Derecha: A (saltar / aceptar), X (lanzarse) y AGARRAR (el gatillo: mantener).
    ///  - Arriba a la derecha: START.
    /// Solo en el móvil (o en el editor, si se activa en el menú BichoShow), y se esconden si se
    /// conecta un mando de verdad. Se dibujan con GUI directamente, no con EstiloShow, para que no
    /// se manden a los invitados de una partida online.
    /// </summary>
    public class ControlesTactiles : MonoBehaviour
    {
        public static ControlesTactiles Instancia { get; private set; }
        /// <summary>El mando virtual (null si no hay controles táctiles).</summary>
        public static Gamepad Mando => Instancia != null ? Instancia.mando : null;

#if UNITY_EDITOR
        public const string ClaveEditor = "BichoShow.TactilEnEditor";
        static bool EnEditor => UnityEditor.EditorPrefs.GetBool(ClaveEditor, false);
#else
        static bool EnEditor => false;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Crear()
        {
            if (Instancia != null || (!Application.isMobilePlatform && !EnEditor)) return;
            var go = new GameObject("Controles tactiles");
            DontDestroyOnLoad(go);
            go.AddComponent<ControlesTactiles>();
        }

        enum Boton { A, X, Agarrar, Start }

        class Circulo
        {
            public Boton boton;
            public Vector2 centro;
            public float radio;
            public string texto;
            public Color color;
            public bool pulsado;
        }

        const int DedoStick = -1, DedoNada = -2;

        Gamepad mando;
        readonly Circulo[] botones =
        {
            new Circulo { boton = Boton.A, texto = "A", color = new Color(0.3f, 0.78f, 0.35f) },
            new Circulo { boton = Boton.X, texto = "X", color = new Color(0.25f, 0.55f, 0.95f) },
            new Circulo { boton = Boton.Agarrar, texto = "AGARRAR", color = new Color(1f, 0.72f, 0.2f) },
            new Circulo { boton = Boton.Start, texto = "START", color = new Color(0.55f, 0.5f, 0.65f) },
        };
        readonly Dictionary<int, int> dedos = new Dictionary<int, int>(); // dedo → botón (índice), stick o nada
        Vector2 origenStick, puntaStick, stick;
        bool stickActivo, visibles, habiaEstado;
        float radioStick, unidad;
        Rect segura;
        Texture2D disco, aro;
        GUIStyle estilo;

        void Awake()
        {
            Instancia = this;
            EnhancedTouchSupport.Enable();
            if (Application.isEditor) TouchSimulation.Enable(); // en el editor, el ratón hace de dedo
            mando = InputSystem.AddDevice<Gamepad>("Mando tactil");

            // En el móvil: 60 imágenes por segundo y que la pantalla no se apague jugando.
            if (Application.isMobilePlatform)
            {
                Application.targetFrameRate = 60;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
            disco = Textura(false);
            aro = Textura(true);
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            if (mando != null && mando.added) InputSystem.RemoveDevice(mando);
            if (Application.isEditor) TouchSimulation.Disable();
            EnhancedTouchSupport.Disable();
        }

        /// <summary>¿Hay algún mando de verdad? Entonces sobran los táctiles.</summary>
        bool HayMandoReal()
        {
            foreach (var g in Gamepad.all)
                if (g != mando) return true;
            return false;
        }

        void Update()
        {
            visibles = !HayMandoReal();
            Colocar();

            bool a = false, x = false, agarrar = false, start = false;
            stickActivo = false;
            var vistos = new HashSet<int>();
            if (visibles)
            {
                foreach (var t in Toque.activeTouches)
                {
                    int dedo = t.finger.index;
                    vistos.Add(dedo);
                    Vector2 p = AGui(t.screenPosition);
                    if (!dedos.TryGetValue(dedo, out int papel)) papel = dedos[dedo] = Asignar(p);
                    if (papel == DedoStick)
                    {
                        // El stick sigue al dedo si se pasa del borde (así se cambia de sentido enseguida).
                        Vector2 v = p - origenStick;
                        if (v.magnitude > radioStick) origenStick = p - v.normalized * radioStick;
                        puntaStick = p;
                        stickActivo = true;
                    }
                    else if (papel >= 0)
                    {
                        switch (botones[papel].boton)
                        {
                            case Boton.A: a = true; break;
                            case Boton.X: x = true; break;
                            case Boton.Agarrar: agarrar = true; break;
                            case Boton.Start: start = true; break;
                        }
                    }
                }
            }
            // Dedos que se han levantado.
            var fuera = new List<int>();
            foreach (var d in dedos.Keys) if (!vistos.Contains(d)) fuera.Add(d);
            foreach (var d in fuera) dedos.Remove(d);

            stick = Vector2.zero;
            if (stickActivo)
            {
                Vector2 v = (puntaStick - origenStick) / Mathf.Max(1f, radioStick);
                stick = Vector2.ClampMagnitude(new Vector2(v.x, -v.y), 1f); // en pantalla, la y va hacia abajo
                if (stick.magnitude < 0.12f) stick = Vector2.zero;
            }
            foreach (var b in botones)
                b.pulsado = b.boton == Boton.A ? a : b.boton == Boton.X ? x : b.boton == Boton.Agarrar ? agarrar : start;

            // El estado del mando virtual (si no hay nada que contar, uno vacío una sola vez).
            bool algo = a || x || agarrar || start || stick != Vector2.zero;
            if (!algo && !habiaEstado) return;
            habiaEstado = algo;
            var estado = new GamepadState { leftStick = stick, rightTrigger = agarrar ? 1f : 0f };
            if (a) estado = estado.WithButton(GamepadButton.South);
            if (x) estado = estado.WithButton(GamepadButton.West);
            if (start) estado = estado.WithButton(GamepadButton.Start);
            InputSystem.QueueStateEvent(mando, estado);
        }

        /// <summary>Qué hace un dedo nuevo: un botón si cae encima, el stick si cae a la izquierda.</summary>
        int Asignar(Vector2 p)
        {
            for (int i = 0; i < botones.Length; i++)
                if ((p - botones[i].centro).magnitude < botones[i].radio * 1.25f) return i;
            bool yaHayStick = false;
            foreach (var papel in dedos.Values) if (papel == DedoStick) yaHayStick = true;
            if (!yaHayStick && p.x < segura.x + segura.width * 0.45f && p.y > segura.y + unidad * 1.5f)
            {
                origenStick = puntaStick = p;
                return DedoStick;
            }
            return DedoNada;
        }

        /// <summary>Coloca los botones según la pantalla (dentro de la zona segura: sin muescas).</summary>
        void Colocar()
        {
            var s = Screen.safeArea;
            segura = new Rect(s.x, Screen.height - s.yMax, s.width, s.height);
            // Tamaño de un botón: unos 9 mm, si se sabe la densidad de la pantalla.
            float porDpi = Screen.dpi > 0f ? Screen.dpi * 0.36f : 0f;
            unidad = porDpi > 0f ? Mathf.Clamp(porDpi, Screen.height * 0.075f, Screen.height * 0.13f) : Screen.height * 0.1f;
            radioStick = unidad * 1.3f;

            Vector2 centroA = new Vector2(segura.xMax - unidad * 1.7f, segura.yMax - unidad * 1.7f);
            Poner(Boton.A, centroA, unidad * 1.1f);
            Poner(Boton.X, centroA + new Vector2(-unidad * 2.4f, unidad * 0.3f), unidad * 0.9f);
            Poner(Boton.Agarrar, centroA + new Vector2(-unidad * 0.8f, -unidad * 2.3f), unidad * 0.9f);
            Poner(Boton.Start, new Vector2(segura.xMax - unidad * 0.9f, segura.y + unidad * 0.8f), unidad * 0.55f);
        }

        void Poner(Boton boton, Vector2 centro, float radio)
        {
            foreach (var b in botones)
                if (b.boton == boton) { b.centro = centro; b.radio = radio; }
        }

        static Vector2 AGui(Vector2 pantalla) => new Vector2(pantalla.x, Screen.height - pantalla.y);

        // ---------------------------------------------------------------------
        // Dibujo
        // ---------------------------------------------------------------------

        void OnGUI()
        {
            if (!visibles || Event.current.type != EventType.Repaint) return;
            GUI.depth = -1000;
            if (estilo == null) estilo = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

            // Stick: donde está el dedo; si no, un fantasma donde se suele poner.
            Vector2 baseStick = stickActivo ? origenStick : new Vector2(segura.x + unidad * 2.2f, segura.yMax - unidad * 2.1f);
            Vector2 punta = stickActivo ? baseStick + new Vector2(stick.x, -stick.y) * radioStick : baseStick;
            Dibujar(aro, baseStick, radioStick, new Color(1f, 1f, 1f, stickActivo ? 0.55f : 0.22f));
            Dibujar(disco, baseStick, radioStick, new Color(0f, 0f, 0f, stickActivo ? 0.18f : 0.08f));
            Dibujar(disco, punta, unidad * 0.6f, new Color(1f, 1f, 1f, stickActivo ? 0.75f : 0.3f));

            foreach (var b in botones)
            {
                float r = b.radio * (b.pulsado ? 0.92f : 1f);
                Dibujar(disco, b.centro, r, b.color * new Color(1f, 1f, 1f, b.pulsado ? 0.95f : 0.55f));
                Dibujar(aro, b.centro, r, new Color(1f, 1f, 1f, b.pulsado ? 0.9f : 0.5f));
                estilo.fontSize = Mathf.RoundToInt(b.texto.Length > 1 ? r * 0.38f : r * 0.9f);
                estilo.normal.textColor = new Color(1f, 1f, 1f, b.pulsado ? 1f : 0.85f);
                GUI.Label(new Rect(b.centro.x - r, b.centro.y - r, r * 2f, r * 2f), b.texto, estilo);
            }
        }

        static void Dibujar(Texture2D tex, Vector2 centro, float radio, Color color)
        {
            var antes = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(centro.x - radio, centro.y - radio, radio * 2f, radio * 2f), tex);
            GUI.color = antes;
        }

        /// <summary>Un círculo blanco suave (o solo su borde).</summary>
        static Texture2D Textura(bool soloBorde)
        {
            const int n = 128;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x + 0.5f - n * 0.5f, y + 0.5f - n * 0.5f).magnitude / (n * 0.5f);
                    float a = Mathf.Clamp01((1f - d) * n * 0.25f);
                    if (soloBorde) a *= Mathf.Clamp01((d - 0.86f) * n * 0.25f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }
    }
}
