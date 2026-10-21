using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Costura a la fuga": cinta métrica que rueda hacia atrás (contra la carrera) y lleva
    /// encima a quien la pisa. Las marcas de la cinta se mueven a su ritmo para que se vea.
    /// </summary>
    public class CintaTransportadora : MonoBehaviour
    {
        [Tooltip("Hacia dónde lleva (en el mundo) y a qué velocidad (m/s).")]
        public Vector3 direccion = Vector3.back;
        public float velocidad = 2.2f;
        [Tooltip("Tamaño (ancho, largo) de la cara de arriba; el pivote está en su centro, arriba.")]
        public Vector2 tamano = new Vector2(6f, 19f);
        [Tooltip("Marcas que se mueven con la cinta (se dan la vuelta al llegar al final).")]
        public Transform[] marcas;

        readonly Collider[] encima = new Collider[16];
        readonly HashSet<Rigidbody> movidos = new HashSet<Rigidbody>();
        Vector3[] inicioMarcas;

        void Awake()
        {
            if (marcas == null) return;
            inicioMarcas = new Vector3[marcas.Length];
            for (int i = 0; i < marcas.Length; i++) if (marcas[i] != null) inicioMarcas[i] = marcas[i].localPosition;
        }

        void FixedUpdate()
        {
            Vector3 paso = direccion.normalized * velocidad * Time.fixedDeltaTime;
            var medio = new Vector3(tamano.x * 0.5f, 0.5f, tamano.y * 0.5f);
            int n = Physics.OverlapBoxNonAlloc(transform.position + Vector3.up * 0.5f, medio, encima, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            movidos.Clear();
            for (int i = 0; i < n; i++)
            {
                var cuerpo = encima[i].attachedRigidbody;
                if (cuerpo == null || cuerpo.isKinematic || !movidos.Add(cuerpo)) continue;
                if (cuerpo.TryGetComponent(out ControladorBicho bicho) && !bicho.EnSuelo) continue;
                cuerpo.position += paso;
            }
        }

        void Update()
        {
            if (marcas == null || inicioMarcas == null) return;
            // Las marcas avanzan con la cinta y, al salir por un extremo, vuelven por el otro.
            Vector3 local = transform.InverseTransformDirection(direccion.normalized);
            float recorrido = Time.time * velocidad;
            for (int i = 0; i < marcas.Length; i++)
            {
                if (marcas[i] == null) continue;
                float z = inicioMarcas[i].z + local.z * recorrido;
                z = Mathf.Repeat(z + tamano.y * 0.5f, tamano.y) - tamano.y * 0.5f;
                marcas[i].localPosition = new Vector3(inicioMarcas[i].x, inicioMarcas[i].y, z);
            }
        }
    }
}
