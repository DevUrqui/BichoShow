using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Plataforma que sube a la meta solo con los dos de la pareja encima (o uno, si el equipo
    /// es de uno). Arriba se queda mientras haya alguien; vacía, baja al rato.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Ascensor : MonoBehaviour
    {
        public int equipo;
        [Tooltip("Cuánto sube (m).")]
        public float subida = 3.7f;
        [Tooltip("Velocidad de subida y bajada (m/s).")]
        public float velocidad = 1.8f;
        [Tooltip("Tamaño de la zona encima de la plataforma donde se cuenta a los bichos.")]
        public Vector3 tamanoZona = new Vector3(3.8f, 2f, 3.8f);
        [Tooltip("Segundos vacío antes de volver a bajar.")]
        public float esperaBajar = 2f;

        public bool Arriba => nivel > 0.98f;
        public int Encima { get; private set; }
        public float AlturaArriba => abajo.y + subida;

        Rigidbody rb;
        Vector3 abajo;
        float nivel;
        float vacioDesde;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            abajo = transform.position;
        }

        void FixedUpdate()
        {
            float alturaTecho = transform.lossyScale.y * 0.5f;
            Encima = PlacaPresion.Contar(transform.position + Vector3.up * (alturaTecho + tamanoZona.y * 0.5f), tamanoZona * 0.5f, equipo);

            var ronda = RondaFinal.Instancia;
            int necesarios = ronda != null ? Mathf.Clamp(ronda.MiembrosDe(equipo), 1, 2) : 2;

            float objetivo = nivel;
            if (Encima >= necesarios) objetivo = 1f;
            if (Encima > 0) vacioDesde = Time.time;
            else if (Time.time - vacioDesde > esperaBajar) objetivo = 0f;

            nivel = Mathf.MoveTowards(nivel, objetivo, velocidad / Mathf.Max(0.1f, subida) * Time.fixedDeltaTime);
            rb.MovePosition(abajo + Vector3.up * subida * nivel);
        }
    }
}
