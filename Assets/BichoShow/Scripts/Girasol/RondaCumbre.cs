using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Gran final "La Cumbre del Girasol" (todos contra todos): subida en espiral alrededor del
    /// tallo de un girasol gigante, sin puntos de control. Gana quien coge la Corona Dorada en lo
    /// alto. La savia inunda el invernadero desde abajo: quien queda por debajo, se pega y queda
    /// eliminado. Los demás puntúan por lo alto que hayan llegado.
    ///
    /// Eventos de show: al minuto, la fiebre del polen (el doble de bolas, luces doradas y sirena);
    /// y si alguien va muy destacado, el abejorro con cámara se le pone delante.
    /// </summary>
    public class RondaCumbre : RondaRuta
    {
        [Header("La Cumbre")]
        public Savia savia;
        public CoronaCumbre corona;
        public BolasPolen bolas;
        public AbejorroDron dron;
        [Tooltip("Luz principal (parpadea dorada en la fiebre del polen).")]
        public Light sol;
        [Tooltip("Cañones de confeti de las gradas (se disparan al coronar).")]
        public ParticleSystem[] confeti;
        [Tooltip("Altura de la cima (para el porcentaje de subida).")]
        public float alturaCima = 49f;
        [Tooltip("La final dura al menos esto, aunque el show pida rondas más cortas (s).")]
        public float duracionMinima = 150f;
        public float momentoFiebre = 60f;
        public float duracionFiebre = 20f;
        [Tooltip("Ventaja de altura (m) sobre el segundo para que el dron se plante delante del primero.")]
        public float ventajaParaDron = 7f;

        public static RondaCumbre Instancia { get; private set; }

        readonly float[] altura = new float[4];
        readonly bool[] pegado = new bool[4];
        readonly List<ControladorBicho> corredores = new List<ControladorBicho>();
        int ganador = -1;
        bool fiebreHecha, avisoDron, avisoSavia;
        Color colorSol;
        float intensidadSol;
        float siguienteLider;

        public override string Titulo => "LA CUMBRE DEL GIRASOL";
        protected override string Descripcion => "¡La gran final! Sube por la espiral del girasol antes de que te alcance la savia y coge la Corona Dorada.";
        protected override string ConsejoInicio => "¡Arriba! La savia sube: no te quedes atrás. Coge la corona con el botón de agarrar";
        protected override string AccionX => "placaje (¡empuja a los rivales!)";
        public override bool Individual => true;
        public override string NombreEquipo(int equipo) => equipo == 0 ? "Rojo" : "Azul";
        public override Color ColorEquipo(int equipo) => equipo == 0 ? new Color(1f, 0.38f, 0.35f) : new Color(0.3f, 0.6f, 1f);
        public override int PuntosEquipo(int equipo) => PuntosJugador(equipo) + PuntosJugador(equipo + 2);

        /// <summary>Coronar: 300. Si no, lo alto que se ha llegado (siempre menos).</summary>
        public override int PuntosJugador(int indice)
        {
            if (indice < 0 || indice >= 4) return 0;
            if (indice == ganador) return 300;
            return Mathf.FloorToInt(Mathf.Clamp01(altura[indice] / Mathf.Max(1f, alturaCima)) * 250f);
        }

        protected override string EtiquetaJugador(int indice)
        {
            if (indice < 0 || indice >= 4) return null;
            if (indice == ganador) return "¡CORONADO!";
            if (pegado[indice]) return "pegado en la savia";
            return $"{Mathf.FloorToInt(altura[indice])} m de altura";
        }

        public override bool HaLlegado(ControladorBicho b) =>
            b != null && b.IndiceJugador >= 0 && b.IndiceJugador < 4 && (b.IndiceJugador == ganador || pegado[b.IndiceJugador]);

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
            ganador = -1;
            fiebreHecha = avisoDron = avisoSavia = false;
            for (int i = 0; i < 4; i++) { altura[i] = 0f; pegado[i] = false; }
            if (corona != null)
            {
                corona.AlCoger -= Coronar;
                corona.AlCoger += Coronar;
                corona.Reiniciar();
            }
            if (savia != null) savia.Parar();
            if (bolas != null) bolas.Parar();
            if (dron != null) dron.Grabar(null);
            // Sin puntos de control: quien se cae al suelo del invernadero vuelve a la salida.
            if (gestor != null)
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
                if (i < 0 || i >= 4 || b.Retirado) continue;
                altura[i] = Mathf.Max(altura[i], b.transform.position.y);
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

            // ¿Queda alguien subiendo?
            corredores.RemoveAll(b => b == null);
            bool alguien = false;
            foreach (var b in corredores) if (!b.Retirado && !HaLlegado(b)) { alguien = true; break; }
            if (!alguien && corredores.Count > 0) TerminarRonda();
        }

        protected override void AlTerminarRonda()
        {
            if (savia != null) savia.Parar();
            if (bolas != null) bolas.Parar();
            if (sol != null) { sol.color = colorSol; sol.intensity = intensidadSol; }
            // Vuelven los pegados en la savia (para ver el final todos juntos).
            foreach (var b in corredores)
                if (b != null && b.Retirado && b.IndiceJugador >= 0 && b.IndiceJugador < 4 && pegado[b.IndiceJugador])
                {
                    if (corona != null) b.FijarPuntoReaparicion(corona.transform.position + Vector3.down * 2f, Quaternion.identity);
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

        void Coronar(ControladorBicho b)
        {
            if (EstadoActual != Estado.Jugando || ganador >= 0) return;
            ganador = b.IndiceJugador;
            altura[ganador] = alturaCima;
            Color color = gestor != null ? gestor.ColorDe(ganador) : Color.white;
            Mensaje($"¡{NombreJugador(b)} SE CORONA EN LA CUMBRE!", color, 4f);
            Sonidos.Reproducir(Sonido.Victoria);
            Sonidos.Reproducir(Sonido.Aplausos);
            Efectos.CamaraLenta(0.3f, 0.8f);
            Efectos.Sacudir(0.3f);
            if (corona != null)
            {
                Efectos.Impacto(corona.transform.position, EstiloShow.Oro, 1f);
                corona.gameObject.SetActive(false);
            }
            if (confeti != null) foreach (var c in confeti) if (c != null) c.Play();
            if (b.TryGetComponent(out AnimadorBicho a)) a.Celebrar(5f);
            Estadisticas.Evento("corona", b);
            TerminarRonda();
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
                if (b.Retirado) continue;
                float y = b.transform.position.y;
                if (y > mejor) { segunda = mejor; mejor = y; primero = b; }
                else if (y > segunda) segunda = y;
            }
            bool destacado = primero != null && segunda > float.MinValue && mejor - segunda >= ventajaParaDron;
            dron.Grabar(destacado ? primero : null);
            if (destacado && !avisoDron)
            {
                avisoDron = true;
                Mensaje($"¡{NombreJugador(primero)} va destacado! ¡Está a punto de coronar!", gestor != null ? gestor.ColorDe(primero.IndiceJugador) : Color.white, 3f);
                Sonidos.Reproducir(Sonido.Ooh, null, 0.8f);
            }
            if (!destacado) avisoDron = false;
        }
    }
}
