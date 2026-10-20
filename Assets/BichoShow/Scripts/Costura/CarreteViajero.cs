using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Costura a la fuga", zona 2: carrete de hilo que rueda de lado a lado de su franja,
    /// cruzando el camino. Al tocar a un bicho lo empuja hacia donde va (y lo tumba un momento).
    /// El recorrido es fijo y se ve en el suelo (la guía), así que se puede leer el ritmo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarreteViajero : MonoBehaviour, IEsperaBot
    {
        [Tooltip("Hasta dónde rueda a cada lado de su posición inicial, en el eje X del mundo (m).")]
        public float recorrido = 4.4f;
        [Tooltip("Segundos de una ida y vuelta completa.")]
        public float periodo = 4.5f;
        [Tooltip("Desfase del ciclo (0-1), para que cada carrete vaya a su ritmo.")]
        [Range(0f, 1f)] public float desfase;
        [Tooltip("Radio del carrete (para que ruede a la velocidad justa).")]
        public float radio = 1.3f;

        [Header("Empujón")]
        public float empujon = 7f;
        public float empujonArriba = 3f;
        public float tropiezo = 0.5f;

        [Header("Bots")]
        [Tooltip("Lo que tarda un bicho en cruzar la franja (s): el bot espera a que el carrete no vaya a pasar por su carril en ese rato.")]
        public float tiempoCruce = 1f;

        Rigidbody rb;
        Vector3 centro;
        Quaternion giroInicial;
        readonly Dictionary<ControladorBicho, float> ultimoGolpe = new Dictionary<ControladorBicho, float>();

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            centro = transform.position;
            giroInicial = transform.rotation;
        }

        float Fase(float t) => (t / Mathf.Max(0.1f, periodo) + desfase) * Mathf.PI * 2f;

        /// <summary>Dónde estará el carrete (X del mundo) en el momento t.</summary>
        public float XEn(float t) => centro.x + recorrido * Mathf.Sin(Fase(t));

        float VelocidadEn(float t) => recorrido * Mathf.PI * 2f / Mathf.Max(0.1f, periodo) * Mathf.Cos(Fase(t));

        void FixedUpdate()
        {
            float x = XEn(Time.time);
            rb.MovePosition(new Vector3(x, centro.y, centro.z));
            // Rueda sobre su eje (el Z del mundo) lo que avanza: así no parece que patine.
            float angulo = -(x - centro.x) / Mathf.Max(0.1f, radio) * Mathf.Rad2Deg;
            rb.MoveRotation(Quaternion.AngleAxis(angulo, Vector3.forward) * giroInicial);
        }

        void OnCollisionEnter(Collision choque) => Empujar(choque);
        void OnCollisionStay(Collision choque) => Empujar(choque);

        void Empujar(Collision choque)
        {
            var cuerpo = choque.rigidbody;
            var bicho = cuerpo != null ? cuerpo.GetComponent<ControladorBicho>() : null;
            if (bicho == null || bicho.Retirado) return;
            if (ultimoGolpe.TryGetValue(bicho, out float antes) && Time.time - antes < 0.8f) return;

            float v = VelocidadEn(Time.time);
            float lado = Mathf.Sign(bicho.transform.position.x - transform.position.x);
            // Solo empuja si va hacia el bicho (si se aleja, el bicho solo lo roza).
            if (Mathf.Abs(v) > 0.6f && Mathf.Sign(v) != lado) return;
            ultimoGolpe[bicho] = Time.time;

            Vector3 velocidad = bicho.Cuerpo.linearVelocity;
            velocidad.x = lado * Mathf.Max(empujon, Mathf.Abs(v) * 1.5f);
            velocidad.y = Mathf.Max(velocidad.y, empujonArriba);
            bicho.Cuerpo.linearVelocity = velocidad;
            bicho.Tropezar(tropiezo);

            Vector3 punto = choque.contactCount > 0 ? choque.GetContact(0).point : bicho.transform.position;
            Sonidos.Reproducir(Sonido.Golpe, punto, bicho.EsBot ? 0.6f : 1f);
            Efectos.Impacto(punto, new Color(1f, 0.85f, 0.4f), 0.5f);
            if (!bicho.EsBot) Efectos.Sacudir(0.15f);
        }

        /// <summary>Para los bots: ¿puedo cruzar ya sin que el carrete pase por mi carril?</summary>
        public bool PuedoPasar(ControladorBicho bicho)
        {
            float x = bicho.transform.position.x;
            float margen = radio + 1f;
            for (float dt = 0f; dt <= tiempoCruce; dt += 0.1f)
                if (Mathf.Abs(XEn(Time.time + dt) - x) < margen) return false;
            return true;
        }
    }
}
