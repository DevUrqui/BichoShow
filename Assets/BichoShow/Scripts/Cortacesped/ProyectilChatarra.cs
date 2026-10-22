using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "El Cortacésped Mecha": tuerca o pila de goma que escupe el robot. Rebota por la arena y
    /// tumba al bicho que pilla (empujándolo hacia donde iba ella). Al rato desaparece.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ProyectilChatarra : MonoBehaviour
    {
        public float vida = 7f;
        public float empujon = 6f;

        readonly Dictionary<ControladorBicho, float> ultimo = new Dictionary<ControladorBicho, float>();
        Rigidbody rb;
        float muere;

        void Awake() => rb = GetComponent<Rigidbody>();

        void OnEnable() => muere = Time.time + vida;

        void Update()
        {
            if (Time.time > muere || transform.position.y < -12f) Destroy(gameObject);
        }

        void OnCollisionEnter(Collision c)
        {
            if (c.rigidbody == null || !c.rigidbody.TryGetComponent(out ControladorBicho bicho) || bicho.Retirado) return;
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            if (v.magnitude < 1.5f) return; // parada: solo estorba
            if (ultimo.TryGetValue(bicho, out float antes) && Time.time - antes < 0.8f) return;
            ultimo[bicho] = Time.time;
            bicho.Cuerpo.linearVelocity = v.normalized * empujon + Vector3.up * 3f;
            bicho.Tropezar(0.6f);
            Vector3 punto = c.contactCount > 0 ? c.GetContact(0).point : bicho.transform.position;
            Sonidos.Reproducir(Sonido.Golpe, punto, bicho.EsBot ? 0.6f : 1f, 1.1f);
            Efectos.Impacto(punto, new Color(1f, 0.75f, 0.3f), 0.5f);
        }
    }
}
