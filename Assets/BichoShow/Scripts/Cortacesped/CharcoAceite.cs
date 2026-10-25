using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "El Cortacésped Mecha": charco de aceite que deja una lata al reventar. Crece de golpe, hace
    /// resbalar a quien lo pisa (casi sin agarre) y, al rato, se encoge y desaparece.
    /// </summary>
    public class CharcoAceite : MonoBehaviour
    {
        [Tooltip("Radio del charco a tamaño completo (m).")]
        public float radio = 2.2f;
        public float dura = 7f;
        [Range(0f, 1f)] public float agarre = 0.12f;

        float nacio;
        Vector3 escala;
        float tamano;

        /// <summary>Radio que tiene ahora (para los bots).</summary>
        public float RadioActual => radio * tamano;

        void Awake() => escala = transform.localScale;

        void OnEnable()
        {
            nacio = Time.time;
            tamano = 0.2f;
            transform.localScale = escala * tamano;
        }

        void Update()
        {
            float t = Time.time - nacio;
            float k = t < 0.25f ? t / 0.25f : t > dura - 0.8f ? (dura - t) / 0.8f : 1f;
            tamano = Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(k));
            transform.localScale = new Vector3(escala.x * tamano, escala.y, escala.z * tamano);
            if (t > dura) Destroy(gameObject);
        }

        void FixedUpdate()
        {
            Vector3 c = transform.position;
            float r = RadioActual;
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado || !b.EnSuelo) continue;
                Vector3 d = b.transform.position - c;
                if (Mathf.Abs(d.y) > 1f) continue;
                d.y = 0f;
                if (d.magnitude < r) b.Resbalar(agarre, 0.35f);
            }
        }
    }
}
