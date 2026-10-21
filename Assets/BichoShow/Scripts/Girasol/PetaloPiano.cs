using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "La Cumbre del Girasol", sección 4: pétalo gigante que sube y baja a su ritmo, como una tecla
    /// de piano. Los pétalos del anillo van desfasados: hay que calcular el salto al siguiente.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PetaloPiano : MonoBehaviour, IEsperaBot
    {
        public float amplitud = 0.9f;
        public float periodo = 3f;
        [Range(0f, 1f)] public float desfase;

        [Header("Bots")]
        [Tooltip("El bot salta si, en el rato que tarda en llegar, el pétalo no queda más alto que esto respecto a él (m).")]
        public float subidaMaxima = 1.2f;

        Rigidbody rb;
        Vector3 inicio;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            inicio = transform.position;
        }

        /// <summary>Altura (del pivote) en el momento t.</summary>
        public float AlturaEn(float t) => inicio.y + amplitud * Mathf.Sin((t / periodo + desfase) * Mathf.PI * 2f);

        void FixedUpdate() => rb.MovePosition(new Vector3(inicio.x, AlturaEn(Time.time), inicio.z));

        /// <summary>Para los bots: salta a este pétalo cuando no va a quedar demasiado alto.</summary>
        public bool PuedoPasar(ControladorBicho bicho)
        {
            float y = bicho.transform.position.y;
            for (float dt = 0.4f; dt <= 1.1f; dt += 0.1f)
                if (AlturaEn(Time.time + dt) - y > subidaMaxima) return false;
            return true;
        }
    }
}
