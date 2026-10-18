using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Trampa de playa: un ventilador gigante sopla sobre una zona durante unos segundos.
    /// Desvía la pelota y empuja a los bichos (que además resbalan un poco).
    /// </summary>
    public class RafagaViento : TrampaSaboteable
    {
        [Header("Viento")]
        [Tooltip("Hacia dónde sopla.")]
        public Vector3 direccion = Vector3.forward;
        [Tooltip("Centro de la zona donde sopla (coordenadas del mundo).")]
        public Vector3 centroZona;
        [Tooltip("Tamaño de la zona (caja alineada con el mundo).")]
        public Vector3 tamanoZona = new Vector3(10f, 8f, 14f);
        public float duracion = 2f;
        [Tooltip("Aceleración que recibe la pelota (m/s²).")]
        public float fuerzaPelota = 8f;
        [Tooltip("Aceleración que reciben los bichos (m/s²).")]
        public float fuerzaBichos = 11f;
        [Tooltip("Aspas que giran mientras sopla (opcional).")]
        public Transform aspas;

        float fin = -100f;
        readonly Collider[] encontrados = new Collider[32];
        readonly HashSet<Rigidbody> empujados = new HashSet<Rigidbody>();
        Transform[] rachas;

        public bool Soplando => Time.time < fin;
        public override Vector3 CentroEfecto => centroZona;

        protected override Sonido SonidoAlActivar => Sonido.Viento;

        protected override void AlActivar() => fin = Time.time + duracion;

        void Start() => CrearRachas();

        void FixedUpdate()
        {
            if (!Soplando) return;

            Vector3 dir = direccion.normalized;
            int n = Physics.OverlapBoxNonAlloc(centroZona, tamanoZona * 0.5f, encontrados, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            empujados.Clear();
            for (int i = 0; i < n; i++)
            {
                var rb = encontrados[i].attachedRigidbody;
                if (rb == null || rb.isKinematic || !empujados.Add(rb)) continue;

                if (rb.GetComponent<PelotaVoley>() != null)
                {
                    rb.AddForce(dir * fuerzaPelota, ForceMode.Acceleration);
                }
                else
                {
                    var bicho = rb.GetComponent<ControladorBicho>();
                    if (bicho == null) continue;
                    bicho.Resbalar(0.55f, 0.15f);
                    rb.AddForce(dir * fuerzaBichos, ForceMode.Acceleration);
                }
            }
        }

        // ---------------------------------------------------------------------
        // Aspecto: aspas girando y rachas blancas que cruzan la zona
        // ---------------------------------------------------------------------

        void CrearRachas()
        {
            rachas = new Transform[14];
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(1f, 1f, 1f));
            for (int i = 0; i < rachas.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Racha";
                Destroy(go.GetComponent<Collider>());
                go.GetComponent<Renderer>().sharedMaterial = material;
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.transform.SetParent(transform, true);
                go.SetActive(false);
                rachas[i] = go.transform;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (aspas != null && Soplando) aspas.Rotate(0f, 0f, 900f * Time.deltaTime, Space.Self);
            if (rachas == null) return;

            Vector3 dir = direccion.normalized;
            float largo = Mathf.Abs(Vector3.Dot(tamanoZona, new Vector3(Mathf.Abs(dir.x), Mathf.Abs(dir.y), Mathf.Abs(dir.z))));
            Vector3 lateral = Vector3.Cross(Vector3.up, dir);
            float ancho = Mathf.Abs(Vector3.Dot(tamanoZona, new Vector3(Mathf.Abs(lateral.x), 0f, Mathf.Abs(lateral.z))));

            for (int i = 0; i < rachas.Length; i++)
            {
                rachas[i].gameObject.SetActive(Soplando);
                if (!Soplando) continue;
                // Cada racha avanza a su ritmo y vuelve a empezar al llegar al final.
                float semilla = i * 0.37f;
                float avance = Mathf.Repeat(Time.time * 1.6f + semilla, 1f) - 0.5f;
                float lado = (Mathf.Repeat(semilla * 7.3f, 1f) - 0.5f) * ancho;
                float altura = 0.5f + Mathf.Repeat(semilla * 3.1f, 1f) * 3f;
                rachas[i].position = centroZona + dir * avance * largo + lateral * lado + Vector3.up * (altura - centroZona.y);
                rachas[i].rotation = Quaternion.LookRotation(dir);
                rachas[i].localScale = new Vector3(0.05f, 0.05f, 1.6f);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireCube(centroZona, tamanoZona);
            Gizmos.DrawRay(centroZona, direccion.normalized * 3f);
        }
    }
}
