using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Costura a la fuga", zona 5: alfiletero acolchado. Cuando alguien se sube, se hunde
    /// (rápido); cuando se queda vacío, recupera su altura despacio. Hundido, el salto a la
    /// terraza de arriba queda demasiado alto: hay que saltar enseguida, esperar a que suba o
    /// ir por la rampa del lado.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CojinBlando : MonoBehaviour
    {
        [Tooltip("Tamaño (ancho, fondo) de la cara de arriba, para ver quién está encima.")]
        public Vector2 tamanoArriba = new Vector2(12f, 7f);
        [Tooltip("Altura de la cara de arriba respecto al pivote del cojín.")]
        public float alturaArriba = 0.3f;
        [Tooltip("Cuánto se hunde con alguien encima (m).")]
        public float hundimiento = 0.8f;
        public float velocidadBajar = 1.6f;
        public float velocidadSubir = 0.3f;

        Rigidbody rb;
        Vector3 inicio;
        float bajado;
        bool ocupadoAntes;
        readonly Collider[] encima = new Collider[16];

        /// <summary>0 = a su altura, 1 = hundido del todo.</summary>
        public float Hundido01 => hundimiento > 0f ? bajado / hundimiento : 0f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            inicio = transform.position;
        }

        void FixedUpdate()
        {
            Vector3 centro = transform.position + Vector3.up * (alturaArriba + 0.6f);
            var medio = new Vector3(tamanoArriba.x * 0.5f, 0.6f, tamanoArriba.y * 0.5f);
            int n = Physics.OverlapBoxNonAlloc(centro, medio, encima, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            bool ocupado = false;
            for (int i = 0; i < n && !ocupado; i++)
            {
                var cuerpo = encima[i].attachedRigidbody;
                ocupado = cuerpo != null && cuerpo != rb && cuerpo.TryGetComponent(out ControladorBicho b) && !b.Retirado;
            }
            if (ocupado && !ocupadoAntes) Sonidos.Reproducir(Sonido.Muelle, transform.position, 0.3f, 0.7f);
            ocupadoAntes = ocupado;

            bajado = Mathf.MoveTowards(bajado, ocupado ? hundimiento : 0f, (ocupado ? velocidadBajar : velocidadSubir) * Time.fixedDeltaTime);
            rb.MovePosition(inicio - Vector3.up * bajado);
        }
    }
}
