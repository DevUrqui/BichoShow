using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Compuerta de los pasillos cruzados: se hunde en el suelo mientras alguien pisa su placa
    /// (que está en el pasillo de al lado) y vuelve a subir al soltarla. Con un solo jugador en
    /// el equipo, se queda abierta.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Compuerta : MonoBehaviour
    {
        public PlacaPresion placa;
        public int equipo;
        [Tooltip("Cuánto se hunde al abrirse (m).")]
        public float bajada = 3.2f;
        [Tooltip("Velocidad al abrirse y cerrarse (m/s).")]
        public float velocidad = 7f;
        [Tooltip("Segundos que sigue abierta tras soltar la placa.")]
        public float margen = 0.35f;

        public bool Abierta => apertura > 0.9f;

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
            bool solo = ronda != null && ronda.MiembrosDe(equipo) <= 1;
            if (solo || (placa != null && placa.Pulsada)) abiertaHasta = Time.time + margen;

            float objetivo = Time.time < abiertaHasta ? 1f : 0f;
            apertura = Mathf.MoveTowards(apertura, objetivo, velocidad / Mathf.Max(0.1f, bajada) * Time.fixedDeltaTime);
            rb.MovePosition(cerrada + Vector3.down * bajada * apertura);
        }
    }
}
