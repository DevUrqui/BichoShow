using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "La Cumbre del Girasol", sección 2: hoja larga que hace de puente (atajo de una curva de la
    /// espiral a otra). Se inclina hacia el lado donde pesan más los bichos que van encima y
    /// vuelve sola a su sitio: si os subís todos por el mismo lado, al suelo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PuenteHoja : MonoBehaviour
    {
        [Tooltip("Tamaño (ancho, largo) de la hoja: el eje Z local va a lo largo.")]
        public Vector2 tamano = new Vector2(3f, 14f);
        [Tooltip("Grados por cada metro de desequilibrio (suma de lo apartados del centro que van).")]
        public float gradosPorMetro = 9f;
        public float inclinacionMaxima = 28f;
        [Tooltip("Lo que tarda en ir hacia la inclinación que toca (s).")]
        public float suavizado = 0.5f;

        Rigidbody rb;
        Quaternion giroInicial;
        float angulo, velocidadAngulo;
        readonly Collider[] encima = new Collider[16];

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            giroInicial = transform.rotation;
        }

        void FixedUpdate()
        {
            var medio = new Vector3(tamano.x * 0.5f + 0.3f, 0.8f, tamano.y * 0.5f);
            int n = Physics.OverlapBoxNonAlloc(transform.position + transform.up * 0.9f, medio, encima, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            float peso = 0f;
            for (int i = 0; i < n; i++)
            {
                var cuerpo = encima[i].attachedRigidbody;
                if (cuerpo == null || cuerpo == rb || !cuerpo.TryGetComponent(out ControladorBicho b) || b.Retirado) continue;
                peso += transform.InverseTransformPoint(cuerpo.position).x;
            }
            float objetivo = Mathf.Clamp(-peso * gradosPorMetro, -inclinacionMaxima, inclinacionMaxima);
            angulo = Mathf.SmoothDamp(angulo, objetivo, ref velocidadAngulo, suavizado, Mathf.Infinity, Time.fixedDeltaTime);
            float bamboleo = Mathf.Sin(Time.time * 1.7f) * 1.5f;
            rb.MoveRotation(giroInicial * Quaternion.Euler(0f, 0f, angulo + bamboleo));
        }
    }
}
