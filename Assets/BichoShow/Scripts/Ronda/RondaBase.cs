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
                    DibujarEspera(w, h);
                    break;

                case Estado.CuentaAtras:
                    {
                        // Cada número entra grande y se encoge, con su color.
                        int n = Mathf.CeilToInt(finCuentaAtras - Time.time);
                        float pulso = 1f - Mathf.Repeat(finCuentaAtras - Time.time, 1f); // 0 al empezar cada segundo
                        float escala = 1f + 0.7f * Mathf.Pow(1f - pulso, 3f);
                        Color[] colores = { EstiloShow.Oro, EstiloShow.Rosa, EstiloShow.Cian };
                        var centro = new Rect(0, h * 0.27f, w, h * 0.3f);
                        EstiloShow.Texto(centro, n.ToString(), Mathf.RoundToInt(EstiloShow.Px(230f) * escala),
                                         colores[Mathf.Abs(n) % colores.Length].WithAlpha(1f - pulso * 0.4f), TextAnchor.MiddleCenter, EstiloShow.Fuente.Titulo, false, 1.3f);
                        EstiloShow.Texto(new Rect(0, h * 0.56f, w, h * 0.08f), Titulo, Mathf.RoundToInt(EstiloShow.Px(64f)), Color.white);
                        DibujarControles(h - EstiloShow.Px(150f), 1f);
                    }
                    break;

                case Estado.Jugando:
                    DibujarMarcador(w, h);
                    // Los primeros segundos: lo más importante de la prueba, bien grande.
                    if (TiempoJugado < 9f && !string.IsNullOrEmpty(ConsejoInicio))
                    {
                        float alfa = Mathf.Clamp01((9f - TiempoJugado) / 1f);
                        float latido = 1f + Mathf.Sin(Time.time * 6f) * 0.03f;
                        EstiloShow.TextoEnPastilla(new Vector2(w * 0.5f, h - EstiloShow.Px(240f)), ConsejoInicio,
                            Mathf.RoundToInt(EstiloShow.Px(40f) * latido), EstiloShow.Oro.WithAlpha(alfa), EstiloShow.MoradoOscuro.WithAlpha(0.85f),
                            EstiloShow.Fuente.Titulo);
                        DibujarControles(h - EstiloShow.Px(150f), alfa);
                    }
                    break;

                case Estado.Fin:
                    DibujarMarcador(w, h);
                    int p0 = PuntosEquipo(0), p1 = PuntosEquipo(1);
                    int ganador = p0 == p1 ? -1 : p0 > p1 ? 0 : 1;
                    EstiloShow.Banda(new Rect(0, h * 0.27f, w, h * 0.24f), EstiloShow.MoradoOscuro.WithAlpha(0.8f));
                    EstiloShow.TituloRebote(new Rect(0, h * 0.28f, w, h * 0.14f),
                        ganador < 0 ? "¡EMPATE!" : $"¡GANA EL EQUIPO {NombreEquipo(ganador).ToUpper()}!",
                        Mathf.RoundToInt(EstiloShow.Px(110f)),
                        ganador < 0 ? new[] { Color.white, EstiloShow.Oro } : new[] { ColorEquipo(ganador), Color.white, EstiloShow.Oro });
                    Texto(new Rect(0, h * 0.42f, w, h * 0.07f), $"{NombreEquipo(0)} {p0}  —  {p1} {NombreEquipo(1)}", estiloMedio, Color.white);
                    if (!ControladaPorShow)
                        Texto(new Rect(0, h * 0.78f, w, h * 0.07f), "START / ENTER para otra ronda", estiloMedio, Parpadeo(Color.yellow));
                    break;
            }

            // Mensajes recientes, apilados bajo el reloj
            // (cada uno en su pastilla oscura para que se lea sobre cualquier suelo)
            float y = EstiloShow.Px(150f);
            int tamanoMensaje = Mathf.RoundToInt(EstiloShow.Px(30f));
            for (int i = mensajes.Count - 1; i >= 0 && i >= mensajes.Count - 4; i--)
            {
                float vida = Mathf.Clamp01((mensajes[i].hasta - Time.time) / 0.4f);
                var c = mensajes[i].color; c.a = vida;
                EstiloShow.TextoEnPastilla(new Vector2(w * 0.5f, y + tamanoMensaje), mensajes[i].texto, tamanoMensaje, c);
                y += tamanoMensaje * 2.1f;
            }
        }

        /// <summary>
        /// Jugando la prueba suelta (sin el show): tarjeta centrada con el título, de qué va,
        /// los equipos, bots y saboteador, y el botón de empezar.
        /// </summary>
        void DibujarEspera(float w, float h)
        {
            System.Func<float, float> px = EstiloShow.Px;
            System.Func<float, int> tam = v => Mathf.RoundToInt(EstiloShow.Px(v));

            var tarjeta = new Rect(w * 0.5f - Mathf.Min(px(700f), w * 0.46f), h * 0.07f, Mathf.Min(px(1400f), w * 0.92f), px(560f));
            EstiloShow.Panel(tarjeta, EstiloShow.MoradoOscuro.WithAlpha(0.86f), px(40f));
            float m = px(50f);
            var dentro = new Rect(tarjeta.x + m, tarjeta.y, tarjeta.width - m * 2f, tarjeta.height);

            EstiloShow.TituloRebote(new Rect(tarjeta.x, tarjeta.y + px(24f), tarjeta.width, px(130f)), Titulo,
                                    Mathf.Min(tam(110f), Mathf.FloorToInt(tam(110f) * dentro.width / Mathf.Max(1f, EstiloShow.Ancho(Titulo, tam(110f))))),
                                    new[] { EstiloShow.Oro, EstiloShow.Rosa, EstiloShow.Cian, Color.white }, 0.6f);
            EstiloShow.Texto(new Rect(dentro.x, tarjeta.y + px(160f), dentro.width, px(90f)), Descripcion, tam(34f), Color.white,
                             TextAnchor.UpperCenter, EstiloShow.Fuente.Texto, true);

            // Equipos: dos pastillas de color
            float y = tarjeta.y + px(270f);
            float anchoEquipo = Mathf.Min(px(420f), dentro.width * 0.48f);
            for (int i = 0; i < 2; i++)
            {
                var r = new Rect(tarjeta.center.x + (i == 0 ? -anchoEquipo - px(14f) : px(14f)), y, anchoEquipo, px(70f));
                EstiloShow.Panel(r, Color.Lerp(ColorEquipo(i), EstiloShow.MoradoOscuro, 0.2f), r.height * 0.5f);
                EstiloShow.Texto(r, $"{(i == 0 ? "J1 y J3" : "J2 y J4")}  ·  {NombreEquipo(i).ToUpper()}", tam(36f), Color.white);
            }
            y += px(92f);

            string bots = rellenarConBots && ControladorBicho.Todos.Count < jugadoresConBots
                ? $"Los huecos hasta {jugadoresConBots} los ocupa la máquina" : "";
            if (bots.Length > 0)
                EstiloShow.Texto(new Rect(dentro.x, y, dentro.width, px(44f)), bots, tam(30f), EstiloShow.Cian, TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto);
            y += px(50f);
            if (zonaSaboteadores != null)
            {
                string enPlataforma = "";
                foreach (var b in ControladorBicho.Todos)
                    if (!b.EsBot && EnZonaSaboteadores(b)) enPlataforma += $" J{b.IndiceJugador + 1}";
                string linea = enPlataforma.Length > 0 ? $"Serán SABOTEADORES:{enPlataforma}" : "Súbete a la plataforma morada para ser SABOTEADOR";
                EstiloShow.Texto(new Rect(dentro.x, y, dentro.width, px(44f)), linea, tam(30f), EstiloShow.Lila, TextAnchor.MiddleCenter, EstiloShow.Fuente.Texto);
            }

            // Botón de empezar (o cómo unirse, si aún no hay nadie)
            var boton = new Rect(tarjeta.center.x - px(330f), tarjeta.yMax - px(36f), px(660f), px(88f));
            if (ControladorBicho.Todos.Count > 0)
                EstiloShow.Boton(boton, "START / ENTER para empezar", true, tam(40f));
            else
                EstiloShow.Boton(boton, "Pulsa un botón para unirte", true, tam(40f));

            DibujarControles(h - px(150f), 1f);
        }

        /// <summary>Barra de controles con botones de mando (y las teclas debajo, en pequeño).</summary>
        protected void DibujarControles(float y, float alfa)
        {
            if (alfa <= 0.01f) return;
            var antes = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alfa);
            float px50 = EstiloShow.Px(50f);
            EstiloShow.Ayuda(new Rect(0, y, Screen.width, px50),
                ("Stick", EstiloShow.GlifoTecla, "mover"), ("A", EstiloShow.GlifoA, "saltar"),
                ("RT", EstiloShow.GlifoTecla, "agarrar"), ("X", EstiloShow.GlifoX, AccionX));
            EstiloShow.TextoEnPastilla(new Vector2(Screen.width * 0.5f, y + px50 + EstiloShow.Px(34f)),
                $"Teclado: WASD mover · espacio saltar · E agarrar · Q {AccionX}", Mathf.RoundToInt(EstiloShow.Px(24f)), new Color(1f, 1f, 1f, alfa));
            GUI.color = antes;
        }

        /// <summary>Qué hace el botón X en esta prueba.</summary>
        protected virtual string AccionX => "lanzarse";

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

            // Reloj en una píldora (roja y latiendo en los últimos 10 s)
            var reloj = new Rect(w * 0.5f - EstiloShow.Px(120f), EstiloShow.Px(16f), EstiloShow.Px(240f), EstiloShow.Px(92f));
            if (urgente) reloj = EstiloShow.Escalar(reloj, 1f + 0.06f * Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI)));
            EstiloShow.Panel(reloj, urgente ? new Color(0.8f, 0.16f, 0.22f, 0.92f) : EstiloShow.Morado.WithAlpha(0.9f), reloj.height * 0.5f);
            EstiloShow.Texto(reloj, TextoReloj, Mathf.RoundToInt(EstiloShow.Px(64f)), urgente ? Color.white : EstiloShow.Oro);

            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? w * 0.03f : w * 0.67f;
                var anclaje = i == 0 ? TextAnchor.UpperLeft : TextAnchor.UpperRight;

                // Panel del equipo con su color: nombre pequeño y puntos grandes
                var panel = new Rect(x, EstiloShow.Px(18f), w * 0.3f, EstiloShow.Px(84f));
                EstiloShow.Panel(panel, Color.Lerp(ColorEquipo(i), EstiloShow.MoradoOscuro, 0.35f).WithAlpha(0.9f), EstiloShow.Px(26f));
                float margen = EstiloShow.Px(22f);
                var dentro = new Rect(panel.x + margen, panel.y, panel.width - margen * 2f, panel.height);
                EstiloShow.Texto(dentro, NombreEquipo(i).ToUpper(), Mathf.RoundToInt(EstiloShow.Px(40f)), Color.white,
                                 i == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
                EstiloShow.Texto(dentro, PuntosEquipo(i).ToString(), Mathf.RoundToInt(EstiloShow.Px(60f)), EstiloShow.Oro,
                                 i == 0 ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);

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

        /// <summary>
        /// Texto de la interfaz con el estilo del show (contorno, sombra, brillo). Del estilo se usa
        /// el tamaño, la alineación y si se parte en líneas; los tamaños grandes van con la fuente de títulos.
        /// </summary>
        public static void Texto(Rect r, string texto, GUIStyle estilo, Color color)
        {
            var fuente = estilo.fontSize >= Screen.height / 22 ? EstiloShow.Fuente.Titulo : EstiloShow.Fuente.Texto;
            EstiloShow.Texto(r, texto, estilo.fontSize, color, estilo.alignment, fuente, estilo.wordWrap);
        }

        public static void Barra(Rect r, float valor, Color color) => EstiloShow.Barra(r, valor, color);

        public static Color Parpadeo(Color c) => Mathf.Repeat(Time.time * 2f, 1f) < 0.6f ? c : new Color(c.r, c.g, c.b, 0.35f);
    }
}
