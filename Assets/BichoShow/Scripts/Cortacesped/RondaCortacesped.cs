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
    ///
    /// Villano: el saboteador de la ronda (en el show, el que va último) maneja el robot. Gana
    /// 1 punto por cada bicho que tumba y 3 por cada uno que se cae.
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
        protected override string Descripcion => "¡Aguanta los ataques del robot! Salta su láser y sus cuchillas, corre contra su aspiradora, esquiva su chatarra (y salta sus ruedas) y no te quedes en el césped que corta. Cada ataque sin caerte, puntos.";
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
                robot.AlDerribar += b => { if (EstadoActual == Estado.Jugando && robot.villano != null) robot.villano.SumarPuntos(1, "¡golpe!"); };
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
            if (robot != null) { robot.fase = 1; robot.villano = null; }
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
            if (robot != null && robot.villano != null)
                Mensaje($"¡{robot.villano.Etiqueta} maneja el robot!", robot.villano.ColorSaboteador, 3f);
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
                    if (robot.villano != null) robot.villano.SumarPuntos(3, foso ? "¡al foso!" : "¡fuera!");
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

        // ---------------------------------------------------------------------
        // El villano
        // ---------------------------------------------------------------------

        protected override bool AdmiteSaboteador => robot != null;

        protected override void AlCrearSaboteador(Saboteador s)
        {
            // El que sabotea maneja el robot: elige los ataques y usa sus poderes.
            s.Villano = true;
            var b = s.GetComponent<ControladorBicho>();
            s.Etiqueta = b != null ? $"Villano {NombreJugador(b)}" : "Villano";
            if (robot != null) robot.villano = s;
        }

        /// <summary>Las tres cartas del villano (el siguiente ataque), encima de su panel.</summary>
        protected override void OnGUI()
        {
            base.OnGUI();
            if (robot == null || !robot.Pilotado || EstadoActual != Estado.Jugando || robot.Opciones.Count == 0) return;
            if (ControladaPorShow && ShowBicho.Instancia != null && ShowBicho.Instancia.TapaLaPantalla) return;
            float w = Screen.width, h = Screen.height;
            Color color = robot.villano.ColorSaboteador;
            float ancho = EstiloShow.Px(360f), alto = EstiloShow.Px(78f), hueco = EstiloShow.Px(22f);
            float x0 = w * 0.5f - (ancho * 3f + hueco * 2f) * 0.5f;
            float y = h * 0.93f - EstiloShow.Px(140f);
            EstiloShow.Texto(new Rect(0, y - EstiloShow.Px(46f), w, EstiloShow.Px(40f)), "PRÓXIMO ATAQUE DEL VILLANO", Mathf.RoundToInt(EstiloShow.Px(28f)), color);
            for (int i = 0; i < robot.Opciones.Count; i++)
            {
                var o = robot.Opciones[i];
                bool elegida = i == robot.Elegida;
                var r = new Rect(x0 + i * (ancho + hueco), y, ancho, alto);
                if (elegida) r = EstiloShow.Escalar(r, 1.08f + Mathf.Sin(Time.time * 6f) * 0.02f);
                EstiloShow.Panel(r, elegida ? color : new Color(0.12f, 0.06f, 0.2f, 0.85f), r.height * 0.35f);
                string texto = Corto(o.a) + (o.b.HasValue ? $" + {Corto(o.b.Value)}" : "") + (o.c.HasValue ? $" + {Corto(o.c.Value)}" : "");
                int tam = Mathf.RoundToInt(EstiloShow.Px(o.c.HasValue ? 22f : o.b.HasValue ? 26f : 32f));
                EstiloShow.Texto(r, texto, tam, elegida ? Color.white : new Color(1f, 1f, 1f, 0.7f));
            }
        }

        static string Corto(CortacespedMecha.Ataque a) => a switch
        {
            CortacespedMecha.Ataque.OndaLaser => "LÁSER",
            CortacespedMecha.Ataque.Aspiracion => "ASPIRADORA",
            CortacespedMecha.Ataque.LluviaTuercas => "CHATARRA",
            CortacespedMecha.Ataque.Barrido => "CUCHILLAS",
            _ => "CORTE",
        };

        static string Nombre(CortacespedMecha.Ataque a) => a switch
        {
            CortacespedMecha.Ataque.OndaLaser => "ONDA LÁSER",
            CortacespedMecha.Ataque.Aspiracion => "ASPIRACIÓN",
            CortacespedMecha.Ataque.LluviaTuercas => "LLUVIA DE CHATARRA",
            CortacespedMecha.Ataque.Barrido => "BARRIDO DE CUCHILLAS",
            _ => "CORTE DE CÉSPED",
        };

        static string Consejo(CortacespedMecha.Ataque a) => a switch
        {
            CortacespedMecha.Ataque.OndaLaser => "¡salta los rosas, no los azules!",
            CortacespedMecha.Ataque.Aspiracion => "¡corre hacia fuera!",
            CortacespedMecha.Ataque.LluviaTuercas => "¡esquívala, salta las ruedas y sal de las dianas!",
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
