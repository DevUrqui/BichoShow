using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Bot para la final "Juntos o nada". Pregunta a la prueba en la que está su pareja a dónde
    /// ir según su papel (rol 0 / rol 1) y lo que esté haciendo su compañero, y va. Con un poco
    /// de temblor al andar, y si se atasca, salta y se desvía.
    /// </summary>
    public class BotFinal : MonoBehaviour, IEntradaBicho
    {
        [Range(0f, 1f)] public float habilidad = 0.55f;

        public Vector2 Mover { get; private set; }
        public bool Agarrar => false;

        RondaFinal ronda;
        ControladorBicho bicho;
        Camera camara;

        Vector3 objetivo;
        float intensidad;
        bool saltarPendiente;
        float siguientePensar;
        float semillaRuido;
        Vector3 posicionControl;
        float siguienteControl;
        Vector3 desvio;
        float finDesvio;

        public bool ConsumirSaltar()
        {
            bool s = saltarPendiente;
            saltarPendiente = false;
            return s;
        }

        public bool ConsumirLanzarse() => false;

        public void Configurar(RondaFinal rondaFinal, float nivel)
        {
            ronda = rondaFinal;
            habilidad = nivel;
        }

        void Start()
        {
            bicho = GetComponent<ControladorBicho>();
            camara = Camera.main;
            if (ronda == null) ronda = RondaFinal.Instancia;
            semillaRuido = Random.value * 100f;
            posicionControl = transform.position;
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
                siguientePensar = Time.time + Mathf.Lerp(0.4f, 0.12f, habilidad) * Random.Range(0.8f, 1.2f);
            }

            Conducir();
            ComprobarAtasco();
        }

        void Pensar()
        {
            int equipo = RondaBase.EquipoDe(bicho);
            var paso = ronda.PasoActual(equipo);
            if (paso == null)
            {
                objetivo = transform.position;
                intensidad = 0f;
                return;
            }
            objetivo = paso.ObjetivoBot(bicho, ronda.Rol(bicho), ronda.Companero(bicho), out intensidad);
        }

        void Conducir()
        {
            if (Camera.main != null) camara = Camera.main;
            Vector3 hacia = Plano(objetivo - transform.position);
            float distancia = hacia.magnitude;
            Vector3 direccion = distancia > 0.01f ? hacia / distancia : Vector3.zero;

            float ruidoMax = Mathf.Lerp(25f, 4f, habilidad);
            float ruido = (Mathf.PerlinNoise(Time.time * 0.8f, semillaRuido) - 0.5f) * 2f * ruidoMax;
            direccion = Quaternion.Euler(0f, ruido, 0f) * direccion;
            if (Time.time < finDesvio) direccion = (direccion + desvio * 1.3f).normalized;

            float fuerza = intensidad * Mathf.Clamp01(distancia / 0.8f);
            Mover = AStick(direccion * fuerza);
        }

        void ComprobarAtasco()
        {
            if (Time.time < siguienteControl) return;
            siguienteControl = Time.time + 1.3f;

            Vector3 ahora = transform.position;
            float avance = Plano(ahora - posicionControl).magnitude;
            posicionControl = ahora;

            // Empujando el bloque se avanza poco (o nada, hasta que llega el compañero): no es un atasco.
            var paso = ronda.PasoActual(RondaBase.EquipoDe(bicho));
            if (paso != null && paso.HaciendoFuerza(bicho)) return;

            bool deberiaMoverse = intensidad > 0.1f && Plano(objetivo - ahora).magnitude > 1.8f;
            if (deberiaMoverse && avance < 0.3f)
            {
                saltarPendiente = true;
                Vector3 haciaObjetivo = Plano(objetivo - ahora).normalized;
                desvio = Vector3.Cross(Vector3.up, haciaObjetivo) * (Random.value < 0.5f ? -0.5f : 0.5f);
                finDesvio = Time.time + 0.5f;
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

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position + Vector3.up, objetivo + Vector3.up);
            Gizmos.DrawWireSphere(objetivo + Vector3.up, 0.3f);
        }
    }
}
