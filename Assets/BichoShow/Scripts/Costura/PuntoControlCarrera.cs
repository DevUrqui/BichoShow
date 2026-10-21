using UnityEngine;

namespace BichoShow
{
    /// <summary>Un obstáculo que sabe decirle a un bot si es buen momento para pasar.</summary>
    public interface IEsperaBot
    {
        bool PuedoPasar(ControladorBicho bicho);
    }

    /// <summary>
    /// Punto de control de una carrera: al cruzarlo, si te caes vuelves aquí (cada jugador en
    /// su sitio, a lo ancho). Solo cuenta hacia delante: un punto anterior no te hace retroceder.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PuntoControlCarrera : MonoBehaviour
    {
        [Tooltip("Orden en la carrera (1, 2, 3…). La salida es el 0.")]
        public int indice = 1;
        [Tooltip("Separación a lo ancho entre los sitios de reaparición de cada jugador (m).")]
        public float separacion = 1.6f;
        [Tooltip("Dónde se reaparece (suelo); si no hay, el propio punto.")]
        public Transform reaparicion;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        /// <summary>Sitio de reaparición de ese jugador.</summary>
        public Vector3 Reaparicion(int jugador)
        {
            var t = reaparicion != null ? reaparicion : transform;
            return t.position + t.right * ((jugador % 4 - 1.5f) * separacion);
        }

        public Quaternion Orientacion => (reaparicion != null ? reaparicion : transform).rotation;

        void OnTriggerEnter(Collider otro)
        {
            var cuerpo = otro.attachedRigidbody;
            if (cuerpo == null || !cuerpo.TryGetComponent(out ControladorBicho bicho)) return;
            if (RondaCarrera.Activa != null) RondaCarrera.Activa.PuntoAlcanzado(bicho, this);
        }
    }
}
