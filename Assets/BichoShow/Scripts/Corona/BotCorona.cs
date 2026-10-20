using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Bot para "La Corona":
    ///  - Corona suelta: va a por ella (y salta si está más alta).
    ///  - Hay rey y no soy yo: lo persigue y se lanza en plancha cuando está a tiro.
    ///  - Soy el rey: huye del que tenga más cerca, sin acercarse al borde.
    /// </summary>
    public class BotCorona : MonoBehaviour, IEntradaBicho
    {
        [Range(0f, 1f)] public float habilidad = 0.55f;

        public Vector2 Mover { get; private set; }
        public bool Agarrar => false;

        RondaCorona ronda;
        ControladorBicho bicho;
        Camera camara;
        bool saltarPendiente, lanzarsePendiente;
        float siguientePensar, siguienteSalto, semilla;
        Vector3 direccion, objetivo;

        public bool ConsumirSaltar() { bool s = saltarPendiente; saltarPendiente = false; return s; }
        public bool ConsumirLanzarse() { bool s = lanzarsePendiente; lanzarsePendiente = false; return s; }

        public void Configurar(RondaCorona rondaCorona, float nivel)
        {
            ronda = rondaCorona;
            habilidad = nivel;
        }

        void Start()
        {
            bicho = GetComponent<ControladorBicho>();
            camara = Camera.main;
            if (ronda == null) ronda = RondaCorona.Instancia;
            semilla = Random.value * 100f;
        }

        void Update()
        {
            if (ronda == null || bicho == null || ronda.EstadoActual != RondaBase.Estado.Jugando || bicho.Tumbado)
            {
                Mover = Vector2.zero;
                return;
            }
            if (Time.time >= siguientePensar)
            {
                Pensar();
                siguientePensar = Time.time + Mathf.Lerp(0.35f, 0.1f, habilidad) * Random.Range(0.8f, 1.2f);
            }
            Saltar();
            if (Camera.main != null) camara = Camera.main;
            Mover = AStick(direccion);
        }

        void Pensar()
        {
            Vector3 yo = transform.position;
            Vector3 centro = ronda.Centro;
            var rey = ronda.Rey;
            Vector3 deseo;

            if (rey == null && ronda.corona != null)
            {
                objetivo = ronda.corona.transform.position;
                deseo = Plano(objetivo - yo).normalized;
            }
            else if (rey != null && rey != bicho)
            {
                objetivo = rey.transform.position + rey.Cuerpo.linearVelocity * Mathf.Lerp(0f, 0.3f, habilidad);
                Vector3 hacia = Plano(objetivo - yo);
                deseo = hacia.normalized;
                float d = hacia.magnitude;
                bool mismaAltura = Mathf.Abs(rey.transform.position.y - yo.y) < 0.6f;
                if (mismaAltura && d < 2.8f && d > 1.1f && Random.value < Mathf.Lerp(0.08f, 0.3f, habilidad))
                    lanzarsePendiente = true;
            }
            else
            {
                // Soy el rey: huir del perseguidor más cercano, en zigzag.
                ControladorBicho cerca = null;
                float mejor = float.MaxValue;
                foreach (var b in ControladorBicho.Todos)
                {
                    if (b == bicho || b.Retirado) continue;
                    float d = Plano(b.transform.position - yo).sqrMagnitude;
                    if (d < mejor) { mejor = d; cerca = b; }
                }
                objetivo = yo;
                Vector3 lejos = cerca != null ? Plano(yo - cerca.transform.position) : Vector3.forward;
                float zigzag = (Mathf.PerlinNoise(Time.time * 0.9f, semilla) - 0.5f) * 120f;
                float urgencia = Mathf.Clamp01(1f - (Mathf.Sqrt(mejor) - 2f) / 7f);
                deseo = Quaternion.Euler(0f, zigzag, 0f) * lejos.normalized * Mathf.Lerp(0.3f, 1f, urgencia);
            }

            // Sin acercarse al borde.
            Vector3 haciaCentro = Plano(centro - yo);
            float fuera = haciaCentro.magnitude / Mathf.Max(1f, ronda.radioArena);
            float tiron = Mathf.Clamp01((fuera - 0.6f) / 0.3f);
            deseo = Vector3.Lerp(deseo, haciaCentro.normalized * Mathf.Max(0.6f, deseo.magnitude), tiron * Mathf.Lerp(0.6f, 1f, habilidad));

            float ruido = (Mathf.PerlinNoise(Time.time * 1.3f, semilla + 7f) - 0.5f) * Mathf.Lerp(50f, 8f, habilidad);
            direccion = Vector3.ClampMagnitude(Quaternion.Euler(0f, ruido, 0f) * deseo, 1f);
        }

        /// <summary>Si lo que persigue está en el piso de arriba y estoy al pie del escalón, salta.</summary>
        void Saltar()
        {
            if (Time.time < siguienteSalto || !bicho.EnSuelo) return;
            Vector3 yo = transform.position;
            float alRadio = Plano(yo - ronda.Centro).magnitude;
            bool arriba = objetivo.y > yo.y + 0.6f;
            bool alPie = alRadio > ronda.radioPisoAlto - 0.3f && alRadio < ronda.radioPisoAlto + 1.4f;
            bool barredora = yo.y > ronda.alturaPisoAlto - 0.3f && alRadio < 4.2f; // en lo alto gira la barredora
            if ((arriba && alPie) || barredora)
            {
                saltarPendiente = Random.value < Mathf.Lerp(0.35f, 0.85f, habilidad);
                siguienteSalto = Time.time + Random.Range(0.5f, 1.1f);
            }
        }

        Vector2 AStick(Vector3 direccionMundo)
        {
            Vector3 adelante = Vector3.forward, derecha = Vector3.right;
            if (camara != null)
            {
                adelante = Plano(camara.transform.forward).normalized;
                derecha = Plano(camara.transform.right).normalized;
            }
            return new Vector2(Vector3.Dot(direccionMundo, derecha), Vector3.Dot(direccionMundo, adelante));
        }

        static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
