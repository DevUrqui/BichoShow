using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "La Cumbre del Girasol": el arco de META, en el centro de la cabeza del girasol, que gira con
    /// ella. Solo cuenta cruzarlo de un lado a otro por dentro del arco (entre las patas): hay que
    /// colocarse delante de la boca del arco mientras da vueltas.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class MetaGiratoria : MonoBehaviour
    {
        [Tooltip("Media anchura de la boca del arco (m, en su X local).")]
        public float mediaBoca = 4f;

        readonly Dictionary<ControladorBicho, float> lado = new Dictionary<ControladorBicho, float>();

        void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        void OnTriggerStay(Collider otro)
        {
            var cuerpo = otro.attachedRigidbody;
            if (cuerpo == null || !cuerpo.TryGetComponent(out ControladorBicho bicho) || bicho.Retirado) return;
            Vector3 local = transform.InverseTransformPoint(cuerpo.position);
            if (Mathf.Abs(local.x) > mediaBoca) { lado.Remove(bicho); return; }
            if (lado.TryGetValue(bicho, out float antes) && Mathf.Sign(antes) != Mathf.Sign(local.z) && Mathf.Abs(local.z) > 0.05f)
            {
                lado.Remove(bicho);
                if (RondaCumbre.Instancia != null) RondaCumbre.Instancia.Llegar(bicho);
                return;
            }
            if (Mathf.Abs(local.z) > 0.3f) lado[bicho] = local.z;
        }

        void OnTriggerExit(Collider otro)
        {
            var cuerpo = otro.attachedRigidbody;
            if (cuerpo != null && cuerpo.TryGetComponent(out ControladorBicho bicho)) lado.Remove(bicho);
        }
    }
}
