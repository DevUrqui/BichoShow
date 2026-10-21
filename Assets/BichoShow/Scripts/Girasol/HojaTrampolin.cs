using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "La Cumbre del Girasol": hoja redonda encajada en el borde de la pista. Al pisarla te lanza
    /// por el aire hasta "destino" (un atajo de unos segundos) y se cierra un rato; cerrada no
    /// lanza. Si dos saltan a la vez, chocan en el aire: la física se encarga.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HojaTrampolin : MonoBehaviour
    {
        [Tooltip("Dónde aterriza quien sale lanzado.")]
        public Transform destino;
        [Tooltip("Cuánto sube el salto por encima del más alto de los dos extremos (m).")]
        public float alturaVuelo = 4f;
        [Tooltip("Segundos que se queda cerrada tras lanzar a alguien.")]
        public float cierre = 4f;
        [Tooltip("El dibujo de la hoja (se dobla al cerrarse).")]
        public Transform visual;
        public Renderer render;
        public Color colorCerrada = new Color(0.25f, 0.4f, 0.2f);

        float abiertaDesde = -10f;
        Material material;
        Color colorAbierta;
        Vector3 escalaVisual = Vector3.one;
        static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        public bool Abierta => Time.time >= abiertaDesde;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            if (visual != null) escalaVisual = visual.localScale;
            if (render != null)
            {
                material = render.material;
                colorAbierta = material.HasProperty(IdColorBase) ? material.GetColor(IdColorBase) : Color.green;
            }
        }

        void Update()
        {
            float cerrada = Abierta ? 0f : Mathf.Clamp01((abiertaDesde - Time.time) / 0.4f);
            if (visual != null) visual.localScale = new Vector3(escalaVisual.x * (1f - 0.55f * cerrada), escalaVisual.y, escalaVisual.z);
            if (material != null && material.HasProperty(IdColorBase))
                material.SetColor(IdColorBase, Color.Lerp(colorAbierta, colorCerrada, Abierta ? 0f : 1f));
        }

        void OnTriggerEnter(Collider otro)
        {
            if (!Abierta || destino == null) return;
            var cuerpo = otro.attachedRigidbody;
            if (cuerpo == null || !cuerpo.TryGetComponent(out ControladorBicho bicho) || bicho.Retirado) return;

            cuerpo.linearVelocity = Velocidad(cuerpo.position, destino.position);
            abiertaDesde = Time.time + cierre;
            Sonidos.Reproducir(Sonido.Muelle, transform.position, 1f, 1.2f);
            Efectos.Polvo(transform.position, 0.6f);
        }

        /// <summary>Velocidad para llegar de "desde" a "hasta" en parábola (cae más rápido que sube, como el bicho).</summary>
        Vector3 Velocidad(Vector3 desde, Vector3 hasta)
        {
            float g = Mathf.Abs(Physics.gravity.y);
            float gBajada = g * 1.6f; // el bicho cae con gravedad extra
            float cima = Mathf.Max(desde.y, hasta.y) + alturaVuelo;
            float vy = Mathf.Sqrt(2f * g * (cima - desde.y));
            float tiempo = vy / g + Mathf.Sqrt(2f * (cima - hasta.y) / gBajada);
            Vector3 plano = new Vector3(hasta.x - desde.x, 0f, hasta.z - desde.z);
            return plano / Mathf.Max(0.1f, tiempo) + Vector3.up * vy;
        }
    }
}
