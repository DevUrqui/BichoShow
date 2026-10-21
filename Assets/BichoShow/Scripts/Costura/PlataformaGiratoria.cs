using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Costura a la fuga": botón gigante o carrete que gira sobre sí mismo (eje vertical) y
    /// arrastra a quien está encima. Hay que saltar al siguiente sin que el giro te saque.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlataformaGiratoria : MonoBehaviour
    {
        [Tooltip("Grados por segundo (negativo = al otro lado).")]
        public float velocidad = 30f;
        [Tooltip("Radio de la cara de arriba (para saber quién va encima).")]
        public float radio = 2f;
        [Tooltip("Altura de la cara de arriba respecto al pivote.")]
        public float alturaArriba;

        Rigidbody rb;
        readonly Collider[] encima = new Collider[16];
        readonly HashSet<Rigidbody> movidos = new HashSet<Rigidbody>();

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void FixedUpdate()
        {
            float grados = velocidad * Time.fixedDeltaTime;
            Quaternion paso = Quaternion.Euler(0f, grados, 0f);
            rb.MoveRotation(paso * rb.rotation);

            // Quien va encima gira con la plataforma (si no, se le escurriría por debajo).
            Vector3 centro = transform.position + Vector3.up * (alturaArriba + 0.6f);
            int n = Physics.OverlapCapsuleNonAlloc(centro, centro + Vector3.up * 0.4f, radio, encima, ~0, QueryTriggerInteraction.Ignore);
            movidos.Clear();
            for (int i = 0; i < n; i++)
            {
                var cuerpo = encima[i].attachedRigidbody;
                if (cuerpo == null || cuerpo == rb || cuerpo.isKinematic || !movidos.Add(cuerpo)) continue;
                if (!cuerpo.TryGetComponent(out ControladorBicho bicho) || !bicho.EnSuelo) continue;
                Vector3 desde = cuerpo.position - transform.position;
                Vector3 hasta = paso * desde;
                cuerpo.position += new Vector3(hasta.x - desde.x, 0f, hasta.z - desde.z);
            }
        }
    }
}
