using UnityEngine;

namespace BichoShow
{
    /// <summary>La meta: el primero en cruzarla, primero en la carrera.</summary>
    [RequireComponent(typeof(Collider))]
    public class MetaCarrera : MonoBehaviour
    {
        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider otro)
        {
            var cuerpo = otro.attachedRigidbody;
            if (cuerpo == null || !cuerpo.TryGetComponent(out ControladorBicho bicho)) return;
            if (RondaCarrera.Activa != null) RondaCarrera.Activa.Llegar(bicho);
        }
    }
}
