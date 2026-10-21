using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Base de las pruebas de carrera (Costura a la fuga, La Cumbre del Girasol): un camino de
    /// PuntoRuta que siguen los bots (BotCarrera) y una forma de saber quién ya ha llegado.
    /// </summary>
    public abstract class RondaRuta : RondaBase
    {
        [Header("Ruta de los bots")]
        [Tooltip("Todos los puntos del camino de los bots.")]
        public PuntoRuta[] ruta;
        [Tooltip("Por dónde empiezan los bots.")]
        public PuntoRuta inicioRuta;

        /// <summary>Este bicho ya ha terminado la carrera (los bots dejan de correr).</summary>
        public abstract bool HaLlegado(ControladorBicho b);

        /// <summary>Cuánto ha avanzado un punto en la carrera (más = más cerca de la meta).</summary>
        protected virtual float Avance(Vector3 punto) => punto.z;

        public float AvanceDe(Vector3 punto) => Avance(punto);

        /// <summary>El punto del camino más cercano (prefiere los de delante): para seguir tras caerse o salir volando.</summary>
        public PuntoRuta PuntoRutaCercano(Vector3 posicion)
        {
            PuntoRuta mejor = inicioRuta;
            float mejorCoste = float.MaxValue;
            if (ruta == null) return mejor;
            float avanceAqui = Avance(posicion);
            foreach (var p in ruta)
            {
                if (p == null) continue;
                Vector3 d = p.transform.position - posicion;
                float coste = d.magnitude + (Avance(p.transform.position) < avanceAqui - 1f ? 8f : 0f) + Mathf.Max(0f, d.y - 1f) * 4f;
                if (coste < mejorCoste) { mejorCoste = coste; mejor = p; }
            }
            return mejor;
        }
    }
}
