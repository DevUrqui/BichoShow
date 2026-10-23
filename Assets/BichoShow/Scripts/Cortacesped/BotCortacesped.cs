using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Bot para "El Cortacésped Mecha": se queda por el medio del césped; salta los aros bajos y
    /// las cuchillas (los hábiles, justo a tiempo) y no salta con los aros altos cerca; corre
    /// hacia fuera cuando el robot aspira (y hacia dentro si sopla), se aparta de las tuercas y
    /// sale del césped que parpadea.
    /// </summary>
    public class BotCortacesped : MonoBehaviour, IEntradaBicho
    {
        [Range(0f, 1f)] public float habilidad = 0.55f;

        public Vector2 Mover { get; private set; }
        public bool Agarrar => false;

        RondaCortacesped ronda;
        ControladorBicho bicho;
        Camera camara;
        bool saltarPendiente;
        float siguientePensar, semilla, anguloPaseo, radioPaseo, siguientePaseo, ultimoSalto;
        Vector3 direccion;

        public bool ConsumirSaltar() { bool s = saltarPendiente; saltarPendiente = false; return s; }
        public bool ConsumirLanzarse() => false;

        public void Configurar(RondaCortacesped r, float nivel)
        {
            ronda = r;
            habilidad = nivel;
        }

        void Start()
        {
            bicho = GetComponent<ControladorBicho>();
            camara = Camera.main;
            if (ronda == null) ronda = RondaCortacesped.Instancia;
            semilla = Random.value * 100f;
        }

        void Update()
        {
            var robot = ronda != null ? ronda.robot : null;
            if (robot == null || bicho == null || ronda.EstadoActual != RondaBase.Estado.Jugando || bicho.Tumbado)
            {
                Mover = Vector2.zero;
                return;
            }
            Saltar(robot);
            if (Time.time >= siguientePensar)
            {
                Pensar(robot);
                siguientePensar = Time.time + Mathf.Lerp(0.4f, 0.1f, habilidad) * Random.Range(0.8f, 1.2f);
            }
            if (Camera.main != null) camara = Camera.main;
            Mover = AStick(direccion);
        }

        /// <summary>Salta cuando un aro bajo o una cuchilla está a punto de llegar (si no viene uno alto).</summary>
        void Saltar(CortacespedMecha robot)
        {
            if (!bicho.EnSuelo || Time.time - ultimoSalto < 0.5f) return;
            Vector3 rel = Plano(transform.position - robot.Centro);
            float d = rel.magnitude;
            float v = Mathf.Max(1f, robot.VelocidadAros);
            float margen = Mathf.Lerp(0.5f, 0.22f, habilidad) + (Mathf.PerlinNoise(Time.time * 2f, semilla) - 0.5f) * Mathf.Lerp(0.4f, 0.08f, habilidad);
            bool saltar = false, altoCerca = false;
            foreach (var (r, alto) in robot.Aros)
            {
                if (r > d) continue;
                float llega = (d - r) / v;
                if (alto) { if (llega < 0.9f) altoCerca = true; }
                else if (llega < margen) saltar = true;
            }
            // Cuchillas: cuánto les falta (en tiempo) para llegar a mi ángulo.
            float miAngulo = Mathf.Atan2(rel.z, rel.x) * Mathf.Rad2Deg;
            foreach (var (angulo, velocidad) in robot.Cuchillas)
            {
                float falta = Mathf.DeltaAngle(angulo, miAngulo) * Mathf.Sign(velocidad);
                if (falta < 0f) falta += 360f;
                if (falta / Mathf.Max(1f, Mathf.Abs(velocidad)) < margen + 0.05f) saltar = true;
            }
            // Los torpes, a veces, saltan con el aro alto encima.
            if (saltar && (!altoCerca || Random.value > habilidad))
            {
                saltarPendiente = true;
                ultimoSalto = Time.time;
            }
        }

        void Pensar(CortacespedMecha robot)
        {
            Vector3 yo = transform.position;
            Vector3 rel = Plano(yo - robot.Centro);
            float d = rel.magnitude;
            Vector3 fuera = d > 0.05f ? rel / d : Vector3.forward;
            Vector3 deseo;

            if (robot.Aspirando || (robot.Anunciado(CortacespedMecha.Ataque.Aspiracion) && Random.value < habilidad))
            {
                // ¡A correr hacia fuera! (sin pasarse del borde)
                deseo = d < robot.radioPista - 2.5f ? fuera : Vector3.Cross(Vector3.up, fuera) * 0.4f;
            }
            else if (robot.Soplando)
            {
                // Sopla: hacia dentro (sin llegar al foso).
                deseo = d > robot.radioFoso + 3f ? -fuera : Vector3.Cross(Vector3.up, fuera) * 0.4f;
            }
            else
            {
                // Pasear por el medio del césped, dando vueltas.
                if (Time.time >= siguientePaseo || robot.PorcionEnPeligro(robot.Centro + new Vector3(Mathf.Cos(anguloPaseo), 0f, Mathf.Sin(anguloPaseo)) * radioPaseo))
                {
                    anguloPaseo = Mathf.Atan2(rel.z, rel.x) + Random.Range(-0.8f, 0.8f);
                    radioPaseo = Random.Range(robot.radioFoso + 4f, robot.radioPista - 4f);
                    siguientePaseo = Time.time + Random.Range(2f, 4f);
                }
                Vector3 paseo = robot.Centro + new Vector3(Mathf.Cos(anguloPaseo), 0f, Mathf.Sin(anguloPaseo)) * radioPaseo;
                deseo = Plano(paseo - yo);
                deseo = deseo.sqrMagnitude > 1f ? deseo.normalized * 0.6f : Vector3.zero;

                // Apartarse de las tuercas que vienen hacia mí.
                foreach (var p in robot.Proyectiles)
                {
                    Vector3 hacia = Plano(yo - p.position);
                    float dist = hacia.magnitude;
                    if (dist > 4.5f || dist < 0.05f) continue;
                    Vector3 vel = Plano(p.linearVelocity);
                    if (Vector3.Dot(vel, hacia) <= 0f) continue;
                    Vector3 lado = Vector3.Cross(Vector3.up, vel.normalized);
                    if (Vector3.Dot(lado, hacia) < 0f) lado = -lado;
                    deseo += lado * (1f - dist / 4.5f) * 2f * habilidad;
                }
            }

            // Si mi porción del césped se va a caer: a la de al lado.
            if (robot.PorcionEnPeligro(yo) && Random.value < 0.3f + habilidad)
            {
                Vector3 lado = Vector3.Cross(Vector3.up, fuera);
                if (robot.PorcionEnPeligro(yo + lado * 4f)) lado = -lado;
                deseo = lado;
            }

            // Nunca hacia el foso ni fuera del jardín.
            if (d < robot.radioFoso + 1.5f) deseo += fuera * 1.5f;
            if (d > robot.radioPista - 1.5f) deseo -= fuera * 1.5f;

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
