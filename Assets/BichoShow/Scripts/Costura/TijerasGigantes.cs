using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Costura a la fuga": tijeras gigantes de pie sobre el puente, con el eje arriba, en el
    /// centro. Abiertas, las hojas bajan en diagonal hacia los lados: se pasa por el centro.
    /// Cerradas, las dos hojas cuelgan juntas en el centro: se pasa por los lados. Antes de
    /// cerrarse tiemblan y suena un clic; el tijeretazo es rápido y lanza a quien pilla hacia
    /// fuera (y a lo mejor, fuera del puente).
    /// </summary>
    public class TijerasGigantes : MonoBehaviour, IEsperaBot
    {
        [Tooltip("Pivote de cada hoja (gira alrededor de su Z local). Con un Rigidbody cinemático.")]
        public Rigidbody hojaIzq, hojaDer;
        [Tooltip("Ángulo de cada hoja respecto a la vertical cuando están abiertas (grados).")]
        public float anguloAbierto = 40f;
        public float pausaAbierta = 2.2f;
        public float pausaCerrada = 1.2f;
        public float tiempoCerrar = 0.22f;
        public float tiempoAbrir = 0.7f;
        [Tooltip("Aviso antes de cerrar (tiemblan).")]
        public float aviso = 0.5f;
        [Range(0f, 1f)] public float desfase;
        public float empujon = 9f;

        readonly Dictionary<ControladorBicho, float> ultimoGolpe = new Dictionary<ControladorBicho, float>();
        Quaternion giroIzq, giroDer;
        bool avisado;

        float Periodo => pausaAbierta + tiempoCerrar + pausaCerrada + tiempoAbrir;
        float Fase => Mathf.Repeat(Time.time + desfase * Periodo, Periodo);

        /// <summary>0 = cerradas, 1 = abiertas del todo.</summary>
        public float Apertura
        {
            get
            {
                float t = Fase;
                if (t < pausaAbierta) return 1f;
                t -= pausaAbierta;
                if (t < tiempoCerrar) return 1f - t / tiempoCerrar;
                t -= tiempoCerrar;
                if (t < pausaCerrada) return 0f;
                t -= pausaCerrada;
                return Mathf.SmoothStep(0f, 1f, t / tiempoAbrir);
            }
        }

        /// <summary>Se están cerrando (el golpe).</summary>
        public bool Cerrando
        {
            get
            {
                float t = Fase - pausaAbierta;
                return t >= 0f && t < tiempoCerrar;
            }
        }

        void Awake()
        {
            if (hojaIzq != null) { giroIzq = hojaIzq.transform.localRotation; hojaIzq.isKinematic = true; hojaIzq.interpolation = RigidbodyInterpolation.Interpolate; }
            if (hojaDer != null) { giroDer = hojaDer.transform.localRotation; hojaDer.isKinematic = true; hojaDer.interpolation = RigidbodyInterpolation.Interpolate; }
            foreach (var h in GetComponentsInChildren<HojaTijera>()) h.tijeras = this;
        }

        void FixedUpdate()
        {
            float t = Fase;
            float temblor = 0f;
            if (t > pausaAbierta - aviso && t < pausaAbierta)
            {
                temblor = Mathf.Sin(Time.time * 60f) * 2.5f;
                if (!avisado)
                {
                    avisado = true;
                    Sonidos.Reproducir(Sonido.Click, transform.position, 0.6f, 0.8f);
                }
            }
            else if (t < pausaAbierta - aviso) avisado = false;

            float angulo = anguloAbierto * Apertura + temblor;
            Mover(hojaIzq, giroIzq, -angulo);
            Mover(hojaDer, giroDer, angulo);
            if (Cerrando && t - pausaAbierta < Time.fixedDeltaTime * 1.5f)
                Sonidos.Reproducir(Sonido.Mecanismo, transform.position, 0.8f, 1.4f);
        }

        void Mover(Rigidbody hoja, Quaternion reposo, float angulo)
        {
            if (hoja == null) return;
            Quaternion local = reposo * Quaternion.Euler(0f, 0f, angulo);
            Transform padre = hoja.transform.parent;
            hoja.MoveRotation(padre != null ? padre.rotation * local : local);
        }

        /// <summary>Lo llama cada hoja al chocar: cerrándose, lanza al bicho hacia fuera.</summary>
        public void Golpear(ControladorBicho bicho, Vector3 punto)
        {
            if (bicho == null || bicho.Retirado || !Cerrando) return;
            if (ultimoGolpe.TryGetValue(bicho, out float antes) && Time.time - antes < 0.8f) return;
            ultimoGolpe[bicho] = Time.time;
            Vector3 fuera = transform.InverseTransformPoint(bicho.transform.position);
            float lado = Mathf.Abs(fuera.x) < 0.3f ? (Random.value < 0.5f ? -1f : 1f) : Mathf.Sign(fuera.x);
            Vector3 v = transform.right * (lado * empujon) + Vector3.up * 4f;
            bicho.Cuerpo.linearVelocity = v;
            bicho.Tropezar(0.8f);
            Sonidos.Reproducir(Sonido.Golpe, punto, bicho.EsBot ? 0.6f : 1f);
            Efectos.Impacto(punto, new Color(0.85f, 0.85f, 0.9f), 0.7f);
        }

        /// <summary>Los bots pasan por el centro si quedan abiertas un buen rato.</summary>
        public bool PuedoPasar(ControladorBicho bicho) => Fase < pausaAbierta - 1.1f;
    }
}
