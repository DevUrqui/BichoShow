using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Bot para "Lluvia de muebles": huye de las sombras (los hábiles, antes y mejor); si no hay
    /// peligro cerca, pasea y, si ve a otro junto a una sombra, va a empujarlo debajo.
    /// </summary>
    public class BotLluvia : MonoBehaviour, IEntradaBicho
    {
        [Range(0f, 1f)] public float habilidad = 0.55f;

        public Vector2 Mover { get; private set; }
        public bool Agarrar => false;

        RondaLluvia ronda;
        ControladorBicho bicho;
        Camera camara;
        bool lanzarsePendiente;
        float siguientePensar, siguientePaseo, semilla;
        Vector3 direccion, paseo;

        public bool ConsumirSaltar() => false;
        public bool ConsumirLanzarse() { bool s = lanzarsePendiente; lanzarsePendiente = false; return s; }

        public void Configurar(RondaLluvia rondaLluvia, float nivel)
        {
            ronda = rondaLluvia;
            habilidad = nivel;
        }

        void Start()
        {
            bicho = GetComponent<ControladorBicho>();
            camara = Camera.main;
            if (ronda == null) ronda = RondaLluvia.Instancia;
            semilla = Random.value * 100f;
            paseo = transform.position;
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
                // Los torpes reaccionan tarde a las sombras.
                siguientePensar = Time.time + Mathf.Lerp(0.45f, 0.1f, habilidad) * Random.Range(0.8f, 1.2f);
            }
            if (Camera.main != null) camara = Camera.main;
            Mover = AStick(direccion);
        }

        void Pensar()
        {
            Vector3 yo = transform.position;
            Vector3 centro = ronda.Centro;

            // 1) Huir de las sombras cercanas.
            Vector3 huida = Vector3.zero;
            float margen = Mathf.Lerp(0.6f, 1.6f, habilidad);
            foreach (var (punto, radio) in ronda.Peligros())
            {
                Vector3 lejos = Plano(yo - punto);
                float d = lejos.magnitude;
                float limite = radio + margen;
                if (d < limite)
                    huida += (d > 0.05f ? lejos / d : Random.insideUnitSphere.normalized) * (1f - d / limite) * 2f;
            }

            Vector3 deseo;
            if (huida.sqrMagnitude > 0.01f)
            {
                deseo = huida.normalized;
            }
            else
            {
                // 2) ¿Alguien junto a una sombra? A empujarlo.
                ControladorBicho victima = null;
                float mejor = 5f * 5f;
                foreach (var b in ControladorBicho.Todos)
                {
                    if (b == bicho || b.Retirado) continue;
                    float d = Plano(b.transform.position - yo).sqrMagnitude;
                    if (d > mejor) continue;
                    foreach (var (punto, radio) in ronda.Peligros())
                        if (Plano(b.transform.position - punto).magnitude < radio + 1.5f) { victima = b; mejor = d; break; }
                }
                if (victima != null && Random.value < habilidad)
                {
                    deseo = Plano(victima.transform.position - yo).normalized;
                    float d = Mathf.Sqrt(mejor);
                    if (d < 2.5f && d > 1f && Random.value < 0.25f) lanzarsePendiente = true;
                }
                else
                {
                    // 3) Pasear por la zona.
                    if (Time.time >= siguientePaseo || Plano(paseo - yo).magnitude < 1f)
                    {
                        paseo = centro + new Vector3(Random.Range(-ronda.medioTamano.x, ronda.medioTamano.x) * 0.7f, 0f,
                                                     Random.Range(-ronda.medioTamano.y, ronda.medioTamano.y) * 0.7f);
                        siguientePaseo = Time.time + Random.Range(2f, 4f);
                    }
                    deseo = Plano(paseo - yo).normalized * 0.5f;
                }
            }

            // Sin salirse del suelo.
            Vector3 rel = yo - centro;
            float fx = Mathf.Abs(rel.x) / Mathf.Max(1f, ronda.medioTamano.x), fz = Mathf.Abs(rel.z) / Mathf.Max(1f, ronda.medioTamano.y);
            float tiron = Mathf.Clamp01((Mathf.Max(fx, fz) - 0.75f) / 0.2f);
            deseo = Vector3.Lerp(deseo, Plano(centro - yo).normalized, tiron);

            float ruido = (Mathf.PerlinNoise(Time.time * 1.3f, semilla) - 0.5f) * Mathf.Lerp(50f, 8f, habilidad);
            direccion = Vector3.ClampMagnitude(Quaternion.Euler(0f, ruido, 0f) * deseo, 1f);
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
