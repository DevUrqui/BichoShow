using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "La Cumbre del Girasol", sección 3: ventilador del plató que, cada pocos segundos, suelta
    /// una ráfaga hacia fuera de la espiral. Antes de soplar avisa: las aspas aceleran y suena.
    /// La zona que sopla es una caja orientada con el propio objeto (sopla hacia su +Z).
    /// </summary>
    public class VientoLateral : MonoBehaviour
    {
        [Tooltip("Tamaño de la zona que sopla (ancho, alto, largo hacia delante).")]
        public Vector3 tamanoZona = new Vector3(10f, 4f, 9f);
        public float periodo = 5f;
        public float duracion = 1.4f;
        public float aviso = 1f;
        [Range(0f, 1f)] public float desfase;
        public float fuerza = 16f;
        public Transform aspas;

        readonly Collider[] encontrados = new Collider[24];
        readonly HashSet<Rigidbody> empujados = new HashSet<Rigidbody>();
        int ultimoAviso = -1;

        float Fase => Mathf.Repeat(Time.time / periodo + desfase, 1f) * periodo;
        public bool Soplando => Fase < duracion;
        bool Avisando => Fase > periodo - aviso;

        Vector3 CentroZona => transform.position + transform.forward * tamanoZona.z * 0.5f + Vector3.up * (tamanoZona.y * 0.5f - 0.5f);

        void FixedUpdate()
        {
            if (!Soplando) return;
            int n = Physics.OverlapBoxNonAlloc(CentroZona, tamanoZona * 0.5f, encontrados, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            empujados.Clear();
            Vector3 dir = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            for (int i = 0; i < n; i++)
            {
                var rb = encontrados[i].attachedRigidbody;
                if (rb == null || rb.isKinematic || !empujados.Add(rb)) continue;
                if (!rb.TryGetComponent(out ControladorBicho bicho) || bicho.Retirado) continue;
                bicho.Resbalar(0.4f, 0.15f);
                rb.AddForce(dir * fuerza, ForceMode.Acceleration);
            }
        }

        void Update()
        {
            int ciclo = Mathf.FloorToInt(Time.time / periodo + desfase);
            if (Avisando && ciclo != ultimoAviso)
            {
                ultimoAviso = ciclo;
                Sonidos.Reproducir(Sonido.Viento, transform.position, 0.8f);
            }
            if (aspas != null)
            {
                float velocidad = Soplando ? 1400f : Avisando ? 700f : 120f;
                aspas.Rotate(0f, 0f, velocidad * Time.deltaTime, Space.Self);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.5f);
            Gizmos.matrix = Matrix4x4.TRS(CentroZona, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, tamanoZona);
        }
    }
}
