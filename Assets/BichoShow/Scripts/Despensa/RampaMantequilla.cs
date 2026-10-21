using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Despensa Deslizante", zona 3: la rampa de mantequilla. Se agarra menos (40 % menos) y, si
    /// saltas mientras subes, al caer resbalas cuesta abajo: hay que subir corriendo. Encima hay
    /// una lámpara de calor que, a ratos, derrite una franja que sube por la rampa: dentro de
    /// ella casi no hay agarre (se ve brillante y anaranjada). El pivote está abajo en el centro
    /// de la rampa; "adelante" (Z local) es cuesta arriba.
    /// </summary>
    public class RampaMantequilla : MonoBehaviour
    {
        [Tooltip("Ancho y largo de la rampa (m, medidos sobre ella).")]
        public Vector2 tamano = new Vector2(14f, 50f);
        [Range(0f, 1f)] public float agarre = 0.6f;
        [Tooltip("Velocidad cuesta abajo al caer de un salto (m/s).")]
        public float resbalonSalto = 6f;

        [Header("Lámpara de calor")]
        [Range(0f, 1f)] public float agarreDerretida = 0.12f;
        public float largoFranja = 9f;
        [Tooltip("Lo que tarda la franja en recorrer la rampa de abajo arriba (s).")]
        public float duracionFranja = 7f;
        [Tooltip("Rato entre una franja y la siguiente (s).")]
        public float descanso = 4f;
        [Tooltip("La franja derretida que se ve (un plano brillante hijo de la rampa).")]
        public Transform franja;
        [Tooltip("La bombilla de la lámpara (se enciende con la franja).")]
        public Renderer bombilla;
        public Light luzLampara;
        [Tooltip("La lámpara (va por un raíl encima de la rampa, siguiendo a la franja).")]
        public Transform lampara;
        public float alturaLampara = 7f;

        readonly HashSet<ControladorBicho> enElAire = new HashSet<ControladorBicho>();
        readonly HashSet<ControladorBicho> fuera = new HashSet<ControladorBicho>();
        float desdeFranja;

        /// <summary>Dónde está ahora el centro de la franja (m desde abajo); negativo si está apagada.</summary>
        public float CentroFranja
        {
            get
            {
                float c = Mathf.Repeat(Time.time - desdeFranja, duracionFranja + descanso);
                if (c > duracionFranja) return -1f;
                return Mathf.Lerp(-largoFranja * 0.5f, tamano.y + largoFranja * 0.5f, c / duracionFranja);
            }
        }

        void Start() => desdeFranja = Time.time + 2f;

        void Update()
        {
            float centro = CentroFranja;
            bool encendida = centro >= 0f;
            if (franja != null)
            {
                franja.gameObject.SetActive(encendida);
                if (encendida) franja.localPosition = new Vector3(0f, 0.03f, Mathf.Clamp(centro, 0f, tamano.y));
            }
            if (luzLampara != null) luzLampara.enabled = encendida;
            if (lampara != null)
            {
                // Encendida, va con la franja; apagada, vuelve despacio al pie de la rampa.
                Vector3 destino = transform.TransformPoint(0f, alturaLampara, encendida ? Mathf.Clamp(centro, 0f, tamano.y) : 0f);
                destino.y = Mathf.Max(destino.y, transform.TransformPoint(0f, alturaLampara, 0f).y);
                lampara.position = encendida ? destino : Vector3.MoveTowards(lampara.position, destino, tamano.y / Mathf.Max(0.5f, descanso) * Time.deltaTime);
            }
            if (bombilla != null) bombilla.enabled = encendida;
        }

        void FixedUpdate()
        {
            float centro = CentroFranja;
            Vector3 abajo = -transform.forward;
            fuera.Clear();
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado) continue;
                Vector3 local = transform.InverseTransformPoint(b.transform.position);
                bool encima = Mathf.Abs(local.x) < tamano.x * 0.5f && local.z > -0.5f && local.z < tamano.y + 0.5f && local.y > -0.5f && local.y < 2.5f;
                if (!encima) { enElAire.Remove(b); continue; }

                if (!b.EnSuelo)
                {
                    // Ha saltado (va hacia arriba): al caer, resbalará.
                    if (b.Cuerpo.linearVelocity.y > 2f) enElAire.Add(b);
                    continue;
                }
                bool derretida = centro >= 0f && Mathf.Abs(local.z - centro) < largoFranja * 0.5f;
                b.Resbalar(derretida ? agarreDerretida : agarre, 0.2f);
                if (enElAire.Remove(b))
                {
                    // Aterrizaje tras un salto: patina cuesta abajo.
                    var v = b.Cuerpo.linearVelocity;
                    b.Cuerpo.linearVelocity = new Vector3(v.x * 0.5f, v.y, 0f) + abajo * resbalonSalto;
                    b.Resbalar(0.05f, 0.8f);
                    Sonidos.Reproducir(Sonido.Caida, b.transform.position, b.EsBot ? 0.4f : 0.8f, 1.5f);
                }
            }
        }
    }
}
