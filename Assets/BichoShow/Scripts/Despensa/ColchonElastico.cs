using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Despensa Deslizante", zona 4: colchón de bandeja de postre que hace de cama elástica. Al
    /// pisarlo (o caer en él) lanza hacia delante en parábola; el lado lo decides tú (se conserva
    /// lo que ibas de lado). Entre colchón y colchón hay huecos: si te quedas corto, a la cinta
    /// de abajo. El colchón se aplasta un momento al botar. Puede ir y venir de lado ("vaiven").
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class ColchonElastico : MonoBehaviour
    {
        [Tooltip("Velocidad hacia arriba al botar (m/s).")]
        public float subida = 10f;
        [Tooltip("Velocidad hacia delante (Z local) al botar (m/s).")]
        public float avance = 7.5f;
        [Tooltip("Lo más que se conserva de lado (m/s).")]
        public float ladoMaximo = 3f;
        public Transform visual;
        [Tooltip("Lo que va y viene (m, a cada lado). Cero = quieto.")]
        public Vector3 vaiven;
        public float periodoVaiven = 3f;
        [Range(0f, 1f)] public float desfaseVaiven;

        readonly Dictionary<ControladorBicho, float> ultimo = new Dictionary<ControladorBicho, float>();
        Vector3 escalaVisual = Vector3.one, inicio;
        float aplastado = -10f;
        Rigidbody rb;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            if (visual != null) escalaVisual = visual.localScale;
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            inicio = transform.position;
        }

        void FixedUpdate()
        {
            if (vaiven.sqrMagnitude > 0f)
                rb.MovePosition(inicio + vaiven * Mathf.Sin((Time.time / periodoVaiven + desfaseVaiven) * Mathf.PI * 2f));
        }

        void Update()
        {
            if (visual == null) return;
            float k = Mathf.Clamp01(1f - (Time.time - aplastado) / 0.35f);
            float onda = Mathf.Sin(k * Mathf.PI) * 0.35f;
            visual.localScale = new Vector3(escalaVisual.x * (1f + onda * 0.3f), escalaVisual.y * (1f - onda), escalaVisual.z * (1f + onda * 0.3f));
        }

        void OnTriggerStay(Collider otro)
        {
            var cuerpo = otro.attachedRigidbody;
            if (cuerpo == null || !cuerpo.TryGetComponent(out ControladorBicho bicho) || bicho.Retirado) return;
            if (cuerpo.linearVelocity.y > 1f) return; // ya va hacia arriba
            if (ultimo.TryGetValue(bicho, out float antes) && Time.time - antes < 0.3f) return;
            ultimo[bicho] = Time.time;

            Vector3 delante = transform.forward;
            Vector3 derecha = transform.right;
            float lado = Mathf.Clamp(Vector3.Dot(cuerpo.linearVelocity, derecha), -ladoMaximo, ladoMaximo);
            cuerpo.linearVelocity = delante * avance + derecha * lado + Vector3.up * subida;
            aplastado = Time.time;
            Sonidos.Reproducir(Sonido.Muelle, transform.position, bicho.EsBot ? 0.6f : 1f, Random.Range(0.9f, 1.15f));
            Efectos.Polvo(cuerpo.position, 0.4f);
        }
    }
}
