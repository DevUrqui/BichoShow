using UnityEngine;

namespace BichoShow
{
    /// <summary>Una hoja de las tijeras: avisa al chocar con un bicho.</summary>
    public class HojaTijera : MonoBehaviour
    {
        [HideInInspector] public TijerasGigantes tijeras;

        void OnCollisionEnter(Collision c) => Avisar(c);
        void OnCollisionStay(Collision c) => Avisar(c);

        void Avisar(Collision c)
        {
            if (tijeras == null || c.rigidbody == null || !c.rigidbody.TryGetComponent(out ControladorBicho bicho)) return;
            tijeras.Golpear(bicho, c.contactCount > 0 ? c.GetContact(0).point : bicho.transform.position);
        }
    }
}
