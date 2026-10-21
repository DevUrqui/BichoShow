using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Despensa Deslizante", zona 5: tostada de la tostadora gigante. Está escondida dentro de su
    /// ranura (demasiado ancha para saltarla) y, cada poco, salta de golpe y se queda arriba un
    /// rato: es el puente para cruzar. Al saltar lanza hacia arriba a quien tenga encima; al
    /// bajar, quien siga encima cae dentro de la ranura. Antes de saltar suena un "tic".
    /// El pivote es la posición de arriba (a ras de la tostadora).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TostadaSaltarina : MonoBehaviour, IEsperaBot
    {
        [Tooltip("Lo que baja dentro de la ranura (m).")]
        public float profundidad = 3.5f;
        public float tiempoAbajo = 2.4f;
        public float tiempoArriba = 2f;
        [Tooltip("Lo que tarda en saltar (s): muy poco, de golpe.")]
        public float subida = 0.15f;
        public float bajada = 0.7f;
        [Range(0f, 1f)] public float desfase;
        [Tooltip("Velocidad hacia arriba con la que lanza a quien tenga encima al saltar (m/s).")]
        public float impulso = 7f;
        [Tooltip("Tamaño (ancho, largo) de la cara de arriba.")]
        public Vector2 tamano = new Vector2(8.4f, 5f);

        Rigidbody rb;
        Vector3 arriba;
        bool estabaSubiendo, avisado;
        readonly Collider[] encima = new Collider[12];
        readonly HashSet<Rigidbody> lanzados = new HashSet<Rigidbody>();

        float Ciclo => tiempoAbajo + subida + tiempoArriba + bajada;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            arriba = transform.position;
        }

        /// <summary>Lo que está por debajo de arriba en el momento t (0 = a ras, profundidad = escondida).</summary>
        public float HundidaEn(float t)
        {
            float c = Mathf.Repeat(t + desfase * Ciclo, Ciclo);
            if (c < tiempoAbajo) return profundidad;
            c -= tiempoAbajo;
            if (c < subida) return profundidad * (1f - c / subida);
            c -= subida;
            if (c < tiempoArriba) return 0f;
            c -= tiempoArriba;
            return profundidad * Mathf.SmoothStep(0f, 1f, c / bajada);
        }

        void FixedUpdate()
        {
            float t = Time.time;
            float hundida = HundidaEn(t);
            rb.MovePosition(arriba + Vector3.down * hundida);

            // Aviso: "tic" medio segundo antes de saltar.
            float c = Mathf.Repeat(t + desfase * Ciclo, Ciclo);
            bool casi = c > tiempoAbajo - 0.5f && c < tiempoAbajo;
            if (casi && !avisado) Sonidos.Reproducir(Sonido.Click, arriba, 0.7f, 1.8f);
            avisado = casi;

            // Al saltar, lanza hacia arriba a quien esté encima de la ranura.
            bool subiendo = c >= tiempoAbajo && c < tiempoAbajo + subida;
            if (subiendo && !estabaSubiendo)
            {
                Sonidos.Reproducir(Sonido.Muelle, arriba, 1f, 0.8f);
                Lanzar();
            }
            estabaSubiendo = subiendo;
        }

        void Lanzar()
        {
            var medio = new Vector3(tamano.x * 0.5f, 1.2f, tamano.y * 0.5f);
            int n = Physics.OverlapBoxNonAlloc(arriba + Vector3.up * 0.6f, medio, encima, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            lanzados.Clear();
            for (int i = 0; i < n; i++)
            {
                var cuerpo = encima[i].attachedRigidbody;
                if (cuerpo == null || cuerpo == rb || cuerpo.isKinematic || !lanzados.Add(cuerpo)) continue;
                if (!cuerpo.TryGetComponent(out ControladorBicho bicho) || bicho.Retirado) continue;
                var v = cuerpo.linearVelocity;
                cuerpo.linearVelocity = new Vector3(v.x, Mathf.Max(v.y, impulso), v.z);
            }
        }

        /// <summary>Para los bots: cruzar si está arriba y aún le queda rato arriba.</summary>
        public bool PuedoPasar(ControladorBicho bicho) => HundidaEn(Time.time) < 0.05f && HundidaEn(Time.time + 1.1f) < 0.05f;
    }
}
