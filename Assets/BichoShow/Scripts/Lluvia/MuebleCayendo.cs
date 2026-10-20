using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Un mueble que cae del cielo en "Lluvia de muebles": avisa a la ronda si aplasta a un
    /// bicho y cuando toca el suelo; después de un rato en el suelo, se encoge y desaparece.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MuebleCayendo : MonoBehaviour
    {
        public int Oleada { get; set; }
        public bool EnElSuelo { get; private set; }

        RondaLluvia ronda;
        Rigidbody rb;
        float desaparecerEn = -1f;
        Vector3 escala;

        public void Preparar(RondaLluvia rondaLluvia, int oleada, float velocidadCaida)
        {
            ronda = rondaLluvia;
            Oleada = oleada;
            rb = GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.down * velocidadCaida;
            rb.angularVelocity = Random.insideUnitSphere * 1.5f;
            escala = transform.localScale;
        }

        void OnCollisionEnter(Collision choque)
        {
            if (ronda == null) return;
            var bicho = choque.rigidbody != null ? choque.rigidbody.GetComponent<ControladorBicho>() : null;
            if (bicho != null)
            {
                // Aplasta si cae con fuerza y le da desde arriba.
                bool desdeArriba = transform.position.y > bicho.Cuerpo.worldCenterOfMass.y + 0.2f;
                if (!EnElSuelo && desdeArriba && choque.relativeVelocity.magnitude > 4f)
                    ronda.Aplastado(bicho, this);
                return;
            }
            if (!EnElSuelo && choque.contactCount > 0 && choque.GetContact(0).normal.y > 0.5f)
            {
                EnElSuelo = true;
                desaparecerEn = Time.time + 3f;
                ronda.Aterrizo(this, choque.GetContact(0).point);
            }
        }

        void Update()
        {
            if (transform.position.y < -20f) { Destroy(gameObject); return; }
            if (desaparecerEn < 0f || Time.time < desaparecerEn) return;
            float k = 1f - (Time.time - desaparecerEn) / 0.4f;
            if (k <= 0f) { Destroy(gameObject); return; }
            transform.localScale = escala * k;
        }
    }
}
