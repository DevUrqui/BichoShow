using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Prueba "Patata caliente" (todos contra todos):
    ///  - A alguien le toca la bomba (flota sobre su cabeza). Para pasarla, toca a otro bicho.
    ///    Quien te la acaba de dar no puede recibirla de vuelta enseguida (nada de ping-pong).
    ///  - Quien la lleva corre un poco más. La mecha dura un tiempo al azar que nadie sabe.
    ///  - ¡BOOM!: quien la lleva sale volando y los demás ganan 1 punto por sobrevivir.
    ///    Poco después aparece otra bomba. Al sonar la bocina, la que haya explota en el acto.
    /// Nadie queda eliminado: si te caes, reapareces.
    /// </summary>
    public class RondaPatata : RondaBase
    {
        [Header("Patata caliente")]
        public Bomba bomba;
        [Tooltip("Centro y radio de la arena (para los bots, que no se caigan).")]
        public Transform centroArena;
        public float radioArena = 10f;

        [Tooltip("Lo que dura la mecha (s), al azar entre estos dos valores.")]
        public Vector2 duracionMecha = new Vector2(10f, 18f);
        [Tooltip("Espera entre una explosión y la siguiente bomba (s).")]
        public float esperaNuevaBomba = 2.5f;
        [Tooltip("Distancia entre centros para que dos bichos se 'toquen' y pase la bomba.")]
        public float distanciaPase = 1.15f;
        [Tooltip("Segundos que quien da la bomba no puede recibirla de vuelta.")]
        public float inmunidadTrasPasar = 1.2f;
        [Tooltip("Cuánto más rápido corre quien lleva la bomba.")]
        public float velocidadConBomba = 1.15f;
        [Tooltip("Fuerza del bombazo (m/s hacia arriba y hacia los lados).")]
        public Vector2 fuerzaExplosion = new Vector2(11f, 7f);

        readonly int[] puntos = new int[4];
        readonly Dictionary<ControladorBicho, float> inmuneHasta = new Dictionary<ControladorBicho, float>();
        float siguienteBombaEn = -1f;

        public static RondaPatata Instancia { get; private set; }

        public override string Titulo => "PATATA CALIENTE";
        protected override string Descripcion => "Tócale a otro para pasarle la bomba antes de que explote. ¡Sobrevive a todas las que puedas!";
        protected override string ConsejoInicio => "¿Llevas la bomba? ¡Toca a alguien para pasársela!";
        protected override string AccionX => "lanzarse (¡también pasa la bomba!)";
        public override bool Individual => true;
        public override int PuntosJugador(int indice) => indice >= 0 && indice < puntos.Length ? puntos[indice] : 0;
        public override string NombreEquipo(int equipo) => equipo == 0 ? "Rojo" : "Azul";
        public override Color ColorEquipo(int equipo) => equipo == 0 ? new Color(1f, 0.38f, 0.35f) : new Color(0.3f, 0.6f, 1f);
        public override int PuntosEquipo(int equipo) => puntos[equipo] + puntos[equipo + 2];

        public ControladorBicho Portador => bomba != null && bomba.Encendida ? bomba.Portador : null;
        public Vector3 Centro => centroArena != null ? centroArena.position : transform.position;

        public bool Inmune(ControladorBicho b) => inmuneHasta.TryGetValue(b, out float hasta) && Time.time < hasta;

        protected override string EtiquetaJugador(int indice)
        {
            var p = Portador;
            return p != null && p.IndiceJugador == indice ? "¡LLEVA LA BOMBA!" : null;
        }

        public override IEnumerable<Vector3> ObjetivosSabotaje()
        {
            var p = Portador;
            if (p != null) yield return p.transform.position; // las trampas, donde está el lío
        }

        void Awake() => Instancia = this;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instancia == this) Instancia = null;
            QuitarVelocidades();
        }

        // ---------------------------------------------------------------------
        // Ciclo de la ronda
        // ---------------------------------------------------------------------

        protected override void CrearBot(int indice)
        {
            var bot = gestor.CrearBot<BotPatata>(indice);
            if (bot != null) bot.Configurar(this, habilidadBots);
        }

        protected override IEntradaBicho PonerCerebroBot(GameObject bicho)
        {
            var bot = bicho.AddComponent<BotPatata>();
            bot.Configurar(this, habilidadBots);
            return bot;
        }

        protected override void AlPrepararRonda()
        {
            for (int i = 0; i < puntos.Length; i++) puntos[i] = 0;
            inmuneHasta.Clear();
            if (bomba != null) bomba.Apagar();
            siguienteBombaEn = -1f;
            QuitarVelocidades();
        }

        protected override void AlEmpezarRonda()
        {
            siguienteBombaEn = Time.time + 1.5f;
            Mensaje("¡CUIDADO CON LA BOMBA!", Color.white, 1.5f);
        }

        protected override void ActualizarJugando()
        {
            if (bomba == null) return;

            if (!bomba.Encendida)
            {
                if (siguienteBombaEn >= 0f && Time.time >= siguienteBombaEn) NuevaBomba();
                return;
            }

            var portador = bomba.Portador;
            if (portador == null || portador.Retirado || !portador.isActiveAndEnabled)
            {
                Entregar(Elegir(null), null);
                return;
            }

            // ¿Toca a alguien? (centro con centro: tocarse, placarse o empujarse vale)
            Vector3 centro = portador.Cuerpo.worldCenterOfMass;
            ControladorBicho tocado = null;
            float mejor = distanciaPase * distanciaPase;
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == portador || b.Retirado || Inmune(b)) continue;
                float d = (b.Cuerpo.worldCenterOfMass - centro).sqrMagnitude;
                if (d < mejor) { mejor = d; tocado = b; }
            }
            if (tocado != null) Entregar(tocado, portador);

            if (bomba.Restante <= 0f) Explotar();
        }

        /// <summary>Al acabar el tiempo, la bomba que quede explota en el acto.</summary>
        protected override bool PuedeTerminar()
        {
            if (bomba != null && bomba.Encendida) Explotar();
            return true;
        }

        protected override void AlTerminarRonda()
        {
            siguienteBombaEn = -1f;
            if (bomba != null) bomba.Apagar();
            QuitarVelocidades();
        }

        // ---------------------------------------------------------------------
        // La bomba
        // ---------------------------------------------------------------------

        List<ControladorBicho> Activos()
        {
            var lista = new List<ControladorBicho>();
            foreach (var b in ControladorBicho.Todos)
                if (!b.Retirado) lista.Add(b);
            return lista;
        }

        ControladorBicho Elegir(ControladorBicho excepto)
        {
            var lista = Activos();
            if (lista.Count > 1 && excepto != null) lista.Remove(excepto);
            return lista.Count > 0 ? lista[Random.Range(0, lista.Count)] : null;
        }

        void NuevaBomba()
        {
            siguienteBombaEn = -1f;
            var quien = Elegir(null);
            if (quien == null) return;
            bomba.Encender(quien, Random.Range(duracionMecha.x, duracionMecha.y));
            Entregar(quien, null);
            Sonidos.Reproducir(Sonido.Alarma, quien.transform.position);
            Mensaje($"¡{NombreJugador(quien)} tiene la bomba!", new Color(1f, 0.6f, 0.2f), 2f);
        }

        void Entregar(ControladorBicho nuevo, ControladorBicho anterior)
        {
            if (nuevo == null) return;
            if (anterior != null)
            {
                anterior.MultiplicadorVelocidad = 1f;
                inmuneHasta[anterior] = Time.time + inmunidadTrasPasar;
                Sonidos.Reproducir(Sonido.Agarrar, nuevo.transform.position, 1f, 0.8f);
                Sonidos.Reproducir(Sonido.Muelle, nuevo.transform.position, 0.5f, 1.4f);
                Efectos.Impacto(nuevo.Cuerpo.worldCenterOfMass + Vector3.up * 0.8f, new Color(1f, 0.6f, 0.2f), 0.3f);
                Estadisticas.Evento("pase", nuevo, $"de {NombreJugador(anterior)}");
            }
            bomba.Pasar(nuevo);
            nuevo.MultiplicadorVelocidad = velocidadConBomba;
        }

        void Explotar()
        {
            var victima = bomba.Portador;
            Vector3 punto = bomba.transform.position;
            bomba.Apagar();
            siguienteBombaEn = Time.time + esperaNuevaBomba;

            Sonidos.Reproducir(Sonido.Rotura, punto, 1f, 0.6f);
            Sonidos.Reproducir(Sonido.Golpe, punto, 1f, 0.5f);
            Sonidos.Reproducir(Sonido.Ooh, punto);
            Efectos.Impacto(punto, new Color(1f, 0.55f, 0.15f), 1f);
            Efectos.Impacto(punto, new Color(1f, 0.85f, 0.3f), 1f);
            Efectos.Polvo(punto - Vector3.up * 1.2f, 1f);
            Efectos.Sacudir(0.6f);
            Efectos.CamaraLenta(0.3f, 0.5f, 2f);

            // La onda: a la víctima la lanza por los aires; a los de cerca, un empujón.
            foreach (var b in ControladorBicho.Todos)
            {
                if (b.Retirado) continue;
                Vector3 lejos = b.Cuerpo.worldCenterOfMass - punto;
                lejos.y = 0f;
                float d = lejos.magnitude;
                Vector3 dir = d > 0.01f ? lejos / d : Random.insideUnitSphere.WithY(0f).normalized;
                if (b == victima)
                {
                    b.Tropezar(1.6f);
                    b.Cuerpo.AddForce(Vector3.up * fuerzaExplosion.x + Random.insideUnitSphere.WithY(0f).normalized * fuerzaExplosion.y, ForceMode.VelocityChange);
                    b.Cuerpo.AddTorque(Random.onUnitSphere * 10f, ForceMode.VelocityChange);
                }
                else if (d < 3.5f)
                {
                    float k = 1f - d / 3.5f;
                    b.Tropezar(0.6f * k + 0.2f);
                    b.Cuerpo.AddForce(dir * 8f * k + Vector3.up * 4f * k, ForceMode.VelocityChange);
                }
            }

            if (victima != null)
            {
                victima.MultiplicadorVelocidad = 1f;
                foreach (var b in ControladorBicho.Todos)
                {
                    if (b.Retirado || b == victima) continue;
                    puntos[b.IndiceJugador]++;
                }
                Mensaje($"¡BOOM! {NombreJugador(victima)} ha explotado  ·  +1 para los demás", new Color(1f, 0.55f, 0.2f), 3f);
                Estadisticas.Evento("explosion", victima);
            }
        }

        void QuitarVelocidades()
        {
            foreach (var b in ControladorBicho.Todos) b.MultiplicadorVelocidad = 1f;
        }
    }

    static class ExtensionesVector
    {
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
    }
}
