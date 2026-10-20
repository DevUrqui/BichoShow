using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Costura a la fuga", zona 3: panel rígido forrado de tela del Puente Acordeón. Se queda
    /// plano un rato, avisa (tiembla y hace clic), se inclina hacia un lado, aguanta y vuelve.
    /// Nunca desaparece. Cada panel va un poco más tarde que el anterior: quien entra justo
    /// cuando el primero se pone plano puede cruzar todo el puente "en ola verde".
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PanelAcordeon : MonoBehaviour, IEsperaBot
    {
        [Tooltip("Grados que se inclina (sobre el eje de la carrera).")]
        public float anguloMaximo = 30f;
        [Tooltip("1 = se inclina a un lado, -1 = al otro.")]
        public float sentido = 1f;
        [Tooltip("Segundos de un ciclo completo.")]
        public float periodo = 4.5f;
        [Tooltip("Retraso respecto al primer panel (s).")]
        public float retraso;
        public float tiempoPlano = 2.4f;
        public float tiempoInclinar = 0.6f;
        public float tiempoInclinado = 0.7f;
        [Tooltip("Segundos de temblor antes de inclinarse.")]
        public float tiempoAviso = 0.5f;

        [Header("Bots")]
        [Tooltip("El bot salta a este panel si va a seguir plano al menos este tiempo (s).")]
        public float ventanaBot = 1.3f;

        Rigidbody rb;
        Quaternion giroInicial;
        int ultimoCicloAvisado = -1;

        float TiempoVuelta => Mathf.Max(0.1f, periodo - tiempoPlano - tiempoInclinar - tiempoInclinado);

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            giroInicial = transform.rotation;
        }

        /// <summary>Inclinación (grados) en el momento t. Durante el aviso, un temblor pequeño.</summary>
        public float AnguloEn(float t)
        {
            float f = Mathf.Repeat(t - retraso, periodo);
            if (f < tiempoPlano)
                return f > tiempoPlano - tiempoAviso ? Mathf.Sin(f * 55f) * 2.5f : 0f;
            f -= tiempoPlano;
            float maximo = anguloMaximo * Mathf.Sign(sentido);
            if (f < tiempoInclinar) return maximo * Mathf.SmoothStep(0f, 1f, f / tiempoInclinar);
            f -= tiempoInclinar;
            if (f < tiempoInclinado) return maximo;
            f -= tiempoInclinado;
            return maximo * (1f - Mathf.SmoothStep(0f, 1f, f / TiempoVuelta));
        }

        void FixedUpdate()
        {
            float t = Time.time;
            rb.MoveRotation(giroInicial * Quaternion.Euler(0f, 0f, AnguloEn(t)));

            // Clic al empezar a temblar (una vez por ciclo).
            float f = Mathf.Repeat(t - retraso, periodo);
            int ciclo = Mathf.FloorToInt((t - retraso) / periodo);
            if (f > tiempoPlano - tiempoAviso && f < tiempoPlano && ciclo != ultimoCicloAvisado)
            {
                ultimoCicloAvisado = ciclo;
                Sonidos.Reproducir(Sonido.Click, transform.position, 0.35f, 0.9f);
            }
        }

        /// <summary>Para los bots: ¿va a estar plano el rato suficiente para pisarlo?</summary>
        public bool PuedoPasar(ControladorBicho bicho)
        {
            float t = Time.time;
            for (float dt = 0f; dt <= ventanaBot; dt += 0.1f)
                if (Mathf.Abs(AnguloEn(t + dt)) > 5f) return false;
            return true;
        }
    }
}
