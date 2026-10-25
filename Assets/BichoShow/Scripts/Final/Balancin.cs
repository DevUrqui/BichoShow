using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Balancín: una tabla larga sobre el vacío que gira por su centro hacia donde haya más peso
    /// (los bichos que tiene encima, según lo lejos del centro que estén). Inclinada, resbala hacia
    /// el lado que baja. Mientras alguien pisa el freno (al otro lado), se queda recta. Con un solo
    /// jugador en el equipo, siempre recta. El objeto es el eje; la tabla va a lo largo de su Z.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Balancin : MonoBehaviour
    {
        public int equipo;
        public PlacaPresion freno;
        public float largo = 20f;
        public float ancho = 2.6f;
        [Tooltip("Grados por cada metro de desequilibrio.")]
        public float sensibilidad = 2.4f;
        public float anguloMaximo = 20f;
        [Tooltip("Grados por segundo al inclinarse.")]
        public float velocidad = 22f;
        [Tooltip("A partir de esta inclinación se resbala.")]
        public float anguloResbalar = 7f;

        public float Angulo => angulo;
        public bool Frenado { get; private set; }

        Rigidbody rb;
        Quaternion recto;
        float angulo;
        readonly List<ControladorBicho> encima = new List<ControladorBicho>();

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            recto = transform.rotation;
        }

        /// <summary>Dónde está ese bicho a lo largo de la tabla (0 = el eje; + hacia el final).</summary>
        public float PosicionEnTabla(ControladorBicho b) => transform.InverseTransformPoint(b.transform.position).z;

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            encima.Clear();
            float peso = 0f;
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado) continue;
                Vector3 local = transform.InverseTransformPoint(b.transform.position);
                if (Mathf.Abs(local.x) > ancho * 0.5f + 0.3f || Mathf.Abs(local.z) > largo * 0.5f || local.y < -0.6f || local.y > 2.5f) continue;
                encima.Add(b);
                peso += local.z;
            }

            var ronda = RondaFinal.Instancia;
            Frenado = (freno != null && freno.Pulsada) || (ronda != null && ronda.MiembrosDe(equipo) <= 1);
            float objetivo = Frenado ? 0f : Mathf.Clamp(peso * sensibilidad, -anguloMaximo, anguloMaximo);
            angulo = Mathf.MoveTowards(angulo, objetivo, (Frenado ? velocidad * 3f : velocidad) * dt);
            rb.MoveRotation(recto * Quaternion.Euler(angulo, 0f, 0f));

            // Inclinada: resbala hacia el lado que baja.
            float exceso = Mathf.Abs(angulo) - anguloResbalar;
            if (exceso <= 0f) return;
            Vector3 cuestaAbajo = transform.forward * Mathf.Sign(angulo);
            cuestaAbajo.y = 0f;
            cuestaAbajo.Normalize();
            foreach (var b in encima)
            {
                if (!b.EnSuelo) continue;
                b.Resbalar(0.3f, 0.2f);
                b.Cuerpo.linearVelocity += cuestaAbajo * (exceso * 0.5f) * dt; // unos 6 m/s² con la tabla a tope
            }
        }
    }
}
