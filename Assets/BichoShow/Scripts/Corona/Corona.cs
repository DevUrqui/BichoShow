using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// La corona de "La Corona": suelta, rueda y rebota con física; llevada, flota girando
    /// sobre la cabeza de su rey. Quién la coge o la pierde lo decide RondaCorona.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Corona : MonoBehaviour
    {
        [Tooltip("Altura sobre el centro del bicho a la que flota.")]
        public float altura = 1.3f;

        public ControladorBicho Portador { get; private set; }
        public bool Suelta => Portador == null;

        Rigidbody rb;
        Collider[] colliders;
        ControladorBicho anterior;
        float noCogerHasta, anteriorNoCogeHasta;
        Vector3 posicionSuave;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public bool PuedeCoger(ControladorBicho b) =>
            Suelta && Time.time >= noCogerHasta && !(b == anterior && Time.time < anteriorNoCogeHasta);

        public void Coger(ControladorBicho b)
        {
            Portador = b;
            rb.isKinematic = true;
            foreach (var c in colliders) c.enabled = false;
            posicionSuave = transform.position;
        }

        /// <summary>Sale disparada (al derribar al rey). Quien la llevaba tarda un poco más en poder recogerla.</summary>
        public void Soltar(Vector3 velocidad)
        {
            anterior = Portador;
            Portador = null;
            rb.isKinematic = false;
            foreach (var c in colliders) c.enabled = true;
            rb.linearVelocity = velocidad;
            rb.angularVelocity = Random.onUnitSphere * 8f;
            noCogerHasta = Time.time + 0.35f;
            anteriorNoCogeHasta = Time.time + 1.2f;
        }

        /// <summary>La pone suelta en un punto (al empezar o si se cae del mapa).</summary>
        public void Colocar(Vector3 posicion)
        {
            Portador = null;
            anterior = null;
            rb.isKinematic = false;
            foreach (var c in colliders) c.enabled = true;
            rb.position = posicion;
            transform.position = posicion;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        void LateUpdate()
        {
            if (Portador == null) return;
            Vector3 centro = Portador.Cuerpo != null ? Portador.Cuerpo.worldCenterOfMass : Portador.transform.position;
            Vector3 objetivo = centro + Vector3.up * (altura + Mathf.Sin(Time.time * 4f) * 0.05f);
            posicionSuave = Vector3.Lerp(posicionSuave, objetivo, 1f - Mathf.Exp(-25f * Time.deltaTime));
            transform.position = posicionSuave;
            transform.rotation = Quaternion.Euler(0f, Time.time * 120f, Mathf.Sin(Time.time * 3f) * 8f);
        }
    }
}
