using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Puerta que se hunde en el suelo solo si se pisan sus dos placas a la vez, y se queda
    /// abierta unos segundos. Con un solo jugador en el equipo, basta con una placa.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PuertaDoble : MonoBehaviour
    {
        public PlacaPresion placaA, placaB;
        public int equipo;
        public float tiempoAbierta = 5f;
        [Tooltip("Cuánto se hunde al abrirse (m).")]
        public float bajada = 3.4f;
        [Tooltip("Velocidad al abrirse y cerrarse (m/s).")]
        public float velocidad = 5f;

        public bool Abierta => apertura > 0.95f;
        public float SegundosAbierta => Mathf.Max(0f, abiertaHasta - Time.time);

        Rigidbody rb;
        Vector3 cerrada;
        float apertura;
        float abiertaHasta = -10f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            cerrada = transform.position;
        }

        void FixedUpdate()
        {
            var ronda = RondaFinal.Instancia;
            int necesarias = ronda != null ? Mathf.Clamp(ronda.MiembrosDe(equipo), 1, 2) : 2;
            int pulsadas = (placaA != null && placaA.Pulsada ? 1 : 0) + (placaB != null && placaB.Pulsada ? 1 : 0);
            if (pulsadas >= necesarias) abiertaHasta = Time.time + tiempoAbierta;

            float objetivo = Time.time < abiertaHasta ? 1f : 0f;
            apertura = Mathf.MoveTowards(apertura, objetivo, velocidad / Mathf.Max(0.1f, bajada) * Time.fixedDeltaTime);
            rb.MovePosition(cerrada + Vector3.down * bajada * apertura);
        }
    }
}
