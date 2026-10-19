using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Lo común a todas las pruebas del show: estados (Esperando → CuentaAtras → Jugando → Fin),
    /// START para empezar, reloj, rellenar con bots, saboteadores, mensajes y marcador.
    ///
    /// Equipos: jugadores 1 y 3 son el equipo 0; jugadores 2 y 4, el equipo 1.
    /// Cada prueba hereda de aquí y rellena los huecos (qué pasa al empezar, al jugar, al acabar).
    ///
    /// Se puede jugar suelta (START para empezar) o dentro del Show, que la configura y la
    /// arranca él (ConfigurarDesdeShow / IniciarDesdeShow).
    /// </summary>
    public abstract class RondaBase : MonoBehaviour
    {
        public enum Estado { Esperando, CuentaAtras, Jugando, Fin }

        /// <summary>Se lanza al terminar cualquier ronda (lo escucha el Show).</summary>
        public static event System.Action<RondaBase> AlTerminarAlguna;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos() => AlTerminarAlguna = null;

        [Header("Ronda")]
        public float duracionRonda = 90f;
        public float duracionCuentaAtras = 3f;

        [Header("Bots")]
        [Tooltip("Al empezar, rellenar con bots los huecos hasta llegar a 'Jugadores Con Bots'.")]
        public bool rellenarConBots = true;
        [Range(2, 4)] public int jugadoresConBots = 4;
        [Range(0f, 1f)] public float habilidadBots = 0.55f;
        public GestorJugadores gestor;

        [Header("Saboteadores")]
        [Tooltip("Quien esté dentro de esta zona al pulsar START será saboteador esa ronda.")]
        public Collider zonaSaboteadores;
        [Tooltip("Si nadie quiere sabotear, la máquina activa trampas.")]
        public bool saboteadorMaquina = true;
        [Range(0f, 1f)] public float agresividadMaquina = 0.5f;
        [Tooltip("Puntos para el saboteador por cada bicho que su trampa tira al suelo.")]
        public int puntosDerribo = 5;
        [Tooltip("Lo que pase hasta estos segundos después de activar una trampa cuenta para el saboteador.")]
        public float segundosAtribucion = 4f;

        public Estado EstadoActual { get; private set; } = Estado.Esperando;
        public float TiempoRestante { get; protected set; }
        /// <summary>La controla el Show: no espera a START ni ofrece repetir.</summary>
        public bool ControladaPorShow { get; private set; }
        public IReadOnlyList<Saboteador> Saboteadores => saboteadores;

        protected float finCuentaAtras;
        protected GUIStyle estiloGrande, estiloMedio, estiloPequeno;
        protected readonly List<Saboteador> saboteadores = new List<Saboteador>();
        readonly List<(string texto, Color color, float hasta)> mensajes = new List<(string, Color, float)>();
        HashSet<int> saboteadoresForzados;

        // ---------------------------------------------------------------------
        // Lo que cada prueba tiene que decir
        // ---------------------------------------------------------------------

        public abstract string Titulo { get; }
        protected abstract string Descripcion { get; }
        protected virtual string Controles => "Stick / WASD mover · A / espacio saltar · Gatillo / E agarrar · X / Q lanzarse o lanzar lo agarrado";
        /// <summary>Consejo grande que se ve los primeros segundos de la ronda (lo más importante de la prueba).</summary>
        protected virtual string ConsejoInicio => null;
        /// <summary>Segundos que lleva jugándose la ronda.</summary>
        protected float TiempoJugado => Mathf.Max(0f, duracionRonda - TiempoRestante);
        public abstract string NombreEquipo(int equipo);
        public abstract Color ColorEquipo(int equipo);
        public abstract int PuntosEquipo(int equipo);

        /// <summary>Crea un bot en ese número de jugador (con el cerebro que toque en esta prueba).</summary>
        protected abstract void CrearBot(int indice);

        /// <summary>Al pulsar START: limpiar la ronda anterior (antes de saboteadores y bots).</summary>
        protected virtual void AlPrepararRonda() { }
        /// <summary>Después de crear bots y saboteadores, antes de recolocar a todos.</summary>
        protected virtual void AlDespuesDeBots() { }
        /// <summary>Al acabar la cuenta atrás.</summary>
        protected abstract void AlEmpezarRonda();
        /// <summary>Cada frame mientras se juega.</summary>
        protected virtual void ActualizarJugando() { }
        /// <summary>Con el reloj a cero: si devuelve false, la ronda sigue (p. ej. punto de oro).</summary>
        protected virtual bool PuedeTerminar() => true;
        protected virtual void AlTerminarRonda() { }

        /// <summary>Puntos "jugosos" para que la máquina saboteadora apunte (objetos, pelota…).</summary>
        public virtual IEnumerable<Vector3> ObjetivosSabotaje() { yield break; }

        // ---------------------------------------------------------------------
        // Show
        // ---------------------------------------------------------------------

        /// <summary>
        /// El Show configura la ronda antes de arrancarla: bots, habilidad, duración y quién
        /// sabotea (por número de jugador; null = los de la plataforma, como jugando suelta).
        /// </summary>
        public void ConfigurarDesdeShow(bool bots, float habilidad, float duracion, ICollection<int> forzarSaboteadores)
        {
            ControladaPorShow = true;
            rellenarConBots = bots;
            habilidadBots = habilidad;
            duracionRonda = duracion;
            jugadoresConBots = 4;
            saboteadoresForzados = forzarSaboteadores != null ? new HashSet<int>(forzarSaboteadores) : new HashSet<int>();
        }

        public void IniciarDesdeShow()
        {
            if (EstadoActual == Estado.Esperando) EmpezarCuentaAtras();
        }

        // ---------------------------------------------------------------------
        // Bucle
        // ---------------------------------------------------------------------

        protected virtual void Start()
        {
            if (GestorJugadores.Instancia != null) gestor = GestorJugadores.Instancia;
            if (gestor == null) gestor = FindAnyObjectByType<GestorJugadores>();
            ControladorBicho.AlgunoDerribado += AlDerribar;
            Musica.Poner(Musica.Tema.Plato);
            Sonidos.Precargar();
        }

        protected virtual void OnDestroy()
        {
            ControladorBicho.AlgunoDerribado -= AlDerribar;
            TrampaSaboteable.ModoManual = false;
        }

        protected virtual void Update()
        {
            switch (EstadoActual)
            {
                case Estado.Esperando:
                case Estado.Fin:
                    if (!ControladaPorShow && ControladorBicho.Todos.Count > 0 && AlguienPulsoEmpezar())
                        EmpezarCuentaAtras();
                    break;

                case Estado.CuentaAtras:
                    PitarCadaSegundo(finCuentaAtras - Time.time, 1f);
                    if (Time.time >= finCuentaAtras) EmpezarRonda();
                    break;

                case Estado.Jugando:
                    TiempoRestante = Mathf.Max(0f, TiempoRestante - Time.deltaTime);
                    if (TiempoRestante <= 5.5f) PitarCadaSegundo(TiempoRestante, 0.6f); // ¡que se acaba!
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
            PrepararSaboteadores();
            bool hayBots = RellenarConBots();
            if (saboteadores.Count == 0 && saboteadorMaquina && TrampaSaboteable.Todas.Count > 0) CrearSaboteadorMaquina();
            TrampaSaboteable.ModoManual = saboteadores.Count > 0;
            AlDespuesDeBots();

            // Con bots o saboteadores en juego, nadie más puede unirse hasta el final:
            // le tocaría el número de otro. En el Show, nunca durante las pruebas.
            if (gestor != null) gestor.PermitirUnirse(!ControladaPorShow && !hayBots && saboteadores.Count == 0);

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
            Sonidos.Reproducir(Sonido.PitidoSalida);
            Sonidos.Reproducir(Sonido.Silbato);
            Musica.Poner(Musica.Tema.Prueba);
            Estadisticas.InicioRonda(this);
            AlEmpezarRonda();
        }

        int ultimoPitido = -1;

        /// <summary>Un pitido cada vez que cambia el segundo que queda (cuenta atrás y final de ronda).</summary>
        void PitarCadaSegundo(float quedan, float volumen)
        {
            int segundo = Mathf.CeilToInt(quedan);
            if (segundo == ultimoPitido || segundo <= 0) return;
            ultimoPitido = segundo;
            Sonidos.Reproducir(Sonido.Pitido, null, volumen, 1f, 0f);
        }

        protected void TerminarRonda()
        {
            if (EstadoActual != Estado.Jugando) return;
            EstadoActual = Estado.Fin;
            TiempoRestante = 0f;
            ultimoPitido = -1;
            Sonidos.Reproducir(Sonido.Bocina);
            Sonidos.Reproducir(Sonido.Aplausos);
            Musica.Poner(Musica.Tema.Plato);

            // Los saboteadores humanos recuperan su bicho (su marcador sigue en pantalla).
            TrampaSaboteable.ModoManual = false;
            foreach (var s in saboteadores)
            {
                if (s == null || s.EsMaquina) continue;
                var bicho = s.GetComponent<ControladorBicho>();
                if (bicho != null) bicho.Retirar(false);
            }

            // Se puede volver a unir gente; los bots se rehacen al empezar la siguiente ronda.
            if (gestor != null && !ControladaPorShow) gestor.PermitirUnirse(true);

            AlTerminarRonda();
            Estadisticas.FinRonda(this); // después: así incluye los puntos que se dan al acabar
            AlTerminarAlguna?.Invoke(this);
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
        // Saboteadores
        // ---------------------------------------------------------------------

        protected bool EnZonaSaboteadores(ControladorBicho b) =>
            zonaSaboteadores != null && zonaSaboteadores.bounds.Contains(b.transform.position + Vector3.up * 0.5f);

        /// <summary>
        /// Quita los saboteadores de la ronda anterior y elige los nuevos: los que mande el Show
        /// o, jugando suelta, quien esté en la plataforma de saboteadores.
        /// </summary>
        void PrepararSaboteadores()
        {
            foreach (var s in saboteadores)
            {
                if (s == null) continue;
                if (s.EsMaquina) Destroy(s.gameObject);
                else
                {
                    var bichoAnterior = s.GetComponent<ControladorBicho>();
                    if (bichoAnterior != null) bichoAnterior.Retirar(false);
                    Destroy(s);
                }
            }
            saboteadores.Clear();
            if (TrampaSaboteable.Todas.Count == 0) return; // esta prueba no tiene trampas

            var elegidos = new List<ControladorBicho>();
            foreach (var b in ControladorBicho.Todos)
            {
                if (b.EsBot) continue;
                bool elegido = saboteadoresForzados != null ? saboteadoresForzados.Contains(b.IndiceJugador) : EnZonaSaboteadores(b);
                if (elegido) elegidos.Add(b);
            }

            foreach (var b in elegidos)
            {
                var s = b.gameObject.AddComponent<Saboteador>();
                s.Configurar(this, b.GetComponent<PlayerInput>(), b.ColorJugador, $"Saboteador J{b.IndiceJugador + 1}",
                             agresividadMaquina, saboteadores.Count, b.IndiceJugador);
                b.Retirar(true);
                saboteadores.Add(s);
            }
        }

        void CrearSaboteadorMaquina()
        {
            var go = new GameObject("SaboteadorMaquina");
            var s = go.AddComponent<Saboteador>();
            s.Configurar(this, null, new Color(0.8f, 0.45f, 1f), "Saboteador máquina", agresividadMaquina, 0, -1);
            saboteadores.Add(s);
        }

        /// <summary>El saboteador cuya trampa, activada hace poco, tiene este punto dentro de su alcance.</summary>
        protected Saboteador Responsable(Vector3 punto)
        {
            Saboteador mejor = null;
            float mejorDistancia = float.MaxValue;
            foreach (var t in TrampaSaboteable.Todas)
            {
                if (t.UltimoSaboteador == null || Time.time - t.UltimoUso > segundosAtribucion) continue;
                Vector3 d = t.CentroEfecto - punto;
                d.y = 0f;
                float distancia = d.magnitude;
                if (distancia < t.radioEfecto && distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    mejor = t.UltimoSaboteador;
                }
            }
            return mejor;
        }

        void AlDerribar(ControladorBicho bicho)
        {
            if (EstadoActual != Estado.Jugando) return;
            var s = Responsable(bicho.transform.position);
            if (s != null) s.SumarPuntos(puntosDerribo, "¡al suelo!");
        }

        // ---------------------------------------------------------------------
        // Marcador en pantalla (provisional, con OnGUI)
        // ---------------------------------------------------------------------

        protected void Mensaje(string texto, Color color, float duracion) => mensajes.Add((texto, color, Time.time + duracion));

        protected static string Mayuscula(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        protected virtual void OnGUI()
        {
            if (ControladaPorShow && ShowBicho.Instancia != null && ShowBicho.Instancia.TapaLaPantalla) return;
            PrepararEstilos();
            float w = Screen.width, h = Screen.height;

            switch (EstadoActual)
            {
                case Estado.Esperando:
                    if (ControladaPorShow) break; // el Show pone su propio cartel
                    Texto(new Rect(0, h * 0.12f, w, h * 0.1f), Titulo, estiloGrande, Color.white);
                    Texto(new Rect(0, h * 0.23f, w, h * 0.05f), Descripcion, estiloPequeno, Color.white);
                    Texto(new Rect(0, h * 0.30f, w, h * 0.05f),
                        $"Pulsa cualquier botón para unirte   ·   Jugadores 1 y 3: equipo {NombreEquipo(0)}   ·   2 y 4: equipo {NombreEquipo(1)}",
                        estiloPequeno, new Color(1f, 1f, 1f, 0.85f));
                    if (rellenarConBots && ControladorBicho.Todos.Count < jugadoresConBots)
                        Texto(new Rect(0, h * 0.35f, w, h * 0.05f), $"Los huecos hasta {jugadoresConBots} jugadores los ocupará la máquina", estiloPequeno, new Color(0.8f, 0.95f, 1f, 0.9f));
                    if (zonaSaboteadores != null)
                    {
                        string enPlataforma = "";
                        foreach (var b in ControladorBicho.Todos)
                            if (!b.EsBot && EnZonaSaboteadores(b)) enPlataforma += $" J{b.IndiceJugador + 1}";
                        string linea = enPlataforma.Length > 0
                            ? $"Serán SABOTEADORES:{enPlataforma}"
                            : "Súbete a la plataforma morada para ser SABOTEADOR";
                        Texto(new Rect(0, h * 0.40f, w, h * 0.05f), linea, estiloPequeno, new Color(0.85f, 0.6f, 1f));
                    }
                    if (ControladorBicho.Todos.Count > 0)
                        Texto(new Rect(0, h * 0.78f, w, h * 0.07f), "START / ENTER para empezar", estiloMedio, Parpadeo(Color.yellow));
                    Texto(new Rect(0, h * 0.88f, w, h * 0.05f), Controles, estiloPequeno, new Color(1f, 1f, 1f, 0.7f));
                    break;

                case Estado.CuentaAtras:
                    int n = Mathf.CeilToInt(finCuentaAtras - Time.time);
                    float pulso = 1f - Mathf.Repeat(finCuentaAtras - Time.time, 1f);
                    Texto(new Rect(0, h * 0.3f, w, h * 0.25f), n.ToString(), estiloGrande, new Color(1f, 1f, 1f, 1f - pulso * 0.6f));
                    Texto(new Rect(0, h * 0.88f, w, h * 0.05f), Controles, estiloPequeno, new Color(1f, 1f, 1f, 0.7f));
                    break;

                case Estado.Jugando:
                    DibujarMarcador(w, h);
                    // Los primeros segundos: lo más importante de la prueba, bien grande.
                    if (TiempoJugado < 9f && !string.IsNullOrEmpty(ConsejoInicio))
                    {
                        float alfa = Mathf.Clamp01((9f - TiempoJugado) / 1f);
                        var amarillo = Parpadeo(new Color(1f, 0.9f, 0.35f)); amarillo.a *= alfa;
                        Texto(new Rect(0, h * 0.8f, w, h * 0.07f), ConsejoInicio, estiloMedio, amarillo);
                        Texto(new Rect(0, h * 0.88f, w, h * 0.05f), Controles, estiloPequeno, new Color(1f, 1f, 1f, 0.7f * alfa));
                    }
                    break;

                case Estado.Fin:
                    DibujarMarcador(w, h);
                    int p0 = PuntosEquipo(0), p1 = PuntosEquipo(1);
                    int ganador = p0 == p1 ? -1 : p0 > p1 ? 0 : 1;
                    Texto(new Rect(0, h * 0.28f, w, h * 0.12f),
                        ganador < 0 ? "¡EMPATE!" : $"¡GANA EL EQUIPO {NombreEquipo(ganador).ToUpper()}!",
                        estiloGrande, ganador < 0 ? Color.white : ColorEquipo(ganador));
                    Texto(new Rect(0, h * 0.42f, w, h * 0.06f), $"{NombreEquipo(0)} {p0}  —  {p1} {NombreEquipo(1)}", estiloMedio, Color.white);
                    if (!ControladaPorShow)
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

        public static void Texto(Rect r, string texto, GUIStyle estilo, Color color)
        {
            var sombra = new Color(0f, 0f, 0f, 0.65f * color.a);
            estilo.normal.textColor = sombra;
            float d = Mathf.Max(1f, estilo.fontSize * 0.06f);
            GUI.Label(new Rect(r.x + d, r.y + d, r.width, r.height), texto, estilo);
            estilo.normal.textColor = color;
            GUI.Label(r, texto, estilo);
        }

        public static void Barra(Rect r, float valor, Color color)
        {
            var antes = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(valor), r.height - 4), Texture2D.whiteTexture);
            GUI.color = antes;
        }

        public static Color Parpadeo(Color c) => Mathf.Repeat(Time.time * 2f, 1f) < 0.6f ? c : new Color(c.r, c.g, c.b, 0.35f);
    }
}
