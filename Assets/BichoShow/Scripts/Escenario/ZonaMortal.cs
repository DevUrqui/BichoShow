using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Trigger bajo el agua o el vacío: los bichos que caen reaparecen y los objetos frágiles
    /// se dan por rotos. Opcionalmente salpica en la superficie.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ZonaMortal : MonoBehaviour
    {
        [Tooltip("Lo que se dice en el mensaje cuando cae un objeto: \"se ha caído al río\"...")]
        public string motivoObjeto = "se ha caído al vacío";
        [Tooltip("Efecto opcional que se lanza en la superficie al caer algo.")]
        public ParticleSystem salpicadura;
        [Tooltip("Altura de la superficie donde aparece la salpicadura.")]
        public float alturaSuperficie = -3f;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider otro)
        {
            var cuerpo = otro.attachedRigidbody;
            if (cuerpo == null) return;

            var bicho = cuerpo.GetComponent<ControladorBicho>();
            var objeto = cuerpo.GetComponent<ObjetoFragil>();
            if (bicho == null && objeto == null) return;

            if (salpicadura != null)
            {
                Vector3 p = cuerpo.position;
                salpicadura.transform.position = new Vector3(p.x, alturaSuperficie, p.z);
                salpicadura.Play();
            }

            if (bicho != null) bicho.Reaparecer();
            if (objeto != null) objeto.Romper(motivoObjeto);
        }
    }
}
