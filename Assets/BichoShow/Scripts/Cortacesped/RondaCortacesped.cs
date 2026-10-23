using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Prueba "El Cortacésped Mecha" (supervivencia, todos contra todos): un robot cortacésped
    /// gigante en el centro de un jardín de noche va lanzando ataques (CortacespedMecha). La
    /// pelea va por fases: calentamiento, cuchillas (ataques nuevos y variantes) y modo furia
    /// (combos de dos ataques), con un último asalto de hasta tres a la vez. Cada tanda de ataques
    /// que aguantas sin caerte (al foso de chatarra o fuera del jardín), +1; en el modo furia, +2;
    /// en el último asalto, +3. Quien se cae vuelve a su sitio, sin el punto.
    /// </summary>
    public class RondaCortacesped : RondaBase
    {
        [Header("El Cortacésped Mecha")]
        public CortacespedMecha robot;
        [Tooltip("Lo que va de ronda (0-1) en que empiezan la fase 2 y la 3.")]
        public Vector2 cambiosDeFase = new Vector2(0.22f, 0.55f);

        readonly int[] puntos = new int[4];
        readonly HashSet<ControladorBicho> caidos = new HashSet<ControladorBicho>();
        readonly HashSet<ControladorBicho> abajo = new HashSet<ControladorBicho>();
        readonly Dictionary<ControladorBicho, Vector3> reaparicionPuesta = new Dictionary<ControladorBicho, Vector3>();
        int fase = 1;
        bool ultimoAsalto;

        public static RondaCortacesped Instancia { get; private set; }

        public override string Titulo => "EL CORTACÉSPED MECHA";
        protected override string Descripcion => "¡Aguanta los ataques del robot! Salta su láser y sus cuchillas, corre contra su aspiradora, esquiva las tuercas y no te quedes en el césped que corta. Cada ataque sin caerte, puntos.";
        protected override string ConsejoInicio => "¡Mira su pantalla: te dice qué ataque viene!";
        protected override string AccionX => "placaje (¡échalos al foso!)";
        public override bool Individual => true;
        public override int PuntosJugador(int indice) => indice >= 0 && indice < puntos.Length ? puntos[indice] : 0;
        public override string NombreEquipo(int equipo) => equipo == 0 ? "Rojo" : "Azul";
        public override Color ColorEquipo(int equipo) => equipo == 0 ? new Color(1f, 0.38f, 0.35f) : new Color(0.3f, 0.6f, 1f);
        public override int PuntosEquipo(int equipo) => puntos[equipo] + puntos[equipo + 2];
        protected override string EtiquetaJugador(int indice) => ultimoAsalto ? "ÚLTIMO ASALTO · x3" : fase >= 3 ? "MODO FURIA · x2" : $"FASE {fase}";

        void Awake()
        {
            Instancia = this;
            if (robot != null)
            {
                robot.AlAnunciar += AlAnunciar;
                robot.AlTerminar += AlTerminarAtaque;
                robot.AlAviso += (texto, color) => { if (EstadoActual == Estado.Jugando) Mensaje(texto, color, 2f); };
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instancia == this) Instancia = null;
        }

        protected override void CrearBot(int indice)
        {
            var bot = gestor.CrearBot<BotCortacesped>(indice);
            if (bot != null) bot.Configurar(this, habilidadBots);
        }

        protected override IEntradaBicho PonerCerebroBot(GameObject bicho)
        {
            var bot = bicho.AddComponent<BotCortacesped>();
            bot.Configurar(this, habilidadBots);
            return bot;
        }

        protected override void AlPrepararRonda()
        {
            for (int i = 0; i < puntos.Length; i++) puntos[i] = 0;
            caidos.Clear();
            abajo.Clear();
            reaparicionPuesta.Clear();
            fase = 1;
            ultimoAsalto = false;
            if (robot != null) robot.fase = 1;
        }

        protected override void AlEmpezarRonda()
        {
            if (robot != null)
            {
                robot.activo = true;
                robot.fase = 1;
                robot.PonerCara(CortacespedMecha.Cara.Enfadado);
            }
            Mensaje("FASE 1: ¡QUE EMPIECE EL CORTE!", new Color(1f, 0.35f, 0.75f), 2f);
        }

        protected override void ActualizarJugando()
        {
            if (robot == null) return;
            float progreso = Mathf.Clamp01(TiempoJugado / Mathf.Max(1f, duracionRonda));
            robot.progreso = progreso;

            // Cambios de fase.
            int nueva = progreso >= cambiosDeFase.y ? 3 : progreso >= cambiosDeFase.x ? 2 : 1;
            if (nueva != fase)
            {
                fase = nueva;
                robot.fase = fase;
                Efectos.Sacudir(0.4f);
                Sonidos.Reproducir(Sonido.Bocina, robot.Centro, 1f, fase == 3 ? 0.8f : 1f);
                if (fase == 2)
                    Mensaje("FASE 2: ¡SACA LAS CUCHILLAS!", new Color(1f, 0.55f, 0.3f), 3f);
                else
                {
                    Mensaje("FASE 3: ¡MODO FURIA! Ataques dobles · puntos x2", new Color(1f, 0.25f, 0.25f), 3.5f);
                    robot.PonerCara(CortacespedMecha.Cara.Furia);
                }
            }

            if (!ultimoAsalto && robot.UltimoAsalto)
            {
                ultimoAsalto = true;
                Efectos.Sacudir(0.5f);
                Sonidos.Reproducir(Sonido.Bocina, robot.Centro, 1f, 0.65f);
                Mensaje("¡ÚLTIMO ASALTO! Hasta tres ataques a la vez · puntos x3", new Color(1f, 0.15f, 0.2f), 3.5f);
            }

            AsegurarReapariciones();

            // ¿Alguien se ha caído? (al foso o fuera del jardín: está muy por debajo del césped)
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado) continue;
                bool estaAbajo = b.transform.position.y < robot.Centro.y - 1.5f;
                if (estaAbajo && abajo.Add(b))
                {
                    caidos.Add(b);
                    Vector3 rel = b.transform.position - robot.Centro;
                    rel.y = 0f;
                    bool foso = rel.magnitude < robot.radioFoso + 0.5f;
                    Color color = gestor != null ? gestor.ColorDe(b.IndiceJugador) : Color.white;
                    Mensaje(foso ? $"¡{NombreJugador(b)} al foso de chatarra!" : $"¡{NombreJugador(b)} se ha caído del jardín!", color, 2f);
                    Estadisticas.Evento("caida", b, foso ? "foso" : "borde");
                }
                else if (!estaAbajo) abajo.Remove(b);
            }
        }

        /// <summary>
        /// Si el sitio donde reaparece un bicho está en una porción del césped que se va a abrir
        /// (o abierta), se le cambia a la porción segura más cercana: si no, caería una y otra vez.
        /// Cuando pasa el peligro, vuelve a su sitio de siempre.
        /// </summary>
        void AsegurarReapariciones()
        {
            if (gestor == null) return;
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado) continue;
                var punto = gestor.PuntoAparicion(b.IndiceJugador);
                if (punto == null) continue;
                Vector3 sitio = SitioSeguro(punto.position);
                if (reaparicionPuesta.TryGetValue(b, out var antes) && (antes - sitio).sqrMagnitude < 0.01f) continue;
                reaparicionPuesta[b] = sitio;
                Vector3 mirar = robot.Centro - sitio;
                mirar.y = 0f;
                b.FijarPuntoReaparicion(sitio, mirar.sqrMagnitude > 0.01f ? Quaternion.LookRotation(mirar) : punto.rotation);
            }
        }

        Vector3 SitioSeguro(Vector3 sitio)
        {
            if (!robot.PorcionEnPeligro(sitio)) return sitio;
            Vector3 rel = sitio - robot.Centro;
            for (int k = 1; k <= 12; k++)
                foreach (float signo in new[] { 1f, -1f })
                {
                    Vector3 otro = robot.Centro + Quaternion.Euler(0f, signo * 15f * k, 0f) * rel;
                    if (!robot.PorcionEnPeligro(otro)) return otro;
                }
            return sitio;
        }

        static string Nombre(CortacespedMecha.Ataque a) => a switch
        {
            CortacespedMecha.Ataque.OndaLaser => "ONDA LÁSER",
            CortacespedMecha.Ataque.Aspiracion => "ASPIRACIÓN",
            CortacespedMecha.Ataque.LluviaTuercas => "LLUVIA DE TUERCAS",
            CortacespedMecha.Ataque.Barrido => "BARRIDO DE CUCHILLAS",
            _ => "CORTE DE CÉSPED",
        };

        static string Consejo(CortacespedMecha.Ataque a) => a switch
        {
            CortacespedMecha.Ataque.OndaLaser => "¡salta los rosas, no los azules!",
            CortacespedMecha.Ataque.Aspiracion => "¡corre hacia fuera!",
            CortacespedMecha.Ataque.LluviaTuercas => "¡esquívalas!",
            CortacespedMecha.Ataque.Barrido => "¡salta la cuchilla!",
            _ => "¡sal del césped que parpadea!",
        };

        void AlAnunciar(CortacespedMecha.Ataque a, CortacespedMecha.Ataque? b)
        {
            if (EstadoActual != Estado.Jugando) return;
            caidos.Clear();
            var c = robot.Tercero;
            if (b.HasValue && c.HasValue)
                Mensaje($"¡TRIPLE! {Nombre(a)} + {Nombre(b.Value)} + {Nombre(c.Value)}", new Color(1f, 0.2f, 0.25f), 3f);
            else if (b.HasValue)
                Mensaje($"¡COMBO! {Nombre(a)} + {Nombre(b.Value)}", new Color(1f, 0.3f, 0.3f), 2.5f);
            else
                Mensaje($"¡{Nombre(a)}! {Consejo(a)}", a == CortacespedMecha.Ataque.OndaLaser ? new Color(1f, 0.35f, 0.8f) : new Color(1f, 0.75f, 0.3f), 2.2f);
        }

        void AlTerminarAtaque()
        {
            if (EstadoActual != Estado.Jugando) return;
            int premio = ultimoAsalto ? 3 : fase >= 3 ? 2 : 1;
            int salvados = 0;
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado || caidos.Contains(b) || b.IndiceJugador < 0 || b.IndiceJugador >= puntos.Length) continue;
                puntos[b.IndiceJugador] += premio;
                salvados++;
            }
            caidos.Clear();
            if (salvados > 0) Sonidos.Reproducir(Sonido.Logro, robot.Centro, 0.5f, 1.3f);
        }

        protected override void AlTerminarRonda()
        {
            if (robot != null) robot.Parar();
        }
    }
}
