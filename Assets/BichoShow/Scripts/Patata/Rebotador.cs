using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Seta rebotadora: el bicho (o lo que sea) que la toque sale disparado hacia arriba y un
    /// poco hacia fuera, con un "¡boing!" y la seta aplastándose como una gominola.
    /// </summary>
    public class Rebotador : MonoBehaviour
    {
        public float impulsoArriba = 12f;
        public float impulsoFuera = 3f;
        [Tooltip("Lo que se aplasta al rebotar (opcional).")]
        public Transform visual;

        float ultimoRebote = -10f;
        Vector3 escalaVisual = Vector3.one;

        void Awake()
        {
            if (visual != null) escalaVisual = visual.localScale;
        }

        void OnCollisionEnter(Collision choque) => Rebotar(choque.rigidbody);
        void OnTriggerEnter(Collider otro) => Rebotar(otro.attachedRigidbody);

        void Rebotar(Rigidbody rb)
        {
            if (rb == null || rb.isKinematic) return;
            Vector3 fuera = rb.position - transform.position;
            fuera.y = 0f;
            fuera = fuera.sqrMagnitude > 0.01f ? fuera.normalized : Vector3.forward;
            Vector3 v = rb.linearVelocity;
            v.y = Mathf.Max(v.y, 0f);
            rb.linearVelocity = v;
            rb.AddForce(Vector3.up * impulsoArriba + fuera * impulsoFuera, ForceMode.VelocityChange);

            if (Time.time - ultimoRebote > 0.15f)
            {
                Sonidos.Reproducir(Sonido.Muelle, transform.position, 1f, Random.Range(0.9f, 1.15f));
                Efectos.Polvo(transform.position + Vector3.up * 0.9f, 0.4f);
            }
            ultimoRebote = Time.time;
            var bicho = rb.GetComponent<ControladorBicho>();
            if (bicho != null) Estadisticas.Evento("rebote", bicho);
        }

        void Update()
        {
            if (visual == null) return;
            float t = Time.time - ultimoRebote;
            float k = t < 0.6f ? Mathf.Sin(t * 30f) * Mathf.Exp(-t * 7f) * 0.35f : 0f;
            visual.localScale = Vector3.Scale(escalaVisual, new Vector3(1f + k * 0.5f, 1f - k, 1f + k * 0.5f));
        }
    }
}
