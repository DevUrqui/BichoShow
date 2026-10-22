using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Costura a la fuga": un acerico enorme del que salen alfileres por oleadas. Antes de salir,
    /// la cabeza del alfiler asoma y tiembla; luego sale de golpe y lanza por los aires a quien
    /// esté encima. Un rato después se vuelve a esconder. Arriba, los alfileres son postes que
    /// estorban. Las oleadas recorren el campo de lado a lado y en damero: hay que leerlas.
    /// </summary>
    public class CampoAlfileres : MonoBehaviour
    {
        [Tooltip("Cada alfiler (con su Rigidbody cinemático); el pivote, en el suelo del acerico.")]
        public Rigidbody[] alfileres;
        [Tooltip("En qué momento del ciclo sale cada uno (0-1).")]
        public float[] fases;
        public float periodo = 3.2f;
        [Tooltip("Lo que asoma, tiembla y avisa antes de salir (s).")]
        public float aviso = 0.6f;
        public float subida = 0.12f;
        public float arriba = 0.9f;
        public float bajada = 0.5f;
        [Tooltip("Altura del alfiler fuera (m) y escondido (m, negativo).")]
        public float alturaFuera = 1.5f, alturaDentro = -1.6f, alturaAsomado = -1.35f;
        public float lanzamiento = 9f;

        readonly Collider[] cerca = new Collider[16];
        readonly Dictionary<ControladorBicho, float> ultimo = new Dictionary<ControladorBicho, float>();
        Vector3[] inicio;
        float[] avisadoEn;

        void Awake()
        {
            inicio = new Vector3[alfileres.Length];
            avisadoEn = new float[alfileres.Length];
            for (int i = 0; i < alfileres.Length; i++)
            {
                inicio[i] = alfileres[i].transform.position;
                alfileres[i].isKinematic = true;
                alfileres[i].interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        /// <summary>Altura del alfiler i en este momento, y si está saliendo ahora.</summary>
        float Altura(int i, out bool saliendo, out bool avisando)
        {
            saliendo = avisando = false;
            float t = Mathf.Repeat(Time.time / periodo + (fases != null && i < fases.Length ? fases[i] : 0f), 1f) * periodo;
            // Ciclo: aviso → subida → arriba → bajada → escondido.
            if (t < aviso)
            {
                avisando = true;
                return alturaAsomado + Mathf.Sin(Time.time * 50f) * 0.04f;
            }
            t -= aviso;
            if (t < subida) { saliendo = true; return Mathf.Lerp(alturaAsomado, alturaFuera, t / subida); }
            t -= subida;
            if (t < arriba) return alturaFuera;
            t -= arriba;
            if (t < bajada) return Mathf.Lerp(alturaFuera, alturaDentro, t / bajada);
            return alturaDentro;
        }

        void FixedUpdate()
        {
            for (int i = 0; i < alfileres.Length; i++)
            {
                var a = alfileres[i];
                if (a == null) continue;
                float y = Altura(i, out bool saliendo, out bool avisando);
                a.MovePosition(inicio[i] + Vector3.up * y);
                if (avisando && Time.time - avisadoEn[i] > periodo * 0.5f)
                {
                    avisadoEn[i] = Time.time;
                    if (i % 3 == 0) Sonidos.Reproducir(Sonido.Click, inicio[i], 0.25f, 1.6f);
                }
                if (saliendo) Lanzar(inicio[i] + Vector3.up * (alturaAsomado + 0.25f)); // a ras del acerico
            }
        }

        void Lanzar(Vector3 pie)
        {
            int n = Physics.OverlapCapsuleNonAlloc(pie + Vector3.up * 0.2f, pie + Vector3.up * 1.6f, 0.55f, cerca, ~0, QueryTriggerInteraction.Ignore);
            for (int k = 0; k < n; k++)
            {
                var cuerpo = cerca[k].attachedRigidbody;
                if (cuerpo == null || cuerpo.isKinematic || !cuerpo.TryGetComponent(out ControladorBicho bicho) || bicho.Retirado) continue;
                if (ultimo.TryGetValue(bicho, out float antes) && Time.time - antes < 0.6f) continue;
                ultimo[bicho] = Time.time;
                Vector3 lado = bicho.transform.position - pie;
                lado.y = 0f;
                lado = lado.sqrMagnitude > 0.01f ? lado.normalized : Random.insideUnitSphere;
                lado.y = 0f;
                cuerpo.linearVelocity = lado.normalized * 2.5f + Vector3.back * 1.5f + Vector3.up * lanzamiento;
                bicho.Tropezar(0.7f);
                Sonidos.Reproducir(Sonido.Muelle, pie, bicho.EsBot ? 0.6f : 1f, 1.3f);
                Efectos.Impacto(pie + Vector3.up, new Color(1f, 0.8f, 0.3f), 0.6f);
            }
        }
    }
}
