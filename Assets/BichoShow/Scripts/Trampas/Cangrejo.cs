using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Trampa de playa: un cangrejo gigante sale de la arena, cruza el campo de lado (como
    /// los cangrejos) arrollando bichos y pelota, y se vuelve a enterrar.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Cangrejo : TrampaSaboteable
    {
        [Header("Carrera")]
        [Tooltip("Por dónde sale (en el suelo, coordenadas del mundo).")]
        public Vector3 desde = new Vector3(-5f, 0f, -7f);
        [Tooltip("Por dónde se vuelve a enterrar.")]
        public Vector3 hasta = new Vector3(-5f, 0f, 7f);
        public float duracionCarrera = 2f;
        public float duracionAsomar = 0.45f;
        [Tooltip("Profundidad a la que espera enterrado.")]
        public float profundidad = 1.6f;
        [Tooltip("Pieza que se balancea al andar (opcional).")]
        public Transform cuerpo;

        Rigidbody rb;
        float inicio = -100f;

        public override Vector3 CentroEfecto => (desde + hasta) * 0.5f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Mira de lado al camino: anda como un cangrejo.
            Vector3 camino = hasta - desde;
            camino.y = 0f;
            if (camino.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, camino.normalized));
                rb.rotation = transform.rotation;
            }
            Colocar(desde - Vector3.up * profundidad);
        }

        protected override void AlActivar() => inicio = Time.time;

        void FixedUpdate()
        {
            float t = Time.time - inicio;
            float total = duracionAsomar * 2f + duracionCarrera;
            if (t < 0f || t > total)
            {
                rb.MovePosition(desde - Vector3.up * profundidad);
                return;
            }

            Vector3 punto;
            if (t < duracionAsomar)
            {
                punto = desde - Vector3.up * profundidad * (1f - t / duracionAsomar);           // asoma
            }
            else if (t < duracionAsomar + duracionCarrera)
            {
                float k = (t - duracionAsomar) / duracionCarrera;
                punto = Vector3.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, k));               // corre
            }
            else
            {
                float k = (t - duracionAsomar - duracionCarrera) / duracionAsomar;
                punto = hasta - Vector3.up * profundidad * k;                                   // se entierra
            }
            rb.MovePosition(punto);
        }

        protected override void Update()
        {
            base.Update();
            // Se balancea mientras corre.
            if (cuerpo != null)
                cuerpo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 18f) * 8f);
        }

        void Colocar(Vector3 punto)
        {
            transform.position = punto;
            if (rb != null) rb.position = punto;
        }
    }
}
