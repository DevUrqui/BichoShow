using System;
using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "El Cortacésped Mecha": neumático que el robot suelta desde el borde del foso y que rueda en
    /// línea recta hacia fuera del jardín. Tumba al que pilla en el suelo: hay que saltarlo. Al pasar
    /// el borde, se cae al vacío y desaparece. No usa físicas: lo mueve este script.
    /// </summary>
    public class RuedaRodante : MonoBehaviour
    {
        [Tooltip("Radio de la rueda (m), ya escalada.")]
        public float radio = 0.8f;

        [NonSerialized] public CortacespedMecha robot;
        [NonSerialized] public Vector3 direccion = Vector3.forward;
        [NonSerialized] public float velocidad = 8f;
        [NonSerialized] public float hasta = 22f;

        readonly Dictionary<ControladorBicho, float> tocados = new Dictionary<ControladorBicho, float>();
        float girado, caida;

        void Update()
        {
            float dt = Time.deltaTime;
            transform.position += direccion * velocidad * dt;
            girado += velocidad * dt / Mathf.Max(0.1f, radio) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.LookRotation(direccion) * Quaternion.Euler(girado, 0f, 0f);

            Vector3 centro = robot != null ? robot.Centro : Vector3.zero;
            Vector3 rel = transform.position - centro;
            rel.y = 0f;
            if (rel.magnitude > hasta)
            {
                // Pasado el borde: se cae al vacío.
                caida += 18f * dt;
                transform.position += Vector3.down * caida * dt;
                if (transform.position.y < centro.y - 15f) Destroy(gameObject);
                return;
            }

            float suelo = transform.position.y - radio;
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado) continue;
                if (tocados.TryGetValue(b, out float antes) && Time.time - antes < 0.8f) continue;
                Vector3 d = b.transform.position - transform.position;
                float altura = b.transform.position.y - suelo;
                d.y = 0f;
                if (d.magnitude > radio + 0.35f || altura > radio + 0.15f || altura < -1f) continue; // saltando por encima, se libra
                tocados[b] = Time.time;
                b.Cuerpo.linearVelocity = direccion * 7f + Vector3.up * 5f;
                b.Tropezar(0.9f);
                Sonidos.Reproducir(Sonido.Derribo, b.transform.position, b.EsBot ? 0.6f : 1f, 0.9f);
                Efectos.Impacto(b.transform.position + Vector3.up * 0.5f, new Color(1f, 0.8f, 0.2f), 0.6f);
                if (robot != null) robot.Derribado(b);
            }
        }
    }
}
