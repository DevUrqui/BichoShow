using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Bot para "Patata caliente":
    ///  - Con la bomba: persigue al más cercano (que no sea inmune) y se lanza en plancha cuando
    ///    está a tiro.
    ///  - Sin ella: huye de quien la lleva, en zigzag y sin acercarse al borde.
    ///  - Salta la barredora del centro cuando le pilla cerca (los torpes, a destiempo).
    /// </summary>
    public class BotPatata : MonoBehaviour, IEntradaBicho
    {
        [Range(0f, 1f)] public float habilidad = 0.55f;

        public Vector2 Mover { get; private set; }
        public bool Agarrar => false;

        RondaPatata ronda;
        ControladorBicho bicho;
        Camera camara;
        bool saltarPendiente, lanzarsePendiente;
        float siguientePensar, siguienteSalto, semilla;
        Vector3 direccion;

        public bool ConsumirSaltar() { bool s = saltarPendiente; saltarPendiente = false; return s; }
        public bool ConsumirLanzarse() { bool s = lanzarsePendiente; lanzarsePendiente = false; return s; }

        public void Configurar(RondaPatata rondaPatata, float nivel)
        {
            ronda = rondaPatata;
            habilidad = nivel;
        }

        void Start()
        {
            bicho = GetComponent<ControladorBicho>();
            camara = Camera.main;
            if (ronda == null) ronda = RondaPatata.Instancia;
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
            SaltarBarredora();
            if (Camera.main != null) camara = Camera.main;
            Mover = AStick(direccion);
        }

        void Pensar()
        {
            Vector3 yo = transform.position;
            Vector3 centro = ronda.Centro;
            var portador = ronda.Portador;

            Vector3 deseo;
            if (portador == bicho)
            {
                // ¡La tengo yo! A por el más cercano que se pueda.
                ControladorBicho presa = null;
                float mejor = float.MaxValue;
                foreach (var b in ControladorBicho.Todos)
                {
                    if (b == bicho || b.Retirado || ronda.Inmune(b)) continue;
                    float d = Plano(b.transform.position - yo).sqrMagnitude;
                    if (d < mejor) { mejor = d; presa = b; }
                }
                if (presa == null) { deseo = Plano(centro - yo).normalized * 0.4f; }
                else
                {
                    // Apunta un poco por delante de donde va la presa.
                    Vector3 prevision = presa.transform.position + presa.Cuerpo.linearVelocity * Mathf.Lerp(0f, 0.35f, habilidad);
                    deseo = Plano(prevision - yo).normalized;
                    float distancia = Mathf.Sqrt(mejor);
                    if (distancia < 2.6f && distancia > 1.2f && Random.value < Mathf.Lerp(0.05f, 0.25f, habilidad))
                        lanzarsePendiente = true;
                }
            }
            else if (portador != null)
            {
                // Huir del que la lleva, en zigzag.
                Vector3 lejos = Plano(yo - portador.transform.position);
                float distancia = lejos.magnitude;
                float urgencia = Mathf.Clamp01(1f - (distancia - 2f) / 7f);
                float zigzag = (Mathf.PerlinNoise(Time.time * 0.9f, semilla) - 0.5f) * 140f;
                deseo = Quaternion.Euler(0f, zigzag, 0f) * (distancia > 0.01f ? lejos / distancia : Vector3.forward) * Mathf.Lerp(0.25f, 1f, urgencia);
            }
            else
            {
                // Sin bomba en juego: deambular cerca del centro.
                float a = Mathf.PerlinNoise(Time.time * 0.3f, semilla) * 720f;
                deseo = Quaternion.Euler(0f, a, 0f) * Vector3.forward * 0.4f;
            }

            // No acercarse al borde: cuanto más fuera, más tira hacia el centro.
            Vector3 haciaCentro = Plano(centro - yo);
            float fuera = haciaCentro.magnitude / Mathf.Max(1f, ronda.radioArena);
            float tiron = Mathf.Clamp01((fuera - 0.55f) / 0.35f);
            deseo = Vector3.Lerp(deseo, haciaCentro.normalized * Mathf.Max(0.6f, deseo.magnitude), tiron * Mathf.Lerp(0.6f, 1f, habilidad));

            // Los torpes se despistan un poco.
            float ruido = (Mathf.PerlinNoise(Time.time * 1.3f, semilla + 7f) - 0.5f) * Mathf.Lerp(50f, 8f, habilidad);
            direccion = Vector3.ClampMagnitude(Quaternion.Euler(0f, ruido, 0f) * deseo, 1f);
        }

        /// <summary>La barredora gira en el centro: cerca de ella, saltar de vez en cuando.</summary>
        void SaltarBarredora()
        {
            if (Time.time < siguienteSalto || !bicho.EnSuelo) return;
            float d = Plano(transform.position - ronda.Centro).magnitude;
            if (d < 4.2f)
            {
                saltarPendiente = Random.value < Mathf.Lerp(0.35f, 0.8f, habilidad);
                siguienteSalto = Time.time + Random.Range(0.6f, 1.2f);
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
