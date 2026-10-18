using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Base de toda trampa que un saboteador puede activar. Lleva la cuenta de quién la usó
    /// por última vez (para darle los puntos de lo que provoque), el tiempo de espera entre
    /// usos y un destello de aviso al activarse.
    /// </summary>
    public abstract class TrampaSaboteable : MonoBehaviour
    {
        /// <summary>Todas las trampas activas de la escena.</summary>
        public static readonly List<TrampaSaboteable> Todas = new List<TrampaSaboteable>();

        /// <summary>
        /// Con saboteadores en juego, las trampas automáticas (empujadores) dejan de ir solas
        /// y solo se mueven cuando alguien las activa.
        /// </summary>
        public static bool ModoManual;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos()
        {
            Todas.Clear();
            ModoManual = false;
        }

        [Header("Sabotaje")]
        [Tooltip("Nombre que ve el saboteador. Si está vacío, se usa el del objeto.")]
        public string nombreTrampa;
        [Tooltip("Segundos entre dos activaciones.")]
        public float enfriamiento = 6f;
        [Tooltip("Lo que pase a esta distancia (m) poco después de activarla cuenta como obra del saboteador.")]
        public float radioEfecto = 7f;
        [Tooltip("Altura del rombo del saboteador sobre la trampa.")]
        public float alturaMarcador = 3f;
        [Tooltip("Renderers que destellan al activarla.")]
        public Renderer[] renderersAviso;

        public string Nombre => string.IsNullOrEmpty(nombreTrampa) ? gameObject.name : nombreTrampa;
        public float UltimoUso { get; private set; } = -100f;
        public Saboteador UltimoSaboteador { get; private set; }
        public bool Lista => Time.time - UltimoUso >= enfriamiento;
        public float TiempoParaLista => Mathf.Max(0f, enfriamiento - (Time.time - UltimoUso));

        /// <summary>Centro de la zona donde la trampa hace daño.</summary>
        public virtual Vector3 CentroEfecto => transform.position;
        public Vector3 PuntoMarcador => CentroEfecto + Vector3.up * alturaMarcador;

        float finDestello;
        Material[] materialesAviso;
        Color[] coloresAviso;
        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");

        protected virtual void OnEnable() => Todas.Add(this);
        protected virtual void OnDisable() => Todas.Remove(this);

        /// <summary>Activa la trampa si no está en espera. Devuelve si se activó.</summary>
        public bool IntentarActivar(Saboteador quien)
        {
            if (!Lista) return false;
            UltimoUso = Time.time;
            UltimoSaboteador = quien;
            finDestello = Time.time + 0.4f;
            Sonidos.Reproducir(SonidoAlActivar, CentroEfecto);
            AlActivar();
            return true;
        }

        /// <summary>Lo que suena cuando el saboteador la activa.</summary>
        protected virtual Sonido SonidoAlActivar => Sonido.Alarma;

        protected abstract void AlActivar();

        protected virtual void Update()
        {
            if (renderersAviso == null || renderersAviso.Length == 0) return;

            if (materialesAviso == null)
            {
                materialesAviso = new Material[renderersAviso.Length];
                coloresAviso = new Color[renderersAviso.Length];
                for (int i = 0; i < renderersAviso.Length; i++)
                {
                    if (renderersAviso[i] == null) continue;
                    materialesAviso[i] = renderersAviso[i].material;
                    coloresAviso[i] = Leer(materialesAviso[i]);
                }
            }

            bool destellando = Time.time < finDestello;
            bool blanco = destellando && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f;
            for (int i = 0; i < materialesAviso.Length; i++)
                if (materialesAviso[i] != null)
                    Escribir(materialesAviso[i], blanco ? Color.white : coloresAviso[i]);
        }

        static Color Leer(Material m) => m.HasProperty(IdBaseColor) ? m.GetColor(IdBaseColor) : m.color;

        static void Escribir(Material m, Color c)
        {
            if (m.HasProperty(IdBaseColor)) m.SetColor(IdBaseColor, c);
            else m.color = c;
        }
    }
}
