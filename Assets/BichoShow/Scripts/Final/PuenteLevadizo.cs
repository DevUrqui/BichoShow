using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Puente que solo está bajado mientras alguien pisa una de sus placas (una a cada lado del
    /// hueco). Si nadie pisa, se levanta… con quien esté encima. Gira sobre el borde cercano.
    /// Si el equipo tiene un solo jugador, se queda bajado (no hay con quién cooperar).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PuenteLevadizo : MonoBehaviour
    {
        public PlacaPresion[] placas;
        [Tooltip("Equipo dueño de este carril (para saber cuántos son).")]
        public int equipo;
        public float anguloSubido = 75f;
        [Tooltip("Grados por segundo al subir y bajar.")]
        public float velocidad = 140f;

        public bool Bajado => angulo < 4f;

        Rigidbody rb;
        Quaternion rotacionBajado;
        float angulo;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rotacionBajado = transform.rotation;
            angulo = anguloSubido;
            transform.rotation = rotacionBajado * Quaternion.Euler(-angulo, 0f, 0f);
        }

        void FixedUpdate()
        {
            bool bajar = false;
            if (placas != null)
                foreach (var p in placas)
                    if (p != null && p.Pulsada) { bajar = true; break; }

            var ronda = RondaFinal.Instancia;
            if (ronda != null && ronda.MiembrosDe(equipo) <= 1) bajar = true;

            angulo = Mathf.MoveTowards(angulo, bajar ? 0f : anguloSubido, velocidad * Time.fixedDeltaTime);
            rb.MoveRotation(rotacionBajado * Quaternion.Euler(-angulo, 0f, 0f));
        }
    }
}
