using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Prueba "Costura a la fuga" (todos contra todos): carrera de obstáculos sobre una mesa de
    /// costura, de la Tabla de Corte a la meta junto al dedal. Puntos por orden de llegada; quien
    /// no llega, según lo lejos que haya ido. Cuando llega el primero, a los demás les quedan
    /// unos segundos. Si te caes, vuelves al último punto de control que hayas cruzado.
    /// </summary>
    public class RondaCostura : RondaBase
    {
        [Header("Carrera")]
        [Tooltip("Puntos de control, en orden (sin contar la salida).")]
        public PuntoControlCarrera[] puntosControl;
        public MetaCarrera meta;
        [Tooltip("Todos los puntos del camino de los bots.")]
        public PuntoRuta[] ruta;
        [Tooltip("Por dónde empiezan los bots.")]
        public PuntoRuta inicioRuta;
        [Tooltip("Z de la salida y de la meta (para el porcentaje de carrera de quien no llega).")]
        public float zSalida;
        public float zMeta = 145f;
        [Tooltip("La carrera dura al menos esto, aunque el show pida rondas más cortas (s).")]
        public float duracionMinima = 150f;
        [Tooltip("Cuando llega el primero, a los demás les quedan como mucho estos segundos.")]
        public float tiempoTrasPrimero = 30f;
        [Tooltip("Tras cruzar la meta, el bicho celebra este rato y deja el circuito (así la cámara sigue a los que corren).")]
        public float celebracionMeta = 2.5f;

        public static RondaCostura Instancia { get; private set; }

        readonly float[] avance = new float[4];
        readonly int[] llegada = new int[4];      // 0 = no ha llegado; 1 = primero…
        readonly int[] control = new int[4];
        readonly List<ControladorBicho> corredores = new List<ControladorBicho>();
        readonly List<(ControladorBicho bicho, float cuando)> porRetirar = new List<(ControladorBicho, float)>();
        int llegados;

        public override string Titulo => "COSTURA A LA FUGA";
        protected override string Descripcion => "¡Carrera por la mesa de costura! Esquiva los carretes, cruza el acordeón y las cremalleras y sube al dedal.";
        protected override string ConsejoInicio => "¡A la meta! Si te caes, vuelves a la última bandera que cruzaste";
        protected override string AccionX => "placaje (¡empuja a los rivales!)";
        public override bool Individual => true;
        public override string NombreEquipo(int equipo) => equipo == 0 ? "Rojo" : "Azul";
        public override Color ColorEquipo(int equipo) => equipo == 0 ? new Color(1f, 0.38f, 0.35f) : new Color(0.3f, 0.6f, 1f);
        public override int PuntosEquipo(int equipo) => PuntosJugador(equipo) + PuntosJugador(equipo + 2);

        /// <summary>Llegar: 200 el primero, 170 el segundo… Sin llegar: el porcentaje de carrera (siempre menos).</summary>
        public override int PuntosJugador(int indice)
        {
            if (indice < 0 || indice >= 4) return 0;
            return llegada[indice] > 0 ? 200 - 30 * (llegada[indice] - 1) : Mathf.FloorToInt(avance[indice] * 99f);
        }

        protected override string EtiquetaJugador(int indice)
        {
            if (indice < 0 || indice >= 4) return null;
            return llegada[indice] > 0 ? $"¡{llegada[indice]}º EN META!" : $"{Mathf.FloorToInt(avance[indice] * 100f)}% de carrera";
        }

        public bool HaLlegado(ControladorBicho b) => b != null && b.IndiceJugador >= 0 && b.IndiceJugador < 4 && llegada[b.IndiceJugador] > 0;

        void Awake() => Instancia = this;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instancia == this) Instancia = null;
        }

        // ---------------------------------------------------------------------
        // Ciclo
        // ---------------------------------------------------------------------

        protected override void CrearBot(int indice)
        {
            var bot = gestor.CrearBot<BotCarrera>(indice);
            if (bot != null) bot.Configurar(this, habilidadBots);
        }

        protected override IEntradaBicho PonerCerebroBot(GameObject bicho)
        {
            var bot = bicho.AddComponent<BotCarrera>();
            bot.Configurar(this, habilidadBots);
            return bot;
        }

        protected override void AlPrepararRonda()
        {
            llegados = 0;
            porRetirar.Clear();
            for (int i = 0; i < 4; i++) { avance[i] = 0f; llegada[i] = 0; control[i] = 0; }
            // Quien se cae vuelve a la salida (hasta que cruce un punto de control).
            if (gestor != null)
                foreach (var b in ControladorBicho.Todos)
                {
                    var punto = gestor.PuntoAparicion(b.IndiceJugador);
                    if (punto != null) b.FijarPuntoReaparicion(punto.position, punto.rotation);
                }
        }

        protected override void AlDespuesDeBots()
        {
            if (gestor == null) return;
            foreach (var b in ControladorBicho.Todos)
            {
                var punto = gestor.PuntoAparicion(b.IndiceJugador);
                if (punto != null) b.FijarPuntoReaparicion(punto.position, punto.rotation);
            }
        }

        protected override void AlEmpezarRonda()
        {
            TiempoRestante = Mathf.Max(TiempoRestante, duracionMinima);
            corredores.Clear();
            foreach (var b in ControladorBicho.Todos) if (!b.Retirado) corredores.Add(b);
            Mensaje("¡A CORRER!", EstiloShow.Oro, 1.5f);
        }

        protected override void ActualizarJugando()
        {
            foreach (var b in ControladorBicho.Todos)
            {
                int i = b.IndiceJugador;
                if (i < 0 || i >= 4 || llegada[i] > 0) continue;
                float a = Mathf.Clamp01((b.transform.position.z - zSalida) / Mathf.Max(1f, zMeta - zSalida));
                if (a > avance[i]) avance[i] = Mathf.Min(a, 0.99f);
            }

            // Los que ya han llegado, tras celebrarlo, dejan el circuito.
            for (int k = porRetirar.Count - 1; k >= 0; k--)
            {
                if (Time.time < porRetirar[k].cuando) continue;
                if (porRetirar[k].bicho != null) porRetirar[k].bicho.Retirar(true);
                porRetirar.RemoveAt(k);
            }

            // ¿Han llegado todos?
            corredores.RemoveAll(b => b == null);
            bool todos = corredores.Count > 0;
            foreach (var b in corredores) if (!HaLlegado(b)) { todos = false; break; }
            if (todos) TerminarRonda();
        }

        protected override void AlTerminarRonda()
        {
            // Vuelven los que ya habían llegado (para celebrar en el podio de la meta).
            foreach (var b in corredores)
                if (b != null && b.Retirado && HaLlegado(b)) b.Retirar(false);
            porRetirar.Clear();
        }

        // ---------------------------------------------------------------------
        // Puntos de control y meta
        // ---------------------------------------------------------------------

        public void PuntoAlcanzado(ControladorBicho b, PuntoControlCarrera punto)
        {
            int i = b.IndiceJugador;
            if (i < 0 || i >= 4 || EstadoActual != Estado.Jugando || punto.indice <= control[i]) return;
            control[i] = punto.indice;
            b.FijarPuntoReaparicion(punto.Reaparicion(i), punto.Orientacion);
            Sonidos.Reproducir(Sonido.Click, b.transform.position, b.EsBot ? 0.4f : 0.8f, 1.5f);
            Estadisticas.Evento("punto de control", b, punto.indice.ToString());
        }

        public void Llegar(ControladorBicho b)
        {
            int i = b.IndiceJugador;
            if (i < 0 || i >= 4 || EstadoActual != Estado.Jugando || llegada[i] > 0 || b.Retirado) return;
            llegada[i] = ++llegados;
            avance[i] = 1f;
            Color color = gestor != null ? gestor.ColorDe(i) : Color.white;
            Mensaje($"¡{NombreJugador(b)} llega {llegados}º!", color, 2.5f);
            Sonidos.Reproducir(llegados == 1 ? Sonido.Victoria : Sonido.Logro);
            Sonidos.Reproducir(Sonido.Aplausos, null, 0.7f);
            Efectos.Impacto(b.Cuerpo.worldCenterOfMass + Vector3.up, color, 1f);
            if (b.TryGetComponent(out AnimadorBicho animador)) animador.Celebrar(celebracionMeta + 1f);
            if (meta != null) b.FijarPuntoReaparicion(meta.transform.position, meta.transform.rotation);
            Estadisticas.Evento("meta", b, $"{llegados}º");
            porRetirar.Add((b, Time.time + celebracionMeta));

            if (llegados == 1 && TiempoRestante > tiempoTrasPrimero)
            {
                TiempoRestante = tiempoTrasPrimero;
                Mensaje($"¡Ya hay ganador! Quedan {tiempoTrasPrimero:0} segundos", EstiloShow.Oro, 3f);
                Sonidos.Reproducir(Sonido.Alarma, null, 0.6f);
            }
        }

        // ---------------------------------------------------------------------
        // Bots
        // ---------------------------------------------------------------------

        /// <summary>El punto del camino más cercano (prefiere los de delante): para seguir tras caerse.</summary>
        public PuntoRuta PuntoRutaCercano(Vector3 posicion)
        {
            PuntoRuta mejor = inicioRuta;
            float mejorCoste = float.MaxValue;
            if (ruta == null) return mejor;
            foreach (var p in ruta)
            {
                if (p == null) continue;
                Vector3 d = p.transform.position - posicion;
                float coste = d.magnitude + (d.z < -1f ? 8f : 0f) + Mathf.Max(0f, d.y - 1f) * 4f;
                if (coste < mejorCoste) { mejorCoste = coste; mejor = p; }
            }
            return mejor;
        }
    }
}
