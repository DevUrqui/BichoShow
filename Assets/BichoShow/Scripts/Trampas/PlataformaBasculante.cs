using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Plataforma sobre una bisagra que se inclina según dónde se pise.
    /// Si un saboteador la activa, se vuelve "loca": pierde el muelle que la endereza,
    /// recibe un golpe de giro y se bambolea unos segundos.
    /// </summary>
    [RequireComponent(typeof(HingeJoint))]
    public class PlataformaBasculante : TrampaSaboteable
    {
        [Header("Locura")]
        [Tooltip("Golpe de giro al activarla (rad/s).")]
        public float impulsoGiro = 2.5f;
        [Tooltip("Fuerza del bamboleo mientras dura (rad/s²).")]
        public float bamboleo = 6f;
        public float duracionLocura = 2.5f;

        HingeJoint bisagra;
        Rigidbody rb;
        float finLocura = -1f;

        void Awake()
        {
            bisagra = GetComponent<HingeJoint>();
            rb = GetComponent<Rigidbody>();
        }

        protected override void AlActivar()
        {
            finLocura = Time.time + duracionLocura;
            bisagra.useSpring = false;
            float sentido = Random.value < 0.5f ? -1f : 1f;
            rb.AddTorque(Eje * impulsoGiro * sentido, ForceMode.VelocityChange);
        }

        Vector3 Eje => transform.TransformDirection(bisagra.axis);

        void FixedUpdate()
        {
            if (finLocura < 0f) return;

            if (Time.time >= finLocura)
            {
                bisagra.useSpring = true;
                finLocura = -1f;
                return;
            }

            rb.AddTorque(Eje * Mathf.Sin(Time.time * 6f) * bamboleo, ForceMode.Acceleration);
        }
    }
}
