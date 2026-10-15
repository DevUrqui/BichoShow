using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Trampa que gira sin parar (la barredora, las puertas giratorias...).
    /// Usa un Rigidbody cinemático para que la física calcule bien los golpes.
    /// Si un saboteador la activa, gira en sentido contrario y mucho más rápido un momento.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BarraGiratoria : TrampaSaboteable
    {
        [Header("Giro")]
        [Tooltip("Grados por segundo. Negativo para girar al revés.")]
        public float velocidadGiro = 90f;
        [Tooltip("Con sabotaje: multiplica la velocidad (negativo = cambia de sentido).")]
        public float multiplicadorTurbo = -2.5f;
        public float duracionTurbo = 2f;

        Rigidbody rb;
        float finTurbo;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        protected override void AlActivar() => finTurbo = Time.time + duracionTurbo;

        void FixedUpdate()
        {
            float velocidad = velocidadGiro * (Time.time < finTurbo ? multiplicadorTurbo : 1f);
            Quaternion paso = Quaternion.Euler(0f, velocidad * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(paso * rb.rotation);
        }
    }
}
