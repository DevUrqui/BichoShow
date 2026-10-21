using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Despensa Deslizante", zona 1: salero de cristal gigante en el borde del mantel. Es un
    /// tope que rebota a quien lo toca y, cada pocos segundos, tiembla y esparce un círculo de
    /// sal alrededor: ahí se patina. Al final de la carrera (el público golpea las gradas)
    /// tiembla el doble de seguido.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SaleroGigante : MonoBehaviour
    {
        public float periodo = 4f;
        [Tooltip("Periodo cuando el público golpea las gradas (los últimos segundos).")]
        public float periodoPrisa = 2f;
        [Range(0f, 1f)] public float desfase;
        [Tooltip("Lo que tiembla antes de soltar la sal (s).")]
        public float aviso = 0.6f;
        public float radioSal = 3.5f;
        public float duracionSal = 2.5f;
        [Range(0f, 1f)] public float agarreSal = 0.15f;
        public float empujon = 7f;
        [Tooltip("Lo que se ve (tiembla).")]
        public Transform visual;
        [Tooltip("Círculo de sal en el suelo (se enciende mientras resbala).")]
        public GameObject circuloSal;
        [Tooltip("Granos de sal que salen al sacudirse.")]
        public ParticleSystem granos;

        /// <summary>El público golpea las gradas: todos los saleros, el doble de seguido.</summary>
        public static bool Prisa;

        Rigidbody rb;
        Vector3 posicionVisual;
        float siguiente, finSal = -1f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            if (visual != null) posicionVisual = visual.localPosition;
            if (circuloSal != null) circuloSal.SetActive(false);
            siguiente = Time.time + periodo * (1f + desfase);
        }

        void Update()
        {
            float t = Time.time;
            float falta = siguiente - t;
            if (visual != null)
            {
                float temblor = falta < aviso ? 0.12f : 0f;
                visual.localPosition = posicionVisual + new Vector3(Mathf.Sin(t * 70f), 0f, Mathf.Cos(t * 63f)) * temblor;
            }
            if (falta <= 0f)
            {
                siguiente = t + (Prisa ? periodoPrisa : periodo);
                finSal = t + duracionSal;
                if (circuloSal != null) circuloSal.SetActive(true);
                if (granos != null) granos.Play();
                Sonidos.Reproducir(Sonido.Mecanismo, transform.position, 0.5f, 1.6f);
            }
            if (finSal > 0f && t >= finSal)
            {
                finSal = -1f;
                if (circuloSal != null) circuloSal.SetActive(false);
            }
        }

        void FixedUpdate()
        {
            if (finSal < 0f) return;
            Vector3 c = transform.position;
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado || !b.EnSuelo) continue;
                Vector3 p = b.transform.position;
                if (Mathf.Abs(p.y - c.y) > 2f) continue;
                if (new Vector2(p.x - c.x, p.z - c.z).sqrMagnitude < radioSal * radioSal) b.Resbalar(agarreSal, 0.4f);
            }
        }

        void OnCollisionEnter(Collision choque)
        {
            var cuerpo = choque.rigidbody;
            if (cuerpo == null || !cuerpo.TryGetComponent(out ControladorBicho bicho) || bicho.Retirado) return;
            Vector3 fuera = cuerpo.position - transform.position;
            fuera.y = 0f;
            cuerpo.linearVelocity = fuera.normalized * empujon + Vector3.up * 2.5f;
            bicho.Tropezar(0.4f);
            Sonidos.Reproducir(Sonido.Muelle, cuerpo.position, 0.8f, 1.3f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Limpiar() => Prisa = false;
    }
}
