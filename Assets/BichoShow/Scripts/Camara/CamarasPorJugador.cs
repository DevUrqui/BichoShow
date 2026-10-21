using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace BichoShow
{
    /// <summary>
    /// En las carreras, cada jugador de este ordenador con su cámara (CamaraJugador) detrás de su
    /// bicho, como en Fall Guys. La pantalla se reparte: 1 jugador, entera; 2, arriba y abajo;
    /// 3 o 4, en cuartos (con 3, el cuarto hueco es la cámara del show, la de grupo). En un
    /// invitado del online, las cámaras siguen a sus marionetas. Mientras hay cámaras de jugador,
    /// la cámara de grupo (CamaraGrupo, en este mismo objeto) no se dibuja ni divide la pantalla.
    /// Lo añade CamaraGrupo cuando la escena es una carrera.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    [RequireComponent(typeof(CamaraGrupo))]
    public class CamarasPorJugador : MonoBehaviour
    {
        [Tooltip("Grosor de las líneas que separan las pantallas (píxeles a 1080).")]
        public float grosorLinea = 6f;

        struct Jugador { public ControladorBicho bicho; public PlayerInput entrada; public int local; }

        readonly List<Jugador> jugadores = new List<Jugador>();
        readonly List<CamaraJugador> camaras = new List<CamaraJugador>();
        Camera principal;
        AudioListener oidoPrincipal;
        string etiquetaPrincipal;
        bool activo;

        void Awake()
        {
            principal = GetComponent<Camera>();
            oidoPrincipal = GetComponent<AudioListener>();
            etiquetaPrincipal = gameObject.tag;
        }

        void OnDisable() => Volver();

        void OnDestroy()
        {
            foreach (var c in camaras) if (c != null) Destroy(c.gameObject);
            camaras.Clear();
        }

        void LateUpdate()
        {
            Recoger();
            int n = Mathf.Min(jugadores.Count, 4);
            if (n == 0) { Volver(); return; }

            while (camaras.Count < n) camaras.Add(Crear(camaras.Count));
            while (camaras.Count > n)
            {
                Destroy(camaras[camaras.Count - 1].gameObject);
                camaras.RemoveAt(camaras.Count - 1);
            }

            for (int i = 0; i < n; i++)
            {
                var c = camaras[i];
                var j = jugadores[i];
                if (c.bicho != j.bicho) { c.bicho = j.bicho; c.Colocar(); }
                c.entrada = j.entrada;
                c.local = j.local;
                c.Siguiendo = j.bicho.Retirado ? Espectar(j.bicho) : null;
                c.Camara.rect = Hueco(i, n);
                c.gameObject.tag = i == 0 ? "MainCamera" : "Untagged";
                if (c.TryGetComponent(out AudioListener oido)) oido.enabled = i == 0;
            }

            // La cámara del show: solo en el cuarto hueco cuando sois tres.
            principal.enabled = n == 3;
            if (n == 3) principal.rect = Hueco(3, 4);
            if (!activo)
            {
                activo = true;
                gameObject.tag = "Untagged";
                if (oidoPrincipal != null) oidoPrincipal.enabled = false;
                if (TryGetComponent(out DirectorCamaras director)) director.enabled = false;
                if (TryGetComponent(out CamaraSinEstorbos sinEstorbos)) sinEstorbos.enabled = false;
            }
        }

        /// <summary>Los jugadores de este ordenador, por orden (en un invitado, sus marionetas).</summary>
        void Recoger()
        {
            jugadores.Clear();
            if (RedShow.EsInvitado)
            {
                foreach (var m in RedShow.Marionetas)
                {
                    if (!RedShow.EsMia(m) || !m.TryGetComponent(out ControladorBicho b)) continue;
                    PlayerInput entrada = null;
                    foreach (var mando in MandoRemoto.Todos)
                        if (mando != null && mando.Local == m.Local) { entrada = mando.GetComponent<PlayerInput>(); break; }
                    jugadores.Add(new Jugador { bicho = b, entrada = entrada, local = m.Local });
                }
            }
            else
            {
                foreach (var b in ControladorBicho.Todos)
                {
                    if (b == null || b.EsBot || b.EsRemoto || b.EsMarioneta || !b.TryGetComponent(out PlayerInput entrada)) continue;
                    jugadores.Add(new Jugador { bicho = b, entrada = entrada, local = entrada.playerIndex });
                }
            }
            jugadores.Sort((a, b) => a.local.CompareTo(b.local));
        }

        /// <summary>Si su bicho está fuera (eliminado), mira al que va mejor de los que siguen.</summary>
        static ControladorBicho Espectar(ControladorBicho propio)
        {
            var ronda = FindAnyObjectByType<RondaRuta>();
            ControladorBicho mejor = null;
            float mejorAvance = float.MinValue;
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b == propio || b.Retirado) continue;
                float a = ronda != null ? ronda.AvanceDe(b.transform.position) : b.transform.position.z;
                if (a > mejorAvance) { mejorAvance = a; mejor = b; }
            }
            return mejor;
        }

        static Rect Hueco(int i, int n)
        {
            if (n <= 1) return new Rect(0f, 0f, 1f, 1f);
            if (n == 2) return i == 0 ? new Rect(0f, 0.5f, 1f, 0.5f) : new Rect(0f, 0f, 1f, 0.5f);
            // En cuartos: 1 arriba izquierda, 2 arriba derecha, 3 abajo izquierda, 4 abajo derecha.
            return new Rect(i % 2 == 0 ? 0f : 0.5f, i < 2 ? 0.5f : 0f, 0.5f, 0.5f);
        }

        CamaraJugador Crear(int i)
        {
            var go = new GameObject($"Camara del jugador {i + 1}");
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(principal);
            cam.enabled = true;
            // Los ajustes de URP (post-procesado, antialiasing, sombras) no se copian con CopyFrom.
            var datosPrincipal = principal.GetUniversalAdditionalCameraData();
            var datos = cam.GetUniversalAdditionalCameraData();
            datos.renderPostProcessing = datosPrincipal.renderPostProcessing;
            datos.antialiasing = datosPrincipal.antialiasing;
            datos.antialiasingQuality = datosPrincipal.antialiasingQuality;
            datos.renderShadows = datosPrincipal.renderShadows;
            go.AddComponent<AudioListener>().enabled = false;
            return go.AddComponent<CamaraJugador>();
        }

        /// <summary>Sin jugadores con cámara: vuelve la cámara de grupo como siempre.</summary>
        void Volver()
        {
            foreach (var c in camaras) if (c != null) Destroy(c.gameObject);
            camaras.Clear();
            if (!activo) return;
            activo = false;
            if (principal != null) { principal.enabled = true; principal.rect = new Rect(0f, 0f, 1f, 1f); }
            gameObject.tag = string.IsNullOrEmpty(etiquetaPrincipal) ? "MainCamera" : etiquetaPrincipal;
            if (oidoPrincipal != null) oidoPrincipal.enabled = true;
            if (TryGetComponent(out DirectorCamaras director)) director.enabled = true;
            if (TryGetComponent(out CamaraSinEstorbos sinEstorbos)) sinEstorbos.enabled = true;
        }

        void OnGUI()
        {
            int n = camaras.Count;
            if (n < 2) return;
            // Con GUI directamente (no EstiloShow): es de esta pantalla, no se manda a los invitados.
            if (Event.current.type != EventType.Repaint) return;
            float g = grosorLinea * Screen.height / 1080f;
            var antes = GUI.color;
            GUI.color = new Color(0.08f, 0.06f, 0.12f, 1f);
            GUI.DrawTexture(new Rect(0f, Screen.height * 0.5f - g * 0.5f, Screen.width, g), Texture2D.whiteTexture);
            if (n >= 3) GUI.DrawTexture(new Rect(Screen.width * 0.5f - g * 0.5f, 0f, g, Screen.height), Texture2D.whiteTexture);
            GUI.color = antes;
        }
    }
}
