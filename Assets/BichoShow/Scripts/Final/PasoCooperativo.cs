using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Una prueba del circuito de la final que la pareja tiene que superar junta.
    /// Sabe cuándo está superada y le dice a un bot a dónde ir según su papel en la pareja
    /// (rol 0 = el de número más bajo, rol 1 = el otro).
    /// </summary>
    public abstract class PasoCooperativo : MonoBehaviour
    {
        public int equipo;
        [Tooltip("Lo que se le dice a los jugadores en pantalla mientras están en este paso.")]
        public string pista = "¡Cooperad!";
        [Tooltip("Cuando todos los de la pareja pasan de esta Z, el paso está superado.")]
        public float zFin;

        public virtual bool Completado(List<ControladorBicho> miembros)
        {
            if (miembros.Count == 0) return false;
            foreach (var m in miembros)
                if (m.transform.position.z < zFin) return false;
            return true;
        }

        /// <summary>Vuelve a dejar el paso como al principio (para repetir la ronda).</summary>
        public virtual void Reiniciar() { }

        /// <summary>Punto al que debe ir el bot y con qué intensidad de stick (0 = quieto).</summary>
        public abstract Vector3 ObjetivoBot(ControladorBicho bot, int rol, ControladorBicho companero, out float intensidad);

        protected static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
