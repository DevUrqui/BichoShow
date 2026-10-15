using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Bot para el Voley atados. Calcula dónde va a caer la pelota; si cae en su campo y es
    /// el de su pareja que más cerca está, va a por ella. Cuando la tiene a tiro, la golpea
    /// hacia el campo rival (y salta para rematar si viene alta). Si no, vuelve a su sitio.
    /// Es igual de torpe que un jugador: usa los mismos "botones".
    /// </summary>
    public class BotVoley : MonoBehaviour, IEntradaBicho
    {
        [Tooltip("0 = muy torpe y lento de reflejos, 1 = bastante preciso.")]
        [Range(0f, 1f)] public float habilidad = 0.55f;

        public Vector2 Mover { get; private set; }
        public bool Agarrar => false;

        RondaVoley ronda;
        ControladorBicho bicho;
        Camera camara;

        Vector3 objetivo;
        Vector3 errorPrediccion;
        bool saltarPendiente, golpearPendiente;
        float siguientePensar;
        float apuntandoHasta = -10f;
        Vector3 direccionApunte;

        public bool ConsumirSaltar()
        {
            bool s = saltarPendiente;
            saltarPendiente = false;
            return s;
        }

        public bool ConsumirLanzarse()
        {
            bool g = golpearPendiente;
            golpearPendiente = false;
            return g;
        }

        public void Configurar(RondaVoley rondaVoley, float nivel)
        {
            ronda = rondaVoley;
            habilidad = nivel;
        }

        void Start()
        {
            bicho = GetComponent<ControladorBicho>();
            camara = Camera.main;
            if (ronda == null) ronda = FindAnyObjectByType<RondaVoley>();
        }

        int Equipo => RondaBase.EquipoDe(bicho);

        /// <summary>Jugadores 1-2 juegan delante (cerca de la red) y 3-4 detrás.</summary>
        Vector3 SitioBase
        {
            get
            {
                Vector3 centro = ronda.CentroCampo(Equipo);
                Vector3 haciaRival = ronda.HaciaRival(Equipo);
                bool delante = bicho.IndiceJugador < 2;
                return centro + haciaRival * (delante ? 1.5f : -2f);
            }
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

            // Un instante apuntando hacia el campo rival para que el golpe salga en esa dirección.
            if (Time.time < apuntandoHasta)
            {
                Mover = AStick(direccionApunte * 0.4f);
                return;
            }

            Vector3 hacia = Plano(objetivo - transform.position);
            float distancia = hacia.magnitude;
            float intensidad = distancia < 0.4f ? 0f : Mathf.Clamp01(distancia / 1.5f);
            Mover = AStick(distancia > 0.01f ? hacia / distancia * intensidad : Vector3.zero);
        }

        void Pensar()
        {
            var pelota = ronda.Pelota;
            if (pelota == null || pelota.PuntoResuelto)
            {
                objetivo = SitioBase;
                return;
            }

            Vector3 caida = PrediccionCaida(pelota);
            bool caeEnMiCampo = ronda.CampoDe(caida) == Equipo;

            if (caeEnMiCampo && SoyElMasCercano(caida))
            {
                // Un error de cálculo que cambia poco a poco (peor cuanto más torpe).
                if (Random.value < 0.15f) errorPrediccion = Random.insideUnitSphere * Mathf.Lerp(1.6f, 0.2f, habilidad);
                errorPrediccion.y = 0f;
                objetivo = caida + errorPrediccion;
            }
            else
            {
                objetivo = SitioBase;
            }

            // ¿A tiro? Golpear (y saltar antes si viene alta y cerca de la red, para rematar).
            Vector3 aPelota = pelota.transform.position - (transform.position + Vector3.up * 0.6f);
            float horizontal = Plano(aPelota).magnitude;
            bool enMiLado = ronda.CampoDe(pelota.transform.position) == Equipo;

            if (enMiLado && horizontal < 2.2f && aPelota.y > 1.2f && aPelota.y < 3f && pelota.Cuerpo.linearVelocity.y < 0f
                && bicho.EnSuelo && Random.value < Mathf.Lerp(0.15f, 0.5f, habilidad))
                saltarPendiente = true;

            if (enMiLado && aPelota.magnitude < bicho.alcanceGolpe * 0.9f && Random.value < Mathf.Lerp(0.5f, 0.95f, habilidad))
            {
                Vector3 adelante = ronda.HaciaRival(Equipo);
                Vector3 lateral = Vector3.Cross(Vector3.up, adelante) * Random.Range(-0.8f, 0.8f);
                direccionApunte = (adelante * Random.Range(-0.3f, 1f) + lateral).normalized;
                apuntandoHasta = Time.time + 0.08f;
                golpearPendiente = true;
            }
        }

        /// <summary>Dónde estará la pelota cuando baje a la altura de golpeo (con su gravedad).</summary>
        Vector3 PrediccionCaida(PelotaVoley pelota)
        {
            Vector3 p = pelota.transform.position;
            Vector3 v = pelota.Cuerpo.linearVelocity;
            float g = Mathf.Max(0.1f, pelota.GravedadEfectiva);
            float altura = 1.3f;
            float dy = p.y - altura;
            float discriminante = v.y * v.y + 2f * g * dy;
            float t = discriminante > 0f ? (v.y + Mathf.Sqrt(discriminante)) / g : 0f;
            return new Vector3(p.x + v.x * t, 0f, p.z + v.z * t);
        }

        bool SoyElMasCercano(Vector3 punto)
        {
            float mia = Plano(punto - transform.position).sqrMagnitude;
            foreach (var otro in ControladorBicho.Todos)
            {
                if (otro == bicho || RondaBase.EquipoDe(otro) != Equipo) continue;
                if (Plano(punto - otro.transform.position).sqrMagnitude < mia - 0.5f) return false;
            }
            return true;
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
