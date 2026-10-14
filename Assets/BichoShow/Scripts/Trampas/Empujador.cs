using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Bloque que sale de golpe, se queda un momento y vuelve despacio. Antes de empujar
    /// parpadea para avisar: así el golpe se ve venir y da risa en vez de rabia.
    /// Usa un Rigidbody cinemático para empujar bien a bichos y objetos.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Empujador : MonoBehaviour
    {
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

        Rigidbody rb;
        Vector3 inicio;
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
            float fase = Fase(Time.time);
            rb.MovePosition(inicio + recorrido * Extension(fase));
        }

        void Update()
        {
            if (material == null) return;

            // Parpadeo en los últimos segundos antes de salir.
            float tiempoParaSalir = (1f - Fase(Time.time)) * periodo;
            bool avisando = tiempoParaSalir < tiempoAviso;
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
