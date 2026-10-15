using UnityEngine;

namespace BichoShow
{
    /// <summary>Mancha de aceite (trigger): el bicho que la pisa casi no tiene agarre.</summary>
    [RequireComponent(typeof(Collider))]
    public class Charco : MonoBehaviour
    {
        [Range(0f, 1f)] public float agarre = 0.15f;

        void OnTriggerStay(Collider otro)
        {
            var bicho = otro.attachedRigidbody != null ? otro.attachedRigidbody.GetComponent<ControladorBicho>() : null;
            if (bicho != null) bicho.Resbalar(agarre, 0.35f);
        }
    }
}
