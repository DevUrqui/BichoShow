using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "La Cumbre del Girasol": mariquita acolchada gigante que despide hacia atrás a quien la
    /// toca. Las quietas vibran; las que patrullan van y vienen de lado a lado de la pista
    /// (entre "puntoA" y "puntoB").
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MariquitaBumper : MonoBehaviour
    {
        [Header("Patrulla (si los dos puntos son iguales, se queda quieta)")]
        public Vector3 puntoA;
        public Vector3 puntoB;
        [Tooltip("Segundos de una ida y vuelta.")]
        public float periodo = 4f;
        [Range(0f, 1f)] public float desfase;

        [Header("Empujón")]
        public float empujon = 9f;
        public float empujonArriba = 3f;
        public float tropiezo = 0.5f;
        [Tooltip("El dibujo, que se aplasta y vibra.")]
        public Transform visual;

        Rigidbody rb;
        Quaternion giro;
        Vector3 escalaVisual = Vector3.one;
        float finAplastado = -10f;
        readonly Dictionary<ControladorBicho, float> ultimoGolpe = new Dictionary<ControladorBicho, float>();

        bool Patrulla => (puntoB - puntoA).sqrMagnitude > 0.01f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            giro = transform.rotation;
            if (visual != null) escalaVisual = visual.localScale;
            if (!Patrulla) puntoA = puntoB = transform.position;
        }

        void FixedUpdate()
        {
            if (!Patrulla) return;
            float k = 0.5f - 0.5f * Mathf.Cos((Time.time / Mathf.Max(0.1f, periodo) + desfase) * Mathf.PI * 2f);
            Vector3 p = Vector3.Lerp(puntoA, puntoB, k);
            rb.MovePosition(p);
            // Mira hacia donde va.
            Vector3 v = (puntoB - puntoA) * Mathf.Sin((Time.time / Mathf.Max(0.1f, periodo) + desfase) * Mathf.PI * 2f);
            if (v.sqrMagnitude > 0.01f) rb.MoveRotation(Quaternion.LookRotation(new Vector3(v.x, 0f, v.z)));
        }

        void Update()
        {
            if (visual == null) return;
            // Quieta: vibra un poco. Al golpear: se aplasta y recupera la forma de un bote.
            float vibra = Patrulla ? 0f : Mathf.Sin(Time.time * 40f) * 0.02f;
            float golpe = Mathf.Clamp01((finAplastado - Time.time) / 0.25f);
            visual.localScale = new Vector3(escalaVisual.x * (1f + golpe * 0.2f + vibra), escalaVisual.y * (1f - golpe * 0.3f - vibra),
                                            escalaVisual.z * (1f + golpe * 0.2f + vibra));
        }

        void OnCollisionEnter(Collision c) => Empujar(c);
        void OnCollisionStay(Collision c) => Empujar(c);

        void Empujar(Collision choque)
        {
            var cuerpo = choque.rigidbody;
            var bicho = cuerpo != null ? cuerpo.GetComponent<ControladorBicho>() : null;
            if (bicho == null || bicho.Retirado) return;
            if (ultimoGolpe.TryGetValue(bicho, out float antes) && Time.time - antes < 0.6f) return;
            ultimoGolpe[bicho] = Time.time;

            Vector3 fuera = bicho.transform.position - transform.position;
            fuera.y = 0f;
            if (fuera.sqrMagnitude < 0.01f) fuera = transform.forward;
            Vector3 v = fuera.normalized * empujon;
            v.y = empujonArriba;
            bicho.Cuerpo.linearVelocity = v;
            bicho.Tropezar(tropiezo);
            finAplastado = Time.time + 0.25f;

            Vector3 punto = choque.contactCount > 0 ? choque.GetContact(0).point : bicho.transform.position;
            Sonidos.Reproducir(Sonido.Muelle, punto, bicho.EsBot ? 0.6f : 1f);
            Efectos.Impacto(punto, new Color(1f, 0.3f, 0.25f), 0.5f);
        }
    }
}
