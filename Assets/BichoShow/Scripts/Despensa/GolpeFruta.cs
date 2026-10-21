using UnityEngine;

namespace BichoShow
{
    /// <summary>Va en cada pieza de fruta: le pasa los choques a su lanzador (FrutaRodante).</summary>
    public class GolpeFruta : MonoBehaviour
    {
        public FrutaRodante dueno;
        Rigidbody rb;

        void Awake() => rb = GetComponent<Rigidbody>();

        void OnCollisionEnter(Collision choque)
        {
            if (dueno != null) dueno.Golpear(rb, choque);
        }
    }
}
