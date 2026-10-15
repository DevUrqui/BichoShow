using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Trampa de playa: una ola entra en medio campo, empuja un poco al llegar y lo deja
    /// encharcado (resbaladizo) unos segundos.
    /// </summary>
    public class Ola : TrampaSaboteable
    {
        [Header("Ola")]
        [Tooltip("Centro de la zona que se encharca (en el suelo).")]
        public Vector3 centroZona;
        [Tooltip("Tamaño de la zona en el suelo (X, Z).")]
        public Vector2 tamano = new Vector2(10f, 12f);
        [Tooltip("Hacia dónde entra el agua (empuje inicial).")]
        public Vector3 direccion = Vector3.forward;
        public float duracion = 4f;
        public float empujeInicial = 14f;
        public float tiempoEmpuje = 0.6f;
        [Range(0f, 1f)] public float agarre = 0.3f;
        [Tooltip("Lámina de agua que sube y baja (sin collider).")]
        public Transform agua;

        float inicio = -100f;
        readonly Collider[] encontrados = new Collider[32];

        public override Vector3 CentroEfecto => centroZona;
        bool Activa => Time.time - inicio < duracion;

        protected override void AlActivar() => inicio = Time.time;

        void FixedUpdate()
        {
            if (!Activa) return;
            float t = Time.time - inicio;

            int n = Physics.OverlapBoxNonAlloc(centroZona + Vector3.up, new Vector3(tamano.x * 0.5f, 1f, tamano.y * 0.5f),
                                               encontrados, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var rb = encontrados[i].attachedRigidbody;
                var bicho = rb != null ? rb.GetComponent<ControladorBicho>() : null;
                if (bicho == null) continue;
                bicho.Resbalar(agarre, 0.2f);
                if (t < tiempoEmpuje) rb.AddForce(direccion.normalized * empujeInicial, ForceMode.Acceleration);
            }
        }

        protected override void Update()
        {
            base.Update();
            if (agua == null) return;

            // Sube rápido, se queda y baja despacio.
            float t = Time.time - inicio;
            float nivel = 0f;
            if (t >= 0f && t < duracion)
                nivel = t < 0.4f ? t / 0.4f : t > duracion - 1f ? (duracion - t) : 1f;

            agua.gameObject.SetActive(nivel > 0.01f);
            agua.position = new Vector3(centroZona.x, Mathf.Lerp(-0.2f, 0.06f, nivel), centroZona.z);
            agua.localScale = new Vector3(tamano.x, 0.08f, tamano.y);
        }
    }
}
