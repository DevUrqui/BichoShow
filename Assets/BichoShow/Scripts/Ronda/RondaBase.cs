using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Lo común a todas las pruebas del show: estados (Esperando → CuentaAtras → Jugando → Fin),
    /// START para empezar, reloj, rellenar con bots, mensajes y marcador en pantalla.
    ///
    /// Equipos: jugadores 1 y 3 son el equipo 0; jugadores 2 y 4, el equipo 1.
    /// Cada prueba hereda de aquí y rellena los huecos (qué pasa al empezar, al jugar, al acabar).
    /// </summary>
    public abstract class RondaBase : MonoBehaviour
    {
        public enum Estado { Esperando, CuentaAtras, Jugando, Fin }

        [Header("Ronda")]
        public float duracionRonda = 90f;
        public float duracionCuentaAtras = 3f;

        [Header("Bots")]
        [Tooltip("Al empezar, rellenar con bots los huecos hasta llegar a 'Jugadores Con Bots'.")]
        public bool rellenarConBots = true;
        [Range(2, 4)] public int jugadoresConBots = 4;
        [Range(0f, 1f)] public float habilidadBots = 0.55f;
        public GestorJugadores gestor;

        public Estado EstadoActual { get; private set; } = Estado.Esperando;
        public float TiempoRestante { get; protected set; }

        protected float finCuentaAtras;
        protected GUIStyle estiloGrande, estiloMedio, estiloPequeno;
        readonly List<(string texto, Color color, float hasta)> mensajes = new List<(string, Color, float)>();

        // ---------------------------------------------------------------------
        // Lo que cada prueba tiene que decir
        // ---------------------------------------------------------------------

        protected abstract string Titulo { get; }
        protected abstract string Descripcion { get; }
        protected virtual string Controles => "Stick mover · A saltar · Gatillo agarrar · X lanzarse / lanzar lo agarrado";
        public abstract string NombreEquipo(int equipo);
        public abstract Color ColorEquipo(int equipo);
        public abstract int PuntosEquipo(int equipo);

        /// <summary>Crea un bot en ese número de jugador (con el cerebro que toque en esta prueba).</summary>
        protected abstract void CrearBot(int indice);

        /// <summary>Al pulsar START: limpiar la ronda anterior y preparar roles (antes de crear bots).</summary>
        protected virtual void AlPrepararRonda() { }
        /// <summary>Después de crear los bots, antes de recolocar a todos.</summary>
        protected virtual void AlDespuesDeBots() { }
        /// <summary>Si es true, nadie puede unirse durante la ronda aunque no haya bots.</summary>
        protected virtual bool BloquearUniones => false;
        /// <summary>Al acabar la cuenta atrás.</summary>
        protected abstract void AlEmpezarRonda();
        /// <summary>Cada frame mientras se juega.</summary>
        protected virtual void ActualizarJugando() { }
        /// <summary>Con el reloj a cero: si devuelve false, la ronda sigue (p. ej. punto de oro).</summary>
        protected virtual bool PuedeTerminar() => true;
        protected virtual void AlTerminarRonda() { }

        // ---------------------------------------------------------------------
        // Bucle
        // ---------------------------------------------------------------------

        protected virtual void Start()
        {
            if (gestor == null) gestor = FindAnyObjectByType<GestorJugadores>();
        }

        protected virtual void Update()
        {
            switch (EstadoActual)
            {
                case Estado.Esperando:
                case Estado.Fin:
                    if (ControladorBicho.Todos.Count > 0 && AlguienPulsoEmpezar())
                        EmpezarCuentaAtras();
                    break;

                case Estado.CuentaAtras:
                    if (Time.time >= finCuentaAtras) EmpezarRonda();
                    break;

                case Estado.Jugando:
                    TiempoRestante = Mathf.Max(0f, TiempoRestante - Time.deltaTime);
                    ActualizarJugando();
                    if (EstadoActual == Estado.Jugando && TiempoRestante <= 0f && PuedeTerminar())
                        TerminarRonda();
                    break;
            }

            mensajes.RemoveAll(m => Time.time > m.hasta);
        }

        static bool AlguienPulsoEmpezar()
        {
            foreach (var entrada in PlayerInput.all)
            {
                var accion = entrada.actions != null ? entrada.actions.FindAction("Empezar") : null;
                if (accion != null && accion.WasPressedThisFrame()) return true;
            }
            return false;
        }

        protected void EmpezarCuentaAtras()
        {
            EstadoActual = Estado.CuentaAtras;
            finCuentaAtras = Time.time + duracionCuentaAtras;

            AlPrepararRonda();
            bool hayBots = RellenarConBots();
            AlDespuesDeBots();

            // Con bots (o roles especiales) en juego, nadie más puede unirse hasta el final:
            // le tocaría el número de otro.
            if (gestor != null) gestor.PermitirUnirse(!hayBots && !BloquearUniones);

            foreach (var b in ControladorBicho.Todos) b.Reaparecer();
        }

        /// <summary>Crea bots en los números de jugador que no ocupa ningún humano. Devuelve si creó alguno.</summary>
        bool RellenarConBots()
        {
            if (gestor == null) return false;
            gestor.EliminarBots();
            if (!rellenarConBots) return false;

            var ocupados = new HashSet<int>();
            foreach (var b in ControladorBicho.Todos)
                if (!b.EsBot) ocupados.Add(b.IndiceJugador);

            bool creados = false;
            for (int i = 0; i < jugadoresConBots; i++)
            {
                if (ocupados.Contains(i)) continue;
                CrearBot(i);
                creados = true;
            }
            return creados;
        }

        void EmpezarRonda()
        {
            EstadoActual = Estado.Jugando;
            TiempoRestante = duracionRonda;
            AlEmpezarRonda();
        }

        protected void TerminarRonda()
        {
            if (EstadoActual != Estado.Jugando) return;
            EstadoActual = Estado.Fin;
            TiempoRestante = 0f;
            // Se puede volver a unir gente; los bots se rehacen al empezar la siguiente ronda.
            if (gestor != null) gestor.PermitirUnirse(true);
            AlTerminarRonda();
        }

        // ---------------------------------------------------------------------
        // Equipos
        // ---------------------------------------------------------------------

        public static int EquipoDe(ControladorBicho bicho) => bicho.IndiceJugador % 2;

        protected int JugadoresEn(int equipo)
        {
            int n = 0;
            foreach (var b in ControladorBicho.Todos)
                if (EquipoDe(b) == equipo) n++;
            return n;
        }

        // ---------------------------------------------------------------------
        // Marcador en pantalla (provisional, con OnGUI)
        // ---------------------------------------------------------------------

        protected void Mensaje(string texto, Color color, float duracion) => mensajes.Add((texto, color, Time.time + duracion));

        protected static string Mayuscula(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        protected virtual void OnGUI()
        {
            PrepararEstilos();
            float w = Screen.width, h = Screen.height;

            switch (EstadoActual)
            {
                case Estado.Esperando:
                    Texto(new Rect(0, h * 0.12f, w, h * 0.1f), Titulo, estiloGrande, Color.white);
                    Texto(new Rect(0, h * 0.23f, w, h * 0.05f), Descripcion, estiloPequeno, Color.white);
                    Texto(new Rect(0, h * 0.30f, w, h * 0.05f),
                        $"Pulsa cualquier botón para unirte   ·   Jugadores 1 y 3: equipo {NombreEquipo(0)}   ·   2 y 4: equipo {NombreEquipo(1)}",
                        estiloPequeno, new Color(1f, 1f, 1f, 0.85f));
                    if (rellenarConBots && ControladorBicho.Todos.Count < jugadoresConBots)
                        Texto(new Rect(0, h * 0.35f, w, h * 0.05f), $"Los huecos hasta {jugadoresConBots} jugadores los ocupará la máquina", estiloPequeno, new Color(0.8f, 0.95f, 1f, 0.9f));
                    DibujarEsperandoExtra(w, h);
                    if (ControladorBicho.Todos.Count > 0)
                        Texto(new Rect(0, h * 0.78f, w, h * 0.07f), "START / ENTER para empezar", estiloMedio, Parpadeo(Color.yellow));
                    Texto(new Rect(0, h * 0.88f, w, h * 0.05f), Controles, estiloPequeno, new Color(1f, 1f, 1f, 0.7f));
                    break;

                case Estado.CuentaAtras:
                    int n = Mathf.CeilToInt(finCuentaAtras - Time.time);
                    float pulso = 1f - Mathf.Repeat(finCuentaAtras - Time.time, 1f);
                    Texto(new Rect(0, h * 0.3f, w, h * 0.25f), n.ToString(), estiloGrande, new Color(1f, 1f, 1f, 1f - pulso * 0.6f));
                    break;

                case Estado.Jugando:
                    DibujarMarcador(w, h);
                    break;

                case Estado.Fin:
                    DibujarMarcador(w, h);
                    int p0 = PuntosEquipo(0), p1 = PuntosEquipo(1);
                    int ganador = p0 == p1 ? -1 : p0 > p1 ? 0 : 1;
                    Texto(new Rect(0, h * 0.28f, w, h * 0.12f),
                        ganador < 0 ? "¡EMPATE!" : $"¡GANA EL EQUIPO {NombreEquipo(ganador).ToUpper()}!",
                        estiloGrande, ganador < 0 ? Color.white : ColorEquipo(ganador));
                    Texto(new Rect(0, h * 0.42f, w, h * 0.06f), $"{NombreEquipo(0)} {p0}  —  {p1} {NombreEquipo(1)}", estiloMedio, Color.white);
                    Texto(new Rect(0, h * 0.78f, w, h * 0.07f), "START / ENTER para otra ronda", estiloMedio, Parpadeo(Color.yellow));
                    break;
            }

            // Mensajes recientes, apilados bajo el reloj
            float y = h * 0.14f;
            for (int i = mensajes.Count - 1; i >= 0 && i >= mensajes.Count - 4; i--)
            {
                float vida = Mathf.Clamp01((mensajes[i].hasta - Time.time) / 0.4f);
                var c = mensajes[i].color; c.a = vida;
                Texto(new Rect(0, y, w, h * 0.05f), mensajes[i].texto, estiloPequeno, c);
                y += h * 0.045f;
            }
        }

        /// <summary>Líneas extra en la pantalla de espera (por debajo de la de bots).</summary>
        protected virtual void DibujarEsperandoExtra(float w, float h) { }

        /// <summary>Texto grande arriba en el centro. Por defecto, el reloj.</summary>
        protected virtual string TextoReloj
        {
            get
            {
                int segundos = Mathf.CeilToInt(Mathf.Max(0f, TiempoRestante));
                return $"{segundos / 60}:{segundos % 60:00}";
            }
        }

        protected virtual void DibujarMarcador(float w, float h)
        {
            int segundos = Mathf.CeilToInt(Mathf.Max(0f, TiempoRestante));
            bool urgente = EstadoActual == Estado.Jugando && segundos <= 10;
            Color colorReloj = urgente ? Parpadeo(new Color(1f, 0.35f, 0.3f)) : Color.white;
            Texto(new Rect(0, h * 0.02f, w, h * 0.1f), TextoReloj, estiloGrande, colorReloj);

            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? w * 0.03f : w * 0.67f;
                var anclaje = i == 0 ? TextAnchor.UpperLeft : TextAnchor.UpperRight;

                estiloMedio.alignment = anclaje;
                Texto(new Rect(x, h * 0.03f, w * 0.3f, h * 0.06f), $"{NombreEquipo(i).ToUpper()}  {PuntosEquipo(i)}", estiloMedio, ColorEquipo(i));
                estiloMedio.alignment = TextAnchor.MiddleCenter;

                estiloPequeno.alignment = anclaje;
                if (JugadoresEn(i) == 0)
                    Texto(new Rect(x, h * 0.09f, w * 0.3f, h * 0.04f), "sin jugadores", estiloPequeno, new Color(1f, 1f, 1f, 0.5f));
                else
                    DibujarInfoEquipo(i, x, w * 0.3f, h);
                estiloPequeno.alignment = TextAnchor.MiddleCenter;
            }
        }

        /// <summary>Información extra bajo el nombre de cada equipo (barra de integridad, etc.).</summary>
        protected virtual void DibujarInfoEquipo(int equipo, float x, float ancho, float h) { }

        protected void PrepararEstilos()
        {
            int tamano = Mathf.Max(12, Screen.height / 28);
            if (estiloGrande == null)
            {
                estiloGrande = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
                estiloMedio = new GUIStyle(estiloGrande);
                estiloPequeno = new GUIStyle(estiloGrande) { fontStyle = FontStyle.Normal };
            }
            estiloGrande.fontSize = tamano * 3;
            estiloMedio.fontSize = Mathf.RoundToInt(tamano * 1.3f);
            estiloPequeno.fontSize = Mathf.RoundToInt(tamano * 0.8f);
        }

        protected static void Texto(Rect r, string texto, GUIStyle estilo, Color color)
        {
            var sombra = new Color(0f, 0f, 0f, 0.65f * color.a);
            estilo.normal.textColor = sombra;
            float d = Mathf.Max(1f, estilo.fontSize * 0.06f);
            GUI.Label(new Rect(r.x + d, r.y + d, r.width, r.height), texto, estilo);
            estilo.normal.textColor = color;
            GUI.Label(r, texto, estilo);
        }

        protected static void Barra(Rect r, float valor, Color color)
        {
            var antes = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(valor), r.height - 4), Texture2D.whiteTexture);
            GUI.color = antes;
        }

        protected static Color Parpadeo(Color c) => Mathf.Repeat(Time.time * 2f, 1f) < 0.6f ? c : new Color(c.r, c.g, c.b, 0.35f);
    }
}
