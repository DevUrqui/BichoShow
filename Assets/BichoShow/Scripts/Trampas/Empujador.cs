using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Bloque que sale de golpe, se queda un momento y vuelve despacio. Antes de empujar
    /// parpadea para avisar: así el golpe se ve venir y da risa en vez de rabia.
    /// Usa un Rigidbody cinemático para empujar bien a bichos y objetos.
    ///
    /// Con saboteadores en juego (TrampaSaboteable.ModoManual) deja de ir solo: sale cuando
    /// lo activan, tras un aviso más corto.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Empujador : TrampaSaboteable
    {
        [Header("Movimiento")]
        [Tooltip("Hacia dónde sale, en coordenadas del mundo, y cuánto (m).")]
        public Vector3 recorrido = new Vector3(4.5f, 0f, 0f);
        [Tooltip("Duración de un ciclo completo (s).")]
        public float periodo = 4f;
        [Tooltip("Retraso del ciclo, para que varios empujadores no vayan a la vez (s).")]
        public float desfase;
        [Tooltip("Parte del ciclo que tarda en salir (0-1). Pequeño = golpe rápido.")]
        [Range(0.05f, 0.5f)] public float fraccionSalida = 0.25f;
        [Tooltip("Parte del ciclo que se queda fuera.")]
        [Range(0f, 0.5f)] public float fraccionFuera = 0.2f;
        [Tooltip("Parte del ciclo que tarda en volver.")]
        [Range(0.05f, 0.6f)] public float fraccionVuelta = 0.3f;
        [Tooltip("Segundos de parpadeo de aviso antes de salir.")]
        public float tiempoAviso = 0.6f;
        public Renderer cuerpoVisible;
        public Color colorAviso = Color.white;
        [Tooltip("Con saboteador: segundos de parpadeo entre activarlo y que salga.")]
        public float avisoManual = 0.35f;

        Rigidbody rb;
        Vector3 inicio;
        float inicioManual = -100f;

        public override Vector3 CentroEfecto =>
            (Application.isPlaying ? inicio : transform.position) + recorrido * 0.5f;

        protected override void AlActivar() => inicioManual = Time.time + avisoManual;
        Material material;
        Color colorNormal;

        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            inicio = transform.position;

            if (cuerpoVisible != null)
            {
                material = cuerpoVisible.material;
                colorNormal = material.HasProperty(IdBaseColor) ? material.GetColor(IdBaseColor) : material.color;
            }
        }

        void FixedUpdate()
        {
            float extension = ModoManual ? ExtensionManual() : Extension(Fase(Time.time));
            rb.MovePosition(inicio + recorrido * extension);
        }

        float ExtensionManual()
        {
            float fase = (Time.time - inicioManual) / Mathf.Max(0.1f, periodo);
            return fase < 0f || fase >= 1f ? 0f : Extension(fase);
        }

        protected override void Update()
        {
            base.Update();
            if (material == null) return;

            // Parpadeo justo antes de salir.
            bool avisando;
            if (ModoManual)
            {
                avisando = Time.time < inicioManual;
            }
            else
            {
                float tiempoParaSalir = (1f - Fase(Time.time)) * periodo;
                avisando = tiempoParaSalir < tiempoAviso;
            }
            Color c = avisando && Mathf.Repeat(Time.time * 8f, 1f) < 0.5f ? colorAviso : colorNormal;
            if (material.HasProperty(IdBaseColor)) material.SetColor(IdBaseColor, c);
            else material.color = c;
        }

        float Fase(float tiempo) => Mathf.Repeat((tiempo + desfase) / Mathf.Max(0.1f, periodo), 1f);

        /// <summary>0 = recogido, 1 = fuera del todo.</summary>
        float Extension(float fase)
        {
            if (fase < fraccionSalida) return Mathf.SmoothStep(0f, 1f, fase / fraccionSalida);
            fase -= fraccionSalida;
            if (fase < fraccionFuera) return 1f;
            fase -= fraccionFuera;
            if (fase < fraccionVuelta) return 1f - Mathf.SmoothStep(0f, 1f, fase / fraccionVuelta);
            return 0f;
        }
    }
}
