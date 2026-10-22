using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Costura a la fuga": carrete de hilo tumbado, con el eje a lo largo de la carrera, que
    /// rueda sobre sí mismo. Hay que correr por encima: el giro arrastra de lado a quien lo pisa
    /// (y el carrete es redondo: si te vas al borde, resbalas). De vez en cuando frena, tiembla
    /// y cambia de sentido. El pivote está en el eje.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarreteRodante : MonoBehaviour
    {
        [Tooltip("Lo que gira (el dibujo). La colisión es redonda, no hace falta girarla.")]
        public Transform visual;
        public float radio = 1.8f;
        public float largo = 8f;
        [Tooltip("Arrastre de lado a quien va encima (m/s).")]
        public float arrastre = 2.2f;
        [Tooltip("Cada cuánto cambia de sentido (s).")]
        public float periodoCambio = 4f;
        [Range(0f, 1f)] public float desfase;

        readonly Collider[] encima = new Collider[16];
        readonly HashSet<Rigidbody> movidos = new HashSet<Rigidbody>();
        float anguloVisual;
        bool avisado;

        /// <summary>Sentido de giro ahora (-1 o 1) y cuánto gira (0 mientras cambia de sentido).</summary>
        float Giro(out bool cambiando)
        {
            float t = Mathf.Repeat(Time.time / periodoCambio + desfase, 2f);
            float sentido = t < 1f ? 1f : -1f;
            float dentro = Mathf.Repeat(t, 1f) * periodoCambio;
            cambiando = dentro > periodoCambio - 0.7f;
            float frena = Mathf.Clamp01((periodoCambio - dentro) / 0.7f);
            return sentido * Mathf.SmoothStep(0f, 1f, Mathf.Min(frena, Mathf.Clamp01(dentro / 0.5f)));
        }

        void Awake()
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        void FixedUpdate()
        {
            float giro = Giro(out bool cambiando);
            float dt = Time.fixedDeltaTime;

            // El dibujo rueda (y tiembla mientras cambia de sentido).
            anguloVisual += giro * arrastre / radio * Mathf.Rad2Deg * dt;
            if (visual != null)
                visual.localRotation = Quaternion.Euler(0f, 0f, anguloVisual + (cambiando ? Mathf.Sin(Time.time * 55f) * 1.5f : 0f));
            if (cambiando && !avisado) { avisado = true; Sonidos.Reproducir(Sonido.Mecanismo, transform.position, 0.4f, 0.8f); }
            else if (!cambiando) avisado = false;

            // Quien va encima se va de lado con el giro.
            Vector3 a = transform.position + transform.forward * (-largo * 0.5f) + Vector3.up * (radio + 0.5f);
            Vector3 b = transform.position + transform.forward * (largo * 0.5f) + Vector3.up * (radio + 0.5f);
            int n = Physics.OverlapCapsuleNonAlloc(a, b, 0.9f, encima, ~0, QueryTriggerInteraction.Ignore);
            movidos.Clear();
            for (int i = 0; i < n; i++)
            {
                var cuerpo = encima[i].attachedRigidbody;
                if (cuerpo == null || cuerpo.isKinematic || !movidos.Add(cuerpo)) continue;
                if (!cuerpo.TryGetComponent(out ControladorBicho bicho) || !bicho.EnSuelo) continue;
                cuerpo.position += transform.right * (-giro * arrastre * dt);
            }
        }
    }
}
