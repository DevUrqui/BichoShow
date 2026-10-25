using System;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "El Cortacésped Mecha": perno gigante que cae del cielo. Primero sale una diana roja en el
    /// césped (latiendo, cada vez más deprisa); luego el perno baja girando, se clava y tumba a los
    /// que estén dentro. Al rato se hunde y desaparece. El objeto está en el suelo, en el centro de
    /// la diana; lo que baja es su hijo "cuerpo".
    /// </summary>
    public class PernoGigante : MonoBehaviour
    {
        public Transform cuerpo;
        public Transform diana;
        [Tooltip("Lo que dura el aviso hasta que se clava (s).")]
        public float aviso = 1.2f;
        public float altura = 16f;
        [Tooltip("Radio del golpe (m).")]
        public float radio = 2.3f;

        [NonSerialized] public CortacespedMecha robot;

        /// <summary>Ya se ha clavado (la diana ya no es peligrosa).</summary>
        public bool Caido { get; private set; }

        float inicio, caidoEn;

        void OnEnable()
        {
            inicio = Time.time;
            Caido = false;
            if (cuerpo != null) cuerpo.localPosition = Vector3.up * altura;
            Sonidos.Reproducir(Sonido.Alarma, transform.position, 0.5f, 1.6f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time - inicio;
            if (!Caido)
            {
                // La diana late cada vez más deprisa; el perno empieza a bajar a mitad del aviso.
                float k = Mathf.Clamp01(t / Mathf.Max(0.1f, aviso));
                if (diana != null) diana.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, k) * (1f + Mathf.Sin(t * Mathf.Lerp(8f, 26f, k)) * 0.06f);
                float baja = Mathf.Clamp01((t - aviso * 0.5f) / (aviso * 0.5f));
                if (cuerpo != null)
                {
                    cuerpo.localPosition = Vector3.up * Mathf.Lerp(altura, 0f, baja * baja);
                    cuerpo.Rotate(0f, 420f * dt, 0f, Space.Self);
                }
                if (baja >= 1f) Clavar();
                return;
            }

            float desde = Time.time - caidoEn;
            if (diana != null) diana.localScale = Vector3.one * Mathf.Max(0.01f, 1f - desde * 3f);
            if (desde > 1.4f && cuerpo != null) cuerpo.localPosition += Vector3.down * 2.5f * dt;
            if (desde > 2.6f) Destroy(gameObject);
        }

        void Clavar()
        {
            Caido = true;
            caidoEn = Time.time;
            Vector3 p = transform.position;
            Efectos.Sacudir(0.25f);
            Efectos.Impacto(p + Vector3.up * 0.4f, new Color(1f, 0.55f, 0.2f), 1f);
            Efectos.Polvo(p, 1.6f);
            Sonidos.Reproducir(Sonido.Rotura, p, 1f, 0.55f);
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado) continue;
                Vector3 d = b.transform.position - p;
                float alto = d.y;
                d.y = 0f;
                if (d.magnitude > radio || alto > 2.2f || alto < -1f) continue;
                Vector3 fuera = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward;
                b.Cuerpo.linearVelocity = fuera * 7f + Vector3.up * 6f;
                b.Tropezar(1.1f);
                Sonidos.Reproducir(Sonido.Derribo, b.transform.position, b.EsBot ? 0.6f : 1f, 0.8f);
                if (robot != null) robot.Derribado(b);
            }
        }
    }
}
