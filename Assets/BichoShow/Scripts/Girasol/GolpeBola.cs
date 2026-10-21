using UnityEngine;

namespace BichoShow
{
    /// <summary>Avisa a BolasPolen cuando su bola choca con algo.</summary>
    public class GolpeBola : MonoBehaviour
    {
        [HideInInspector] public BolasPolen dueno;
        Rigidbody rb;

        void Awake() => rb = GetComponent<Rigidbody>();

        void OnCollisionEnter(Collision c) { if (dueno != null) dueno.Golpear(rb, c); }
        void OnCollisionStay(Collision c) { if (dueno != null) dueno.Golpear(rb, c); }
    }
}
