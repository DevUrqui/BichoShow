using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Prueba "La Corona" (todos contra todos):
    ///  - Hay una corona en lo alto de una tarta gigante. Quien la toca, se la pone.
    ///  - El rey suma 1 punto por segundo (2 en los últimos segundos) y anda algo más lento.
    ///  - Si alguien lo derriba (placaje, empujón, trampa…), la corona sale volando y es de
    ///    quien la pille. Si se cae del mapa, vuelve a lo alto de la tarta.
    /// </summary>
    public class RondaCorona : RondaBase
    {
        [Header("La Corona")]
        public Corona corona;
        [Tooltip("Dónde aparece la corona (lo alto de la tarta).")]
        public Transform aparicionCorona;
        public Transform centroArena;
        public float radioArena = 10f;
        [Tooltip("Altura del piso de arriba de la tarta (para los bots).")]
        public float alturaPisoAlto = 1.2f;
        public float radioPisoAlto = 4.5f;

        public float puntosPorSegundo = 1f;
        [Tooltip("En los últimos segundos los puntos valen doble.")]
        public float segundosPuntosDobles = 15f;
        public float distanciaCoger = 1.3f;
        [Tooltip("Lo que anda el rey (la corona pesa).")]
        public float velocidadRey = 0.9f;

        readonly float[] puntos = new float[4];
        bool avisoDobles;

        public static RondaCorona Instancia { get; private set; }

        public override string Titulo => "LA CORONA";
        protected override string Descripcion => "Ponte la corona y aguántala: cada segundo con ella es un punto. ¡Quítasela al rey de un placaje!";
        protected override string ConsejoInicio => "¡Coge la corona de lo alto de la tarta y no la sueltes!";
        protected override string AccionX => "placaje (¡tira al rey!)";
        public override bool Individual => true;
        public override int PuntosJugador(int indice) => indice >= 0 && indice < puntos.Length ? Mathf.FloorToInt(puntos[indice]) : 0;
        public override string NombreEquipo(int equipo) => equipo == 0 ? "Rojo" : "Azul";
        public override Color ColorEquipo(int equipo) => equipo == 0 ? new Color(1f, 0.38f, 0.35f) : new Color(0.3f, 0.6f, 1f);
        public override int PuntosEquipo(int equipo) => PuntosJugador(equipo) + PuntosJugador(equipo + 2);

        public ControladorBicho Rey => corona != null ? corona.Portador : null;
        public Vector3 Centro => centroArena != null ? centroArena.position : transform.position;
        bool Dobles => TiempoRestante <= segundosPuntosDobles;

        protected override string EtiquetaJugador(int indice)
        {
            var rey = Rey;
            return rey != null && rey.IndiceJugador == indice ? (Dobles ? "¡REY! x2" : "¡REY!") : null;
        }

        public override IEnumerable<Vector3> ObjetivosSabotaje()
        {
            if (corona != null) yield return corona.transform.position;
        }

        void Awake() => Instancia = this;

        protected override void Start()
        {
            base.Start();
            ControladorBicho.AlgunoDerribado += AlDerribarAlguien;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ControladorBicho.AlgunoDerribado -= AlDerribarAlguien;
            if (Instancia == this) Instancia = null;
            foreach (var b in ControladorBicho.Todos) b.MultiplicadorVelocidad = 1f;
        }

        protected override void CrearBot(int indice)
        {
            var bot = gestor.CrearBot<BotCorona>(indice);
            if (bot != null) bot.Configurar(this, habilidadBots);
        }

        protected override IEntradaBicho PonerCerebroBot(GameObject bicho)
        {
            var bot = bicho.AddComponent<BotCorona>();
            bot.Configurar(this, habilidadBots);
            return bot;
        }

        protected override void AlPrepararRonda()
        {
            for (int i = 0; i < puntos.Length; i++) puntos[i] = 0f;
            avisoDobles = false;
            foreach (var b in ControladorBicho.Todos) b.MultiplicadorVelocidad = 1f;
            ColocarCorona();
        }

        protected override void AlEmpezarRonda() => Mensaje("¡A POR LA CORONA!", EstiloShow.Oro, 1.5f);

        protected override void ActualizarJugando()
        {
            if (corona == null) return;
            var rey = Rey;

            if (rey != null)
            {
                if (rey.Retirado || rey.transform.position.y < -3f) { CaerCorona(rey, Vector3.up * 2f); ColocarCorona(); return; }
                puntos[rey.IndiceJugador] += Time.deltaTime * puntosPorSegundo * (Dobles ? 2f : 1f);
            }
            else
            {
                if (corona.transform.position.y < -5f) { ColocarCorona(); return; }
                // ¿Alguien la toca?
                Vector3 p = corona.transform.position;
                ControladorBicho quien = null;
                float mejor = distanciaCoger * distanciaCoger;
                foreach (var b in ControladorBicho.Todos)
                {
                    if (b.Retirado || b.Tumbado || !corona.PuedeCoger(b)) continue;
                    float d = (b.Cuerpo.worldCenterOfMass - p).sqrMagnitude;
                    if (d < mejor) { mejor = d; quien = b; }
                }
                if (quien != null) Coronar(quien);
            }

            if (!avisoDobles && Dobles)
            {
                avisoDobles = true;
                Mensaje("¡ÚLTIMOS SEGUNDOS: PUNTOS DOBLES!", EstiloShow.Oro, 3f);
                Sonidos.Reproducir(Sonido.Alarma);
            }
        }

        protected override void AlTerminarRonda()
        {
            foreach (var b in ControladorBicho.Todos) b.MultiplicadorVelocidad = 1f;
        }

        void ColocarCorona()
        {
            if (corona == null) return;
            Vector3 p = aparicionCorona != null ? aparicionCorona.position : Centro + Vector3.up * 3f;
            corona.Colocar(p);
            Efectos.Impacto(p, EstiloShow.Oro, 0.5f);
            Sonidos.Reproducir(Sonido.Aparecer, p);
        }

        void Coronar(ControladorBicho b)
        {
            corona.Coger(b);
            b.MultiplicadorVelocidad = velocidadRey;
            Sonidos.Reproducir(Sonido.Logro, b.transform.position, 0.8f);
            Efectos.Impacto(b.Cuerpo.worldCenterOfMass + Vector3.up * 1.2f, EstiloShow.Oro, 0.6f);
            Mensaje($"¡{NombreJugador(b)} es el rey!", gestor != null ? gestor.ColorDe(b.IndiceJugador) : Color.white, 1.5f);
            Estadisticas.Evento("corona", b);
        }

        void AlDerribarAlguien(ControladorBicho b)
        {
            if (EstadoActual != Estado.Jugando || corona == null || b != Rey) return;
            Vector3 lado = Random.insideUnitSphere;
            lado.y = 0f;
            CaerCorona(b, Vector3.up * 7f + lado.normalized * 3.5f);
        }

        void CaerCorona(ControladorBicho rey, Vector3 velocidad)
        {
            rey.MultiplicadorVelocidad = 1f;
            corona.Soltar(velocidad);
            Sonidos.Reproducir(Sonido.Lanzar, rey.transform.position);
            Sonidos.Reproducir(Sonido.Ooh, rey.transform.position, 0.7f);
            Estadisticas.Evento("pierde la corona", rey);
        }
    }
}
