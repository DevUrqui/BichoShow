using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// La gran final, "Juntos o nada": cada pareja tiene su propio circuito con pruebas que solo
    /// se superan cooperando (bloque, puente levadizo, puerta doble, pasillos cruzados, camino de
    /// baldosas, balancín y ascensor). Puntos por cada
    /// prueba superada por los dos; bonus a quien llegue primero a la meta.
    /// Si te caes, vuelves al inicio de la prueba en la que está tu pareja.
    /// </summary>
    public class RondaFinal : RondaBase
    {
        [System.Serializable]
        public class Equipo
        {
            public string nombre = "Rojo";
            public Color color = Color.red;
            [Tooltip("Puntos de control: [0] la salida; [i] el inicio de la prueba i (tras superar la anterior).")]
            public Transform[] puntosControl;
            [Tooltip("Las pruebas del circuito, en orden.")]
            public PasoCooperativo[] pasos;

            [HideInInspector] public int puntos;
            [HideInInspector] public int progreso;
            [HideInInspector] public bool terminado;
        }

        public static RondaFinal Instancia { get; private set; }

        [Header("Final")]
        public Equipo[] equipos = new Equipo[2];
        public int puntosPorPaso = 100;
        public int bonusPrimero = 300;
        public int bonusSegundo = 150;
        [Tooltip("La final dura al menos esto, aunque el show pida rondas más cortas (s).")]
        public float duracionMinima = 240f;
        public ParticleSystem confeti;

        int llegados;
        readonly List<ControladorBicho> miembros = new List<ControladorBicho>();

        public override string Titulo => "JUNTOS O NADA";
        protected override string Descripcion => "Cada pareja tiene su circuito y solo se supera cooperando. ¡La primera pareja en la meta gana!";
        protected override string ConsejoInicio => "¡Juntos! Mirad la pista de vuestra pareja arriba: empujar, pisar placas, subir…";
        public override string NombreEquipo(int equipo) => equipos[equipo].nombre;
        public override Color ColorEquipo(int equipo) => equipos[equipo].color;
        public override int PuntosEquipo(int equipo) => equipos[equipo].puntos;

        void Awake() => Instancia = this;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instancia == this) Instancia = null;
        }

        // ---------------------------------------------------------------------
        // Parejas
        // ---------------------------------------------------------------------

        public int MiembrosDe(int equipo) => JugadoresEn(equipo);

        /// <summary>Los de un equipo, ordenados por número de jugador (el primero es el rol 0).</summary>
        public List<ControladorBicho> Miembros(int equipo, List<ControladorBicho> destino = null)
        {
            destino ??= new List<ControladorBicho>();
            destino.Clear();
            foreach (var b in ControladorBicho.Todos)
                if (EquipoDe(b) == equipo) destino.Add(b);
            destino.Sort((x, y) => x.IndiceJugador.CompareTo(y.IndiceJugador));
            return destino;
        }

        public int Rol(ControladorBicho bicho)
        {
            Miembros(EquipoDe(bicho), miembros);
            return Mathf.Max(0, miembros.IndexOf(bicho));
        }

        public ControladorBicho Companero(ControladorBicho bicho)
        {
            Miembros(EquipoDe(bicho), miembros);
            foreach (var m in miembros) if (m != bicho) return m;
            return null;
        }

        public PasoCooperativo PasoActual(int equipo)
        {
            var e = equipos[equipo];
            return e.pasos != null && e.progreso < e.pasos.Length ? e.pasos[e.progreso] : null;
        }

        public bool Terminado(int equipo) => equipos[equipo].terminado;

        public override IEnumerable<Vector3> ObjetivosSabotaje()
        {
            foreach (var b in ControladorBicho.Todos)
                if (!b.EsBot) yield return b.transform.position;
        }

        // ---------------------------------------------------------------------
        // Ciclo de la ronda
        // ---------------------------------------------------------------------

        protected override void AlPrepararRonda()
        {
            llegados = 0;
            foreach (var e in equipos)
            {
                e.puntos = 0;
                e.progreso = 0;
                e.terminado = false;
                if (e.pasos != null) foreach (var p in e.pasos) if (p != null) p.Reiniciar();
            }

            // Que nadie reaparezca en un punto de control de la ronda anterior.
            if (gestor != null)
                foreach (var b in ControladorBicho.Todos)
                {
                    var punto = gestor.PuntoAparicion(b.IndiceJugador);
                    if (punto != null) b.FijarPuntoReaparicion(punto.position, punto.rotation);
                }
        }

        protected override void CrearBot(int indice)
        {
            var bot = gestor.CrearBot<BotFinal>(indice);
            if (bot != null) bot.Configurar(this, habilidadBots);
        }

        protected override IEntradaBicho PonerCerebroBot(GameObject bicho)
        {
            var bot = bicho.AddComponent<BotFinal>();
            bot.Configurar(this, habilidadBots);
            return bot;
        }

        protected override void AlEmpezarRonda()
        {
            TiempoRestante = Mathf.Max(TiempoRestante, duracionMinima);
            for (int i = 0; i < equipos.Length; i++)
            {
                if (equipos[i].pasos == null) continue;
                foreach (var p in equipos[i].pasos)
                    if (p is PasoBloque bloque) bloque.AjustarMasa(MiembrosDe(i));
            }
            Mensaje("¡JUNTOS A POR LA META!", Color.white, 2f);
        }

        protected override void ActualizarJugando()
        {
            int conJugadores = 0, enMeta = 0;
            for (int i = 0; i < equipos.Length; i++)
            {
                var e = equipos[i];
                Miembros(i, miembros);
                if (miembros.Count == 0) continue;
                conJugadores++;
                if (e.terminado) { enMeta++; continue; }

                var paso = PasoActual(i);
                if (paso == null || !paso.Completado(miembros)) continue;

                e.progreso++;
                e.puntos += puntosPorPaso;

                if (e.progreso >= e.pasos.Length)
                {
                    e.terminado = true;
                    enMeta++;
                    llegados++;
                    int bonus = llegados == 1 ? bonusPrimero : bonusSegundo;
                    e.puntos += bonus;
                    Mensaje($"¡EL EQUIPO {e.nombre.ToUpper()} LLEGA A LA META!  +{puntosPorPaso + bonus}", e.color, 4f);
                    Sonidos.Reproducir(Sonido.Victoria);
                    Estadisticas.Evento("meta", null, e.nombre);
                    Sonidos.Reproducir(Sonido.Aplausos);
                    Efectos.Sacudir(0.25f);
                    if (confeti != null && paso is PasoAscensor asc && asc.meta != null)
                    {
                        confeti.transform.position = asc.meta.transform.position + Vector3.up * 0.3f;
                        var main = confeti.main;
                        main.startColor = new ParticleSystem.MinMaxGradient(e.color, new Color(1f, 0.92f, 0.3f));
                        confeti.Play();
                    }
                }
                else
                {
                    Mensaje($"Equipo {e.nombre}: ¡superado!  +{puntosPorPaso}   Ahora: {PasoActual(i).pista}", e.color, 3f);
                    Sonidos.Reproducir(Sonido.Logro);
                    Estadisticas.Evento("prueba superada", null, $"{e.nombre} {e.progreso}/{e.pasos.Length}");
                    FijarPuntoDeControl(i);
                }
            }

            if (conJugadores > 0 && enMeta >= conJugadores) TerminarRonda();
        }

        /// <summary>Al superar una prueba, si alguien se cae vuelve al inicio de la siguiente.</summary>
        void FijarPuntoDeControl(int equipo)
        {
            var e = equipos[equipo];
            if (e.puntosControl == null || e.puntosControl.Length == 0) return;
            var punto = e.puntosControl[Mathf.Min(e.progreso, e.puntosControl.Length - 1)];
            if (punto == null) return;

            Miembros(equipo, miembros);
            Vector3 lateral = Vector3.Cross(Vector3.up, punto.forward).normalized;
            for (int k = 0; k < miembros.Count; k++)
            {
                float lado = miembros.Count == 1 ? 0f : (k == 0 ? -1.2f : 1.2f);
                miembros[k].FijarPuntoReaparicion(punto.position + lateral * lado, punto.rotation);
            }
        }

        // ---------------------------------------------------------------------
        // Pantalla
        // ---------------------------------------------------------------------

        protected override void DibujarInfoEquipo(int equipo, float x, float ancho, float h)
        {
            var e = equipos[equipo];
            int total = e.pasos != null ? e.pasos.Length : 0;
            Barra(new Rect(x, h * 0.095f, ancho, h * 0.025f), total > 0 ? e.progreso / (float)total : 0f, e.color);
            string texto = e.terminado ? "¡EN LA META!" : $"Prueba {e.progreso + 1}/{total}: {PasoActual(equipo)?.pista}";
            Texto(new Rect(x, h * 0.125f, ancho, h * 0.04f), texto, estiloPequeno, Color.white);
        }
    }
}
