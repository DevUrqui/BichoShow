using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Gran final "La Cumbre del Girasol" (todos contra todos): subida en espiral alrededor del
    /// tallo de un girasol gigante, sin puntos de control. Arriba, la cabeza del girasol gira y,
    /// en su centro, el arco de META gira con ella: hay que cruzarlo por su boca. Puntos por orden
    /// de llegada. La savia inunda el invernadero desde abajo: quien queda por debajo, se pega y
    /// queda eliminado (puntúa por lo alto que llegó). Cuando llega el primero, la savia se desborda.
    ///
    /// Eventos de show: al minuto, la fiebre del polen (el doble de bolas, luces doradas y sirena);
    /// y si alguien va muy destacado, el abejorro con cámara se le pone delante.
    /// </summary>
    public class RondaCumbre : RondaRuta
    {
        public override string IdRanking => "cumbre";

        [Header("La Cumbre")]
        public Savia savia;
        public BolasPolen bolas;
        public AbejorroDron dron;
        [Tooltip("El arco de META (para la reaparición y el confeti).")]
        public MetaGiratoria meta;
        [Tooltip("Luz principal (parpadea dorada en la fiebre del polen).")]
        public Light sol;
        [Tooltip("Cañones de confeti de las gradas (se disparan cuando llega el primero).")]
        public ParticleSystem[] confeti;
        [Tooltip("Altura de la cima (para el porcentaje de subida).")]
        public float alturaCima = 56f;
        [Tooltip("La final dura al menos esto, aunque el show pida rondas más cortas (s).")]
        public float duracionMinima = 200f;
        [Tooltip("Cuando llega el primero, a los demás les quedan como mucho estos segundos.")]
        public float tiempoTrasPrimero = 30f;
        [Tooltip("Cuando llega el primero, la savia sube estas veces más deprisa.")]
        public float saviaDesbordada = 2.5f;
        [Tooltip("Tras cruzar la meta, el bicho celebra este rato y deja la pista (así la cámara sigue a los que suben).")]
        public float celebracionMeta = 2.5f;
        public float momentoFiebre = 60f;
        public float duracionFiebre = 20f;
        [Tooltip("Ventaja de altura (m) sobre el segundo para que el dron se plante delante del primero.")]
        public float ventajaParaDron = 8f;

        public static RondaCumbre Instancia { get; private set; }

        readonly float[] altura = new float[4];
        readonly bool[] pegado = new bool[4];
        readonly int[] llegada = new int[4];      // 0 = no ha llegado; 1 = primero…
        readonly List<ControladorBicho> corredores = new List<ControladorBicho>();
        readonly List<(ControladorBicho bicho, float cuando)> porRetirar = new List<(ControladorBicho, float)>();
        int llegados;
        bool fiebreHecha, avisoDron, avisoSavia;
        Color colorSol;
        float intensidadSol;
        float siguienteLider;

        public override string Titulo => "LA CUMBRE DEL GIRASOL";
        protected override string Descripcion => "¡La gran final! Sube por la espiral del girasol antes de que te alcance la savia y cruza el arco de META, que gira en lo alto.";
        protected override string ConsejoInicio => "¡Arriba! La savia sube: no te quedes atrás. Arriba, entra en el arco por delante";
        protected override string AccionX => "placaje (¡empuja a los rivales!)";
        public override bool Individual => true;
        public override string NombreEquipo(int equipo) => equipo == 0 ? "Rojo" : "Azul";
        public override Color ColorEquipo(int equipo) => equipo == 0 ? new Color(1f, 0.38f, 0.35f) : new Color(0.3f, 0.6f, 1f);
        public override int PuntosEquipo(int equipo) => PuntosJugador(equipo) + PuntosJugador(equipo + 2);

        /// <summary>Llegar: 300 el primero, 250 el segundo… Sin llegar: lo alto que se ha subido (siempre menos).</summary>
        public override int PuntosJugador(int indice)
        {
            if (indice < 0 || indice >= 4) return 0;
            if (llegada[indice] > 0) return 300 - 50 * (llegada[indice] - 1);
            return Mathf.FloorToInt(Mathf.Clamp01(altura[indice] / Mathf.Max(1f, alturaCima)) * 140f);
        }

        protected override string EtiquetaJugador(int indice)
        {
            if (indice < 0 || indice >= 4) return null;
            if (llegada[indice] > 0) return $"¡{llegada[indice]}º EN LA CUMBRE!";
            if (pegado[indice]) return $"pegado en la savia ({Mathf.FloorToInt(altura[indice])} m)";
            return $"{Mathf.FloorToInt(altura[indice])} m de altura";
        }

        public override bool HaLlegado(ControladorBicho b) =>
            b != null && b.IndiceJugador >= 0 && b.IndiceJugador < 4 && (llegada[b.IndiceJugador] > 0 || pegado[b.IndiceJugador]);

        protected override float Avance(Vector3 punto) => punto.y;

        void Awake()
        {
            Instancia = this;
            if (sol != null) { colorSol = sol.color; intensidadSol = sol.intensity; }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instancia == this) Instancia = null;
        }

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

        // ---------------------------------------------------------------------
        // Ciclo
        // ---------------------------------------------------------------------

        protected override void AlPrepararRonda()
        {
            llegados = 0;
            porRetirar.Clear();
            fiebreHecha = avisoDron = avisoSavia = false;
            for (int i = 0; i < 4; i++) { altura[i] = 0f; pegado[i] = false; llegada[i] = 0; }
            if (savia != null) savia.Parar();
            if (bolas != null) bolas.Parar();
            if (dron != null) dron.Grabar(null);
            VolverASalida();
        }

        protected override void AlDespuesDeBots() => VolverASalida();

        /// <summary>Sin puntos de control: quien se cae al suelo del invernadero vuelve a la salida.</summary>
        void VolverASalida()
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
            if (savia != null) savia.Empezar();
            if (bolas != null) bolas.Empezar(4f);
            Mensaje("¡A LA CUMBRE!", EstiloShow.Oro, 1.5f);
        }

        protected override void ActualizarJugando()
        {
            foreach (var b in ControladorBicho.Todos)
            {
                int i = b.IndiceJugador;
                if (i < 0 || i >= 4 || b.Retirado || llegada[i] > 0) continue;
                altura[i] = Mathf.Max(altura[i], Mathf.Min(b.transform.position.y, alturaCima));
                // ¿Le ha alcanzado la savia?
                if (savia != null && savia.Subiendo && b.transform.position.y < savia.Altura - 0.2f) Pegar(b);
            }

            if (!avisoSavia && savia != null && savia.Subiendo)
            {
                avisoSavia = true;
                Mensaje("¡La savia empieza a subir!", new Color(1f, 0.7f, 0.25f), 2.5f);
                Sonidos.Reproducir(Sonido.Alarma, null, 0.6f);
            }

            if (!fiebreHecha && TiempoJugado >= momentoFiebre) FiebreDelPolen();
            if (sol != null) ParpadeoDorado();
            if (Time.time >= siguienteLider) { siguienteLider = Time.time + 0.5f; VigilarLider(); }

            // Los que ya han llegado, tras celebrarlo, dejan la pista.
            for (int k = porRetirar.Count - 1; k >= 0; k--)
            {
                if (Time.time < porRetirar[k].cuando) continue;
                if (porRetirar[k].bicho != null) porRetirar[k].bicho.Retirar(true);
                porRetirar.RemoveAt(k);
            }

            // ¿Queda alguien subiendo?
            corredores.RemoveAll(b => b == null);
            bool alguien = false;
            foreach (var b in corredores) if (!HaLlegado(b) && !b.Retirado) { alguien = true; break; }
            if (!alguien && corredores.Count > 0 && porRetirar.Count == 0) TerminarRonda();
        }

        protected override void AlTerminarRonda()
        {
            if (savia != null) savia.Parar();
            if (bolas != null) bolas.Parar();
            if (sol != null) { sol.color = colorSol; sol.intensity = intensidadSol; }
            porRetirar.Clear();
            // Vuelven todos a la cima (los que llegaron y los pegados), para ver el final juntos.
            foreach (var b in corredores)
                if (b != null && b.Retirado && HaLlegado(b))
                {
                    if (meta != null) b.FijarPuntoReaparicion(meta.transform.position + meta.transform.forward * 3f, meta.transform.rotation);
                    b.Retirar(false);
                }
        }

        void Pegar(ControladorBicho b)
        {
            int i = b.IndiceJugador;
            if (pegado[i]) return;
            pegado[i] = true;
            Color color = gestor != null ? gestor.ColorDe(i) : Color.white;
            Mensaje($"¡{NombreJugador(b)} se ha quedado pegado en la savia!", color, 2.5f);
            Sonidos.Reproducir(Sonido.Ooh);
            Sonidos.Reproducir(Sonido.Caida, b.transform.position, 0.8f, 0.7f);
            Efectos.Polvo(b.transform.position, 1f);
            Estadisticas.Evento("eliminado", b, "savia");
            b.Retirar(true);
        }

        /// <summary>Lo llama el arco de META al cruzarlo por su boca.</summary>
        public void Llegar(ControladorBicho b)
        {
            int i = b.IndiceJugador;
            if (i < 0 || i >= 4 || EstadoActual != Estado.Jugando || llegada[i] > 0 || pegado[i] || b.Retirado) return;
            llegada[i] = ++llegados;
            altura[i] = alturaCima;
            Color color = gestor != null ? gestor.ColorDe(i) : Color.white;
            Sonidos.Reproducir(llegados == 1 ? Sonido.Victoria : Sonido.Logro);
            Sonidos.Reproducir(Sonido.Aplausos, null, llegados == 1 ? 1f : 0.7f);
            Efectos.Impacto(b.Cuerpo.worldCenterOfMass + Vector3.up, color, 1f);
            if (b.TryGetComponent(out AnimadorBicho animador)) animador.Celebrar(celebracionMeta + 1f);
            Estadisticas.Evento("meta", b, $"{llegados}º");
            porRetirar.Add((b, Time.time + celebracionMeta));
            ApuntarTiempo(b);

            if (llegados == 1)
            {
                Mensaje($"¡{NombreJugador(b)} LLEGA A LA CUMBRE!", color, 3.5f);
                Efectos.CamaraLenta(0.3f, 0.8f);
                Efectos.Sacudir(0.3f);
                if (confeti != null) foreach (var c in confeti) if (c != null) c.Play();
                if (savia != null) savia.Acelerar(saviaDesbordada);
                if (TiempoRestante > tiempoTrasPrimero) TiempoRestante = tiempoTrasPrimero;
                Mensaje($"¡La savia se desborda! Quedan {Mathf.CeilToInt(TiempoRestante)} segundos", new Color(1f, 0.7f, 0.25f), 3f);
                Sonidos.Reproducir(Sonido.Alarma, null, 0.6f);
            }
            else Mensaje($"¡{NombreJugador(b)} llega {llegados}º!", color, 2.5f);
        }

        // ---------------------------------------------------------------------
        // Eventos de show
        // ---------------------------------------------------------------------

        void FiebreDelPolen()
        {
            fiebreHecha = true;
            if (bolas != null) bolas.Fiebre(duracionFiebre);
            Mensaje("¡FIEBRE DEL POLEN! ¡El doble de bolas!", EstiloShow.Oro, 3f);
            Sonidos.Reproducir(Sonido.Alarma);
            Sonidos.Reproducir(Sonido.Ooh, null, 0.7f);
        }

        void ParpadeoDorado()
        {
            bool fiebre = bolas != null && bolas.EnFiebre;
            if (!fiebre)
            {
                sol.color = Color.Lerp(sol.color, colorSol, Time.deltaTime * 3f);
                sol.intensity = Mathf.Lerp(sol.intensity, intensidadSol, Time.deltaTime * 3f);
                return;
            }
            float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * 9f);
            sol.color = Color.Lerp(colorSol, new Color(1f, 0.75f, 0.2f), 0.6f + 0.4f * pulso);
            sol.intensity = intensidadSol * (0.85f + 0.4f * pulso);
        }

        /// <summary>Si alguien va muy destacado, el abejorro con cámara se le pone delante.</summary>
        void VigilarLider()
        {
            if (dron == null) return;
            ControladorBicho primero = null;
            float mejor = float.MinValue, segunda = float.MinValue;
            foreach (var b in ControladorBicho.Todos)
            {
                if (b.Retirado || HaLlegado(b)) continue;
                float y = b.transform.position.y;
                if (y > mejor) { segunda = mejor; mejor = y; primero = b; }
                else if (y > segunda) segunda = y;
            }
            bool destacado = primero != null && segunda > float.MinValue && mejor - segunda >= ventajaParaDron;
            dron.Grabar(destacado ? primero : null);
            if (destacado && !avisoDron)
            {
                avisoDron = true;
                Mensaje($"¡{NombreJugador(primero)} va destacado! ¡Se escapa hacia la cumbre!", gestor != null ? gestor.ColorDe(primero.IndiceJugador) : Color.white, 3f);
                Sonidos.Reproducir(Sonido.Ooh, null, 0.8f);
            }
            if (!destacado) avisoDron = false;
        }
    }
}
