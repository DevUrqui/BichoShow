using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Prueba "El Cortacésped Mecha" (supervivencia, todos contra todos): un robot cortacésped
    /// gigante en el centro de un jardín de noche va lanzando ataques (CortacespedMecha). Cada
    /// ataque que aguantas sin caerte (al foso de chatarra que lo rodea o fuera del jardín), +1.
    /// Quien se cae vuelve a su sitio, pero se queda sin el punto de ese ataque.
    /// </summary>
    public class RondaCortacesped : RondaBase
    {
        [Header("El Cortacésped Mecha")]
        public CortacespedMecha robot;

        readonly int[] puntos = new int[4];
        readonly HashSet<ControladorBicho> caidos = new HashSet<ControladorBicho>();
        readonly HashSet<ControladorBicho> abajo = new HashSet<ControladorBicho>();

        public static RondaCortacesped Instancia { get; private set; }

        public override string Titulo => "EL CORTACÉSPED MECHA";
        protected override string Descripcion => "¡Aguanta los ataques del robot! Salta su láser, corre contra su aspiradora y esquiva las tuercas. Cada ataque sin caerte, +1.";
        protected override string ConsejoInicio => "¡Mira su pantalla: te dice qué ataque viene!";
        protected override string AccionX => "placaje (¡échalos al foso!)";
        public override bool Individual => true;
        public override int PuntosJugador(int indice) => indice >= 0 && indice < puntos.Length ? puntos[indice] : 0;
        public override string NombreEquipo(int equipo) => equipo == 0 ? "Rojo" : "Azul";
        public override Color ColorEquipo(int equipo) => equipo == 0 ? new Color(1f, 0.38f, 0.35f) : new Color(0.3f, 0.6f, 1f);
        public override int PuntosEquipo(int equipo) => puntos[equipo] + puntos[equipo + 2];

        void Awake()
        {
            Instancia = this;
            if (robot != null)
            {
                robot.AlAnunciar += AlAnunciar;
                robot.AlTerminar += AlTerminarAtaque;
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
        }

        protected override void AlEmpezarRonda()
        {
            if (robot != null)
            {
                robot.activo = true;
                robot.PonerCara(CortacespedMecha.Cara.Enfadado);
            }
            Mensaje("¡QUE EMPIECE EL CORTE!", new Color(1f, 0.35f, 0.75f), 1.8f);
        }

        protected override void ActualizarJugando()
        {
            if (robot == null) return;
            robot.progreso = Mathf.Clamp01(TiempoJugado / Mathf.Max(1f, duracionRonda));

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

        void AlAnunciar(CortacespedMecha.Ataque ataque)
        {
            if (EstadoActual != Estado.Jugando) return;
            caidos.Clear();
            switch (ataque)
            {
                case CortacespedMecha.Ataque.OndaLaser:
                    Mensaje("¡ONDA LÁSER! ¡A SALTAR!", new Color(1f, 0.35f, 0.8f), 2f);
                    break;
                case CortacespedMecha.Ataque.Aspiracion:
                    Mensaje("¡ASPIRACIÓN! ¡CORRED HACIA FUERA!", new Color(0.45f, 0.85f, 1f), 2f);
                    break;
                default:
                    Mensaje("¡LLUVIA DE TUERCAS!", new Color(1f, 0.75f, 0.3f), 2f);
                    break;
            }
        }

        void AlTerminarAtaque(CortacespedMecha.Ataque ataque)
        {
            if (EstadoActual != Estado.Jugando) return;
            int salvados = 0;
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado || caidos.Contains(b) || b.IndiceJugador < 0 || b.IndiceJugador >= puntos.Length) continue;
                puntos[b.IndiceJugador]++;
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
