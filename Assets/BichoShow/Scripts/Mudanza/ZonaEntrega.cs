using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Zona (trigger) dentro de la casa de un equipo. Cuando entra un objeto de ese equipo,
    /// avisa a la ronda para que sume los puntos, y lanza confeti.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ZonaEntrega : MonoBehaviour
    {
        [Tooltip("Equipo al que pertenece esta casa (0 o 1).")]
        public int equipo;
        public ParticleSystem confeti;

        /// <summary>Se lanza al entrar un objeto del equipo correcto.</summary>
        public event System.Action<ZonaEntrega, ObjetoFragil> AlEntrarObjeto;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider otro) => Comprobar(otro);
        void OnTriggerStay(Collider otro) => Comprobar(otro); // por si entró antes de empezar la ronda

        void Comprobar(Collider otro)
        {
            var objeto = otro.attachedRigidbody != null ? otro.attachedRigidbody.GetComponent<ObjetoFragil>() : null;
            if (objeto == null || objeto.Roto || objeto.Entregado || objeto.Equipo != equipo) return;
            AlEntrarObjeto?.Invoke(this, objeto);
        }

        public void Celebrar()
        {
            if (confeti != null) confeti.Play();
        }
    }
}
