using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BichoShow
{
    /// <summary>
    /// Partida online con salas por código (Unity Multiplayer Services: Lobby + Relay, sobre
    /// Netcode for GameObjects). Quien crea la sala es el anfitrión y ejecuta TODO el juego como
    /// siempre (física, pruebas, bots); los invitados solo mandan su mando y ven copias
    /// ("marionetas") de los bichos del anfitrión.
    ///
    ///   invitado → anfitrión  "BS.Mando"   stick, botones y ropa de cada jugador de ese ordenador
    ///   anfitrión → invitados "BS.Estado"  posición y pose de cada bicho (unas 30 veces por segundo)
    ///   anfitrión → invitados "BS.Evento"  saltos, golpes, reapariciones (para sonidos y efectos)
    ///   anfitrión → un invitado "BS.Tiempo" el tiempo de uno de sus jugadores al cruzar la meta (para su ranking)
    ///
    /// No usa NetworkObjects ni prefabs de red: el juego local no cambia y cada escena se monta
    /// igual en todos los ordenadores.
    /// </summary>
    public class RedShow : MonoBehaviour
    {
        public enum Modo { Local, Anfitrion, Invitado }

        public const int MaxJugadores = 4;
        public const int LargoCodigo = 6;
        const float EnviosPorSegundo = 30f;
        /// <summary>Los invitados dibujan los bichos este tiempo en el pasado, para tener siempre dos fotos entre las que interpolar.</summary>
        const float Retraso = 0.1f;

        const string MensajeMando = "BS.Mando", MensajeEstado = "BS.Estado", MensajeEvento = "BS.Evento";
        const string MensajeEscena = "BS.Escena", MensajeListo = "BS.Listo", MensajeCambios = "BS.Cambios",
                     MensajeMovimientos = "BS.Mov", MensajePantalla = "BS.Pantalla", MensajeAvisos = "BS.Avisos",
                     MensajeHola = "BS.Hola", MensajeTiempo = "BS.Tiempo";
        const byte EventoAccion = 0, EventoGolpe = 1, EventoReaparecer = 2;

        public static Modo ModoActual { get; private set; }
        public static bool EnLinea => ModoActual != Modo.Local;
        public static bool EsAnfitrion => ModoActual == Modo.Anfitrion;
        public static bool EsInvitado => ModoActual == Modo.Invitado;
        /// <summary>Código de la sala para pasárselo a los amigos.</summary>
        public static string Codigo { get; private set; }
        /// <summary>Conectando, creando la sala…: el menú no deja hacer nada más.</summary>
        public static bool Ocupado { get; private set; }
        /// <summary>Último aviso para el menú ("No hay ninguna sala con ese código"…). Vacío si todo va bien.</summary>
        public static string Aviso { get; private set; } = "";
        /// <summary>Ordenadores en la sala (contando el del anfitrión).</summary>
        public static int Ordenadores => instancia != null && instancia.sesion != null ? instancia.sesion.PlayerCount : 0;

        /// <summary>En un invitado: momento (en el reloj del anfitrión) que se está dibujando ahora.</summary>
        public static double TiempoDibujo => Time.unscaledTimeAsDouble + desfase - Retraso;

        /// <summary>En un invitado: las copias de los bichos del anfitrión.</summary>
        public static IEnumerable<MarionetaBicho> Marionetas =>
            instancia != null ? instancia.marionetas.Values : (IEnumerable<MarionetaBicho>)Array.Empty<MarionetaBicho>();

        static RedShow instancia;
        static double desfase;
        static bool hayDesfase;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos()
        {
            instancia = null;
            ModoActual = Modo.Local;
            Codigo = null;
            Ocupado = false;
            PartidaRapida = Buscando = false;
            cancelarBusqueda = null;
            Aviso = "";
            desfase = 0;
            hayDesfase = false;
        }

        NetworkManager red;
        ISession sesion;
        float acumulado;
        bool manejadores;

        // Anfitrión
        readonly Dictionary<(ulong, byte), EntradaRemota> entradas = new Dictionary<(ulong, byte), EntradaRemota>();
        readonly Dictionary<ControladorBicho, byte> ids = new Dictionary<ControladorBicho, byte>();
        readonly HashSet<ControladorBicho> escuchados = new HashSet<ControladorBicho>();
        readonly List<ControladorBicho> paraEnviar = new List<ControladorBicho>();
        readonly List<ulong> destinos = new List<ulong>();
        byte siguienteId;

        // Invitado
        readonly Dictionary<byte, MarionetaBicho> marionetas = new Dictionary<byte, MarionetaBicho>();
        readonly HashSet<byte> vistos = new HashSet<byte>();
        readonly List<byte> sobran = new List<byte>();

        // ---------------------------------------------------------------------
        // Crear sala, unirse, salir
        // ---------------------------------------------------------------------

        /// <summary>Borra el último aviso (al volver a entrar en el menú online).</summary>
        public static void LimpiarAviso()
        {
            if (!Ocupado) Aviso = "";
        }

        /// <summary>Crea una sala nueva con este ordenador de anfitrión.</summary>
        public static async void CrearSala()
        {
            if (Ocupado || EnLinea) return;
            Ocupado = true;
            Aviso = "";
            try
            {
                var r = Asegurar();
                await IniciarServicios();
                var opciones = new SessionOptions { MaxPlayers = MaxJugadores, IsPrivate = true }.WithRelayNetwork();
                var s = await MultiplayerService.Instance.CreateSessionAsync(opciones);
                r.Empezar(s, Modo.Anfitrion);
            }
            catch (Exception e)
            {
                Aviso = Explicar(e);
                Debug.LogWarning($"Bicho Show: no se ha podido crear la sala. {e}");
            }
            finally
            {
                Ocupado = false;
            }
        }

        /// <summary>Entra en la sala de un amigo con su código.</summary>
        public static async void Unirse(string codigo)
        {
            if (Ocupado || EnLinea) return;
            codigo = (codigo ?? "").Trim().ToUpperInvariant();
            if (codigo.Length == 0) { Aviso = "Escribe el código de la sala."; return; }
            Ocupado = true;
            Aviso = "";
            try
            {
                var r = Asegurar();
                await IniciarServicios();
                var s = await MultiplayerService.Instance.JoinSessionByCodeAsync(codigo);
                r.Empezar(s, Modo.Invitado);
            }
            catch (Exception e)
            {
                Aviso = Explicar(e);
                Debug.LogWarning($"Bicho Show: no se ha podido entrar en la sala {codigo}. {e}");
            }
            finally
            {
                Ocupado = false;
            }
        }

        // ---------------------------------------------------------------------
        // Partida rápida: sesiones públicas que se juntan solas, sin código
        // ---------------------------------------------------------------------

        /// <summary>Lo que distingue las partidas rápidas de esta versión del juego (no se mezclan versiones).</summary>
        static string ClaveRapida => "rapida-" + Application.version;
        const string PropiedadSalida = "salida";

        /// <summary>La sesión actual es de partida rápida (pública, empieza sola).</summary>
        public static bool PartidaRapida { get; private set; }
        /// <summary>Buscando partida (se puede cancelar).</summary>
        public static bool Buscando { get; private set; }

        static System.Threading.CancellationTokenSource cancelarBusqueda;

        /// <summary>
        /// Busca una partida rápida con hueco de esta versión; si no hay ninguna en unos segundos,
        /// crea una (pública) y este ordenador hace de anfitrión.
        /// </summary>
        public static async void BuscarPartida()
        {
            if (Ocupado || EnLinea) return;
            Ocupado = Buscando = true;
            Aviso = "";
            cancelarBusqueda = new System.Threading.CancellationTokenSource();
            try
            {
                var r = Asegurar();
                await IniciarServicios();
                var buscar = new QuickJoinOptions
                {
                    Filters = { new FilterOption(FilterField.StringIndex1, ClaveRapida, FilterOperation.Equal) },
                    Timeout = TimeSpan.FromSeconds(4),
                    CreateSession = true,
                };
                var opciones = new SessionOptions
                {
                    MaxPlayers = MaxJugadores,
                    IsPrivate = false,
                    SessionProperties = { [ "modo" ] = new SessionProperty(ClaveRapida, VisibilityPropertyOptions.Public, PropertyIndex.String1) },
                }.WithRelayNetwork();
                var s = await MultiplayerService.Instance.MatchmakeSessionAsync(buscar, opciones, cancelarBusqueda.Token);
                PartidaRapida = true;
                r.Empezar(s, s.IsHost ? Modo.Anfitrion : Modo.Invitado);
            }
            catch (OperationCanceledException)
            {
                Aviso = "";
            }
            catch (Exception e)
            {
                Aviso = Explicar(e);
                Debug.LogWarning($"Bicho Show: no se ha podido buscar partida. {e}");
            }
            finally
            {
                Ocupado = Buscando = false;
                cancelarBusqueda = null;
            }
        }

        /// <summary>Deja de buscar partida.</summary>
        public static void CancelarBusqueda() => cancelarBusqueda?.Cancel();

        /// <summary>
        /// En partida rápida, el anfitrión anuncia a todos cuándo empieza el show (dentro de estos
        /// segundos). Con un número negativo, quita el anuncio.
        /// </summary>
        public static async void AnunciarSalida(float segundos)
        {
            var s = instancia != null ? instancia.sesion : null;
            if (s == null || !EsAnfitrion) return;
            try
            {
                string valor = segundos < 0f ? "" : DateTime.UtcNow.AddSeconds(segundos).Ticks.ToString();
                s.AsHost().SetProperty(PropiedadSalida, new SessionProperty(valor, VisibilityPropertyOptions.Member));
                await s.AsHost().SavePropertiesAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Bicho Show: no se ha podido anunciar la salida. {e}");
            }
        }

        /// <summary>Segundos que faltan para que empiece el show de la partida rápida (null si no hay cuenta atrás).</summary>
        public static float? SegundosParaSalida
        {
            get
            {
                var s = instancia != null ? instancia.sesion : null;
                if (s == null || s.Properties == null || !s.Properties.TryGetValue(PropiedadSalida, out var p) || p == null) return null;
                if (!long.TryParse(p.Value, out long ticks)) return null;
                return Mathf.Max(0f, (float)(new DateTime(ticks, DateTimeKind.Utc) - DateTime.UtcNow).TotalSeconds);
            }
        }

        /// <summary>El anfitrión cierra la sesión a gente nueva (mientras dura el show) o la vuelve a abrir.</summary>
        public static async void CerrarAGenteNueva(bool cerrar)
        {
            var s = instancia != null ? instancia.sesion : null;
            if (s == null || !EsAnfitrion || s.IsLocked == cerrar) return;
            try
            {
                s.AsHost().IsLocked = cerrar;
                await s.AsHost().SavePropertiesAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Bicho Show: no se ha podido {(cerrar ? "cerrar" : "abrir")} la sesión. {e}");
            }
        }

        /// <summary>Sale de la sala (si es el anfitrión, la cierra para todos).</summary>
        public static async void Salir()
        {
            if (instancia == null || !EnLinea || Ocupado) return;
            Ocupado = true;
            var s = instancia.sesion;
            instancia.sesion = null;
            try
            {
                if (s != null) await s.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Bicho Show: al salir de la sala. {e}");
            }
            finally
            {
                if (instancia != null) instancia.Limpiar("");
                Ocupado = false;
            }
        }

        static RedShow Asegurar()
        {
            if (instancia != null) return instancia;

            // Se monta desactivado para dar la configuración al NetworkManager antes de su Awake.
            var go = new GameObject("Red (Bicho Show)");
            go.SetActive(false);
            var transporte = go.AddComponent<UnityTransport>();
            var nm = go.AddComponent<NetworkManager>();
            nm.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transporte,
                EnableSceneManagement = false, // cada ordenador carga sus escenas (lo ordena el anfitrión)
                ConnectionApproval = false,
                TickRate = 30,
            };
            transporte.DisconnectTimeoutMS = 10000; // darse cuenta antes de que alguien se ha ido (por defecto, 30 s)
            nm.RunInBackground = true; // con dos ventanas en el mismo PC, la de atrás sigue jugando
            instancia = go.AddComponent<RedShow>();
            instancia.red = nm;
            DontDestroyOnLoad(go);
            go.SetActive(true);

            nm.OnClientStarted += instancia.AlArrancarRed;
            nm.OnClientStopped += instancia.AlPararRed;
            nm.OnClientDisconnectCallback += instancia.AlIrseCliente;
            return instancia;
        }

        /// <summary>Unity Services y la sesión anónima (para salas y para el ranking mundial).</summary>
        public static async Task IniciarServicios()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                // Un perfil distinto en cada ejecución: así dos copias del juego en el mismo PC
                // (o los jugadores virtuales de Multiplayer Play Mode) son jugadores distintos.
                var opciones = new InitializationOptions().SetProfile("j" + Guid.NewGuid().ToString("N").Substring(0, 20));
                await UnityServices.InitializeAsync(opciones);
            }
            while (UnityServices.State == ServicesInitializationState.Initializing) await Task.Yield();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        static string Explicar(Exception e)
        {
            if (e is SessionException se)
            {
                switch (se.Error)
                {
                    case SessionError.SessionNotFound:
                    case SessionError.InvalidSessionIdentifier:
                        return "No hay ninguna sala con ese código. ¿Lo has escrito bien?";
                    case SessionError.SessionDeleted:
                        return "Esa sala ya se ha cerrado.";
                    case SessionError.Forbidden:
                        return "La sala está llena.";
                    case SessionError.RateLimitExceeded:
                        return "Demasiados intentos seguidos: espera unos segundos.";
                    case SessionError.NotAuthorized:
                        return "No se ha podido entrar en los servicios de Unity.";
                }
            }
            if (e is RequestFailedException)
                return "No se puede conectar con los servicios de Unity. ¿Hay internet? ¿Están activados Relay y Lobby en Unity Cloud?";
            return "No se ha podido conectar: " + e.Message;
        }

        void Empezar(ISession s, Modo modo)
        {
            sesion = s;
            ModoActual = modo;
            Codigo = s.Code;
            hayDesfase = false;
            s.RemovedFromSession += AlPerderSesion;
            s.Deleted += AlPerderSesion;

            var gestor = GestorJugadores.Instancia;
            if (modo == Modo.Anfitrion)
            {
                // Los de este ordenador ya están dentro; los de fuera tendrán los números siguientes.
                if (gestor != null) gestor.PermitirUnirse(false);
                if (!PartidaRapida)
                    try { GUIUtility.systemCopyBuffer = Codigo; } catch { /* sin portapapeles */ }
            }
            else
            {
                if (gestor != null) gestor.ConvertirEnMandos();
                UltimoCodigo = Codigo;
                EnviarHola();
            }
            Debug.Log($"Bicho Show: {(PartidaRapida ? "partida rápida" : "sala")} {Codigo} ({(modo == Modo.Anfitrion ? "anfitrión" : "invitado")}).");
        }

        void AlPerderSesion()
        {
            if (!EnLinea) return;
            sesion = null;
            Limpiar(EsInvitado ? "El anfitrión ha cerrado la sala." : "Se ha cerrado la sala.");
        }

        void AlPararRed(bool eraAnfitrion)
        {
            manejadores = false;
            if (!EnLinea || Ocupado) return;
            // Se ha cortado la conexión sin pasar por Salir().
            var s = sesion;
            sesion = null;
            if (s != null) _ = s.LeaveAsync();
            Limpiar(EsInvitado ? "Se ha perdido la conexión. Si la sala sigue abierta, vuelve a entrar con el mismo código." : "Se ha cerrado la sala.");
        }

        /// <summary>Vuelve a la partida local: fuera marionetas y jugadores de fuera; vuelven los bichos de aquí.</summary>
        void Limpiar(string aviso)
        {
            bool eraInvitado = EsInvitado;
            espejo.Parar();
            espejo.PararInvitado();
            conEscena.Clear();
            pantallas.Clear();
            huerfanos.Clear();
            conRetraso.Clear();
            jugadorDe.Clear();
            carteles.Clear();
            memAvisos.SetLength(0);
            GrabadorPantalla.Grabando = false;
            MandoRemoto.Pausado = false;
            if (eraInvitado)
            {
                Time.timeScale = 1f;
                // Si estábamos mirando una prueba (sin lógica), de vuelta al menú.
                if (SceneManager.GetActiveScene().name != EscenaMenu) SceneManager.LoadScene(EscenaMenu);
            }
            foreach (var m in marionetas.Values) if (m != null) Destroy(m.gameObject);
            marionetas.Clear();
            entradas.Clear();
            ids.Clear();
            escuchados.Clear();

            ModoActual = Modo.Local;
            Codigo = null;
            PartidaRapida = false;
            Aviso = aviso;
            var gestor = GestorJugadores.Instancia;
            if (gestor != null)
            {
                gestor.VolverALocal();
                gestor.PermitirUnirse(true);
            }
        }

        // ---------------------------------------------------------------------
        // Mensajes
        // ---------------------------------------------------------------------

        void AlArrancarRed()
        {
            var mensajes = red.CustomMessagingManager;
            if (mensajes == null || manejadores) return;
            mensajes.RegisterNamedMessageHandler(MensajeMando, RecibirMando);
            mensajes.RegisterNamedMessageHandler(MensajeEstado, RecibirEstado);
            mensajes.RegisterNamedMessageHandler(MensajeEvento, RecibirEvento);
            mensajes.RegisterNamedMessageHandler(MensajeEscena, RecibirEscena);
            mensajes.RegisterNamedMessageHandler(MensajeListo, RecibirListo);
            mensajes.RegisterNamedMessageHandler(MensajeCambios, RecibirCambios);
            mensajes.RegisterNamedMessageHandler(MensajeMovimientos, RecibirMovimientos);
            mensajes.RegisterNamedMessageHandler(MensajePantalla, RecibirPantalla);
            mensajes.RegisterNamedMessageHandler(MensajeAvisos, RecibirAvisos);
            mensajes.RegisterNamedMessageHandler(MensajeHola, RecibirHola);
            mensajes.RegisterNamedMessageHandler(MensajeTiempo, RecibirTiempo);
            manejadores = true;
        }

        void Update()
        {
            if (!EnLinea || red == null || !red.IsListening || !manejadores)
            {
                GrabadorPantalla.Grabando = false;
                return;
            }
            if (EsInvitado) espejo.Actualizar(TiempoDibujo);
            else GrabarPantalla();

            acumulado += Time.unscaledDeltaTime;
            if (acumulado < 1f / EnviosPorSegundo) return;
            acumulado = Mathf.Min(acumulado - 1f / EnviosPorSegundo, 0.1f);

            if (EsAnfitrion)
            {
                VigilarConexiones();
                EnviarEstado();
                VigilarEscena();
                EnviarAvisos();
            }
            else EnviarMando();
        }

        // --- Invitado → anfitrión ---

        void EnviarMando()
        {
            if (MandoRemoto.Todos.Count == 0) return;
            using var w = new FastBufferWriter(256, Allocator.Temp);
            w.WriteValueSafe((byte)MandoRemoto.Todos.Count);
            foreach (var m in MandoRemoto.Todos)
            {
                Vector2 d = m.DireccionMundo;
                w.WriteValueSafe(m.Local);
                w.WriteValueSafe(d.x);
                w.WriteValueSafe(d.y);
                w.WriteValueSafe((byte)(m.AgarrarPulsado ? 1 : 0));
                w.WriteValueSafe(m.Saltos);
                w.WriteValueSafe(m.Lanzamientos);
                w.WriteValueSafe(m.Empezar);
                w.WriteValueSafe((byte)m.Gorro);
                w.WriteValueSafe((byte)m.Complemento);
                w.WriteValueSafe((byte)m.Cuerpo);
                w.WriteValueSafe((byte)m.Especie);
                w.WriteValueSafe(new FixedString32Bytes(NombresJugadores.Guardado(m.Local)));
            }
            red.CustomMessagingManager.SendNamedMessage(MensajeMando, NetworkManager.ServerClientId, w, NetworkDelivery.UnreliableSequenced);
        }

        void RecibirMando(ulong cliente, FastBufferReader r)
        {
            if (!EsAnfitrion || cliente == red.LocalClientId) return;
            if (conEscena.Add(cliente))
            {
                // Recién llegado: que cargue la escena en la que estamos (si el show está en marcha, mira).
                uno.Clear();
                uno.Add(cliente);
                EnviarEscena(uno);
            }
            var gestor = GestorJugadores.Instancia;
            r.ReadValueSafe(out byte n);
            for (int i = 0; i < n; i++)
            {
                r.ReadValueSafe(out byte local);
                r.ReadValueSafe(out float dx);
                r.ReadValueSafe(out float dz);
                r.ReadValueSafe(out byte botones);
                r.ReadValueSafe(out byte saltos);
                r.ReadValueSafe(out byte lanzamientos);
                r.ReadValueSafe(out byte empezar);
                r.ReadValueSafe(out byte gorro);
                r.ReadValueSafe(out byte complemento);
                r.ReadValueSafe(out byte cuerpo);
                r.ReadValueSafe(out byte especie);
                r.ReadValueSafe(out FixedString32Bytes nombre);

                var clave = (cliente, local);
                entradas.TryGetValue(clave, out var entrada);
                if (entrada == null)
                {
                    // Jugador nuevo de fuera: bicho nuevo, si cabe (con el show empezado, mira hasta volver al menú).
                    if (gestor == null || gestor.Jugadores.Count - gestor.Bots.Count >= MaxJugadores) continue;
                    if (ShowBicho.Instancia != null && ShowBicho.Instancia.Estado != ShowBicho.EstadoShow.Inactivo) continue;
                    entrada = gestor.CrearRemoto(cliente, local, gorro, complemento, cuerpo, especie);
                    if (entrada == null) continue;
                    entradas[clave] = entrada;
                }
                entrada.Recibir(new Vector2(dx, dz), (botones & 1) != 0, saltos, lanzamientos, empezar);
                entrada.Nombre = NombresJugadores.Limpiar(nombre.ToString());
                if (entrada.TryGetComponent(out Vestuario v))
                {
                    if (v.Especie != especie) v.PonerEspecie(especie);
                    if (v.Gorro != gorro || v.Complemento != complemento || v.Cuerpo != cuerpo) v.Poner(gorro, complemento, cuerpo);
                }
            }
        }

        // --- Anfitrión → invitados ---

        /// <summary>En el anfitrión: a ese ordenador, el tiempo que ha hecho uno de sus jugadores.</summary>
        public static void EnviarTiempo(ulong cliente, byte local, string carrera, float segundos, string bicho)
        {
            var red = instancia != null ? instancia.red : null;
            if (!EsAnfitrion || red == null || !red.IsListening) return;
            using var w = new FastBufferWriter(96, Allocator.Temp);
            w.WriteValueSafe(local);
            w.WriteValueSafe(new FixedString32Bytes(carrera));
            w.WriteValueSafe(segundos);
            w.WriteValueSafe(new FixedString32Bytes(bicho));
            red.CustomMessagingManager.SendNamedMessage(MensajeTiempo, cliente, w, NetworkDelivery.ReliableSequenced);
        }

        void RecibirTiempo(ulong de, FastBufferReader r)
        {
            if (!EsInvitado) return;
            r.ReadValueSafe(out byte local);
            r.ReadValueSafe(out FixedString32Bytes carrera);
            r.ReadValueSafe(out float segundos);
            r.ReadValueSafe(out FixedString32Bytes bicho);
            RankingTiempos.Apuntar(carrera.ToString(), NombresJugadores.DeLocal(local), segundos, bicho.ToString());
        }

        bool PrepararDestinos()
        {
            destinos.Clear();
            foreach (var id in red.ConnectedClientsIds)
                if (id != red.LocalClientId) destinos.Add(id);
            return destinos.Count > 0;
        }

        byte IdDe(ControladorBicho b)
        {
            if (ids.TryGetValue(b, out byte id)) return id;
            // El siguiente libre (los ids de bichos que ya no están se pueden reutilizar).
            for (int i = 0; i < 256; i++)
            {
                byte candidato = siguienteId++;
                if (!ids.ContainsValue(candidato)) { ids[b] = candidato; return candidato; }
            }
            return 0;
        }

        void EnviarEstado()
        {
            // Bichos que ya no existen: fuera.
            sobranBichos.Clear();
            foreach (var b in ids.Keys) if (b == null) sobranBichos.Add(b);
            foreach (var b in sobranBichos) { ids.Remove(b); escuchados.Remove(b); }

            paraEnviar.Clear();
            var gestor = GestorJugadores.Instancia;
            if (gestor != null) foreach (var b in gestor.Jugadores) if (b != null) paraEnviar.Add(b);
            foreach (var b in ControladorBicho.Todos) if (b != null && !paraEnviar.Contains(b)) paraEnviar.Add(b);
            foreach (var b in paraEnviar) Escuchar(b);

            if (!PrepararDestinos()) return;
            int n = Mathf.Min(paraEnviar.Count, 16);
            using var w = new FastBufferWriter(1300, Allocator.Temp);
            w.WriteValueSafe((float)Time.unscaledTimeAsDouble);
            w.WriteValueSafe((byte)n);
            for (int i = 0; i < n; i++)
            {
                var b = paraEnviar[i];
                var entrada = b.GetComponent<EntradaRemota>();
                byte flags = 0;
                if (b.EnSuelo) flags |= MarionetaBicho.FlagEnSuelo;
                if (b.Tumbado) flags |= MarionetaBicho.FlagTumbado;
                if (b.ManoEstirada) flags |= MarionetaBicho.FlagMano;
                if (b.Retirado) flags |= MarionetaBicho.FlagRetirado;
                if (b.EsBot) flags |= MarionetaBicho.FlagBot;
                if (b.TryGetComponent(out AnimadorBicho a) && a.Celebrando) flags |= MarionetaBicho.FlagCelebrando;
                b.TryGetComponent(out Vestuario v);
                Color32 c = b.ColorJugador;

                w.WriteValueSafe(IdDe(b));
                w.WriteValueSafe((byte)b.IndiceJugador);
                w.WriteValueSafe(entrada != null ? entrada.Cliente : red.LocalClientId);
                w.WriteValueSafe(entrada != null ? entrada.Local : (byte)b.IndiceJugador);
                w.WriteValueSafe(flags);
                w.WriteValueSafe(b.transform.position);
                w.WriteValueSafe(b.transform.rotation);
                w.WriteValueSafe(b.Velocidad);
                w.WriteValueSafe(b.PuntoMano);
                w.WriteValueSafe(c.r);
                w.WriteValueSafe(c.g);
                w.WriteValueSafe(c.b);
                w.WriteValueSafe((byte)(v != null ? v.Gorro : 0));
                w.WriteValueSafe((byte)(v != null ? v.Complemento : 0));
                w.WriteValueSafe((byte)(v != null ? v.Cuerpo : 0));
                w.WriteValueSafe((byte)(v != null ? v.Especie : 0));
            }
            red.CustomMessagingManager.SendNamedMessage(MensajeEstado, destinos, w, NetworkDelivery.UnreliableSequenced);
        }

        readonly List<ControladorBicho> sobranBichos = new List<ControladorBicho>();

        /// <summary>Cuando un bicho del anfitrión hace algo, se avisa a los invitados (sonidos, polvo, estrellas…).</summary>
        void Escuchar(ControladorBicho b)
        {
            if (!escuchados.Add(b)) return;
            b.AlHacer += (bicho, accion) => EnviarEvento(EventoAccion, bicho, (byte)accion, 0f, Vector3.zero);
            b.AlRecibirGolpe += (bicho, fuerza, punto) => EnviarEvento(EventoGolpe, bicho, 0, fuerza, punto);
            b.AlReaparecer += bicho => EnviarEvento(EventoReaparecer, bicho, 0, 0f, Vector3.zero);
        }

        void EnviarEvento(byte tipo, ControladorBicho b, byte dato, float fuerza, Vector3 punto)
        {
            if (!EsAnfitrion || red == null || !red.IsListening || b == null || !PrepararDestinos()) return;
            using var w = new FastBufferWriter(64, Allocator.Temp);
            w.WriteValueSafe(tipo);
            w.WriteValueSafe(IdDe(b));
            w.WriteValueSafe(dato);
            if (tipo == EventoGolpe)
            {
                w.WriteValueSafe(fuerza);
                w.WriteValueSafe(punto);
            }
            red.CustomMessagingManager.SendNamedMessage(MensajeEvento, destinos, w, NetworkDelivery.ReliableSequenced);
        }

        // --- En el invitado ---

        void RecibirEstado(ulong de, FastBufferReader r)
        {
            if (!EsInvitado) return;
            r.ReadValueSafe(out float tiempo);
            r.ReadValueSafe(out byte n);

            // Reloj del anfitrión: se ajusta poco a poco (y de golpe si se ha ido mucho).
            double objetivo = tiempo - Time.unscaledTimeAsDouble;
            if (!hayDesfase || Math.Abs(objetivo - desfase) > 1.0) { desfase = objetivo; hayDesfase = true; }
            else desfase += (objetivo - desfase) * 0.05;

            var gestor = GestorJugadores.Instancia;
            vistos.Clear();
            for (int i = 0; i < n; i++)
            {
                r.ReadValueSafe(out byte id);
                r.ReadValueSafe(out byte indice);
                r.ReadValueSafe(out ulong dueno);
                r.ReadValueSafe(out byte local);
                r.ReadValueSafe(out byte flags);
                r.ReadValueSafe(out Vector3 posicion);
                r.ReadValueSafe(out Quaternion rotacion);
                r.ReadValueSafe(out Vector3 velocidad);
                r.ReadValueSafe(out Vector3 puntoMano);
                r.ReadValueSafe(out byte cr);
                r.ReadValueSafe(out byte cg);
                r.ReadValueSafe(out byte cb);
                r.ReadValueSafe(out byte gorro);
                r.ReadValueSafe(out byte complemento);
                r.ReadValueSafe(out byte cuerpo);
                r.ReadValueSafe(out byte especie);
                vistos.Add(id);

                bool esBot = (flags & MarionetaBicho.FlagBot) != 0;
                marionetas.TryGetValue(id, out var m);
                if (m != null && (m.Indice != indice || m.EsBot != esBot))
                {
                    // El id se ha reutilizado para otro bicho.
                    Destroy(m.gameObject);
                    m = null;
                }
                if (m == null)
                {
                    if (gestor == null) continue;
                    m = gestor.CrearMarioneta(indice, esBot);
                    if (m == null) continue;
                    m.Id = id;
                    m.Indice = indice;
                    m.EsBot = esBot;
                    m.transform.SetPositionAndRotation(posicion, rotacion);
                    marionetas[id] = m;
                }
                m.Dueno = dueno;
                m.Local = local;
                m.Aspecto(new Color32(cr, cg, cb, 255), gorro, complemento, cuerpo, especie);
                m.Recibir(new MarionetaBicho.Foto
                {
                    tiempo = tiempo, posicion = posicion, rotacion = rotacion,
                    velocidad = velocidad, puntoMano = puntoMano, flags = flags,
                });
            }

            // Los que ya no están en el anfitrión.
            sobran.Clear();
            foreach (var par in marionetas) if (!vistos.Contains(par.Key)) sobran.Add(par.Key);
            foreach (var id in sobran)
            {
                if (marionetas[id] != null) Destroy(marionetas[id].gameObject);
                marionetas.Remove(id);
            }
        }

        void RecibirEvento(ulong de, FastBufferReader r)
        {
            if (!EsInvitado) return;
            r.ReadValueSafe(out byte tipo);
            r.ReadValueSafe(out byte id);
            r.ReadValueSafe(out byte dato);
            if (!marionetas.TryGetValue(id, out var m) || m == null) return;
            switch (tipo)
            {
                case EventoAccion:
                    m.Bicho.AccionRed((ControladorBicho.AccionBicho)dato);
                    break;
                case EventoGolpe:
                    r.ReadValueSafe(out float fuerza);
                    r.ReadValueSafe(out Vector3 punto);
                    m.Bicho.GolpeRed(fuerza, punto);
                    break;
                case EventoReaparecer:
                    m.Bicho.ReaparecerRed();
                    break;
            }
        }

        // ---------------------------------------------------------------------
        // El show online: escenas, espejo de la escena, pantalla, sonidos
        // ---------------------------------------------------------------------

        public const byte EfectoPolvo = 1, EfectoImpacto = 2, EfectoCamaraLenta = 3;
        const byte AvisoSonido = 1, AvisoEfecto = 2, AvisoMusica = 3;
        const float PantallasPorSegundo = 12f;

        readonly EspejoEscena espejo = new EspejoEscena();
        readonly HashSet<ulong> conEscena = new HashSet<ulong>();
        readonly List<ulong> uno = new List<ulong>(1);
        readonly System.IO.MemoryStream memAvisos = new System.IO.MemoryStream(1024);
        System.IO.BinaryWriter avisos;
        float siguientePantalla;
        static bool reproduciendoRed;

        class FotogramaPantalla
        {
            public float tiempo;
            public byte[] datos;
            public int ancho, alto;
        }
        readonly List<FotogramaPantalla> pantallas = new List<FotogramaPantalla>();

        static string EscenaMenu => ShowBicho.Instancia != null ? ShowBicho.Instancia.escenaMenu : "Menu";

        void OnEnable() => SceneManager.sceneLoaded += AlCargarEscena;
        void OnDisable() => SceneManager.sceneLoaded -= AlCargarEscena;

        void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (EsAnfitrion)
            {
                // Una prueba del show: los invitados la cargan y la copian. El menú: cada uno el suyo.
                bool show = ShowBicho.Instancia != null && ShowBicho.Instancia.Estado != ShowBicho.EstadoShow.Inactivo &&
                            escena.name != EscenaMenu;
                rondaActual = null;
                if (show) espejo.EmpezarAnfitrion(escena);
                else
                {
                    espejo.Parar();
                    SoltarHuerfanos();
                }
                pantallas.Clear();
                if (red != null && red.IsListening && PrepararDestinos())
                {
                    foreach (var c in destinos) conEscena.Add(c);
                    EnviarEscena(destinos);
                }
            }
            else if (EsInvitado)
            {
                if (espejo.AlCargarEscenaInvitado(escena)) EnviarListo();
            }
        }

        void EnviarEscena(IReadOnlyList<ulong> a)
        {
            var escena = SceneManager.GetActiveScene();
            using var w = new FastBufferWriter(256, Allocator.Temp);
            w.WriteValueSafe(espejo.Numero);
            w.WriteValueSafe(espejo.Activo);
            w.WriteValueSafe(escena.name);
            red.CustomMessagingManager.SendNamedMessage(MensajeEscena, a, w, NetworkDelivery.ReliableSequenced);
        }

        void RecibirEscena(ulong de, FastBufferReader r)
        {
            if (red.IsServer) return;
            r.ReadValueSafe(out int numero);
            r.ReadValueSafe(out bool copiar);
            r.ReadValueSafe(out string nombre);
            pantallas.Clear();
            MandoRemoto.Pausado = false;
            if (Time.timeScale != 1f) Time.timeScale = 1f;
            espejo.CambiarEscenaInvitado(numero, nombre, copiar);
        }

        void EnviarListo()
        {
            using var w = new FastBufferWriter(16, Allocator.Temp);
            w.WriteValueSafe(espejo.Numero);
            red.CustomMessagingManager.SendNamedMessage(MensajeListo, NetworkManager.ServerClientId, w, NetworkDelivery.ReliableSequenced);
        }

        void RecibirListo(ulong cliente, FastBufferReader r)
        {
            if (!EsAnfitrion) return;
            r.ReadValueSafe(out int numero);
            var datos = espejo.Listo(cliente, numero, out int largo);
            if (datos == null) return;
            uno.Clear();
            uno.Add(cliente);
            EnviarBytes(MensajeCambios, uno, espejo.Numero, datos, largo, NetworkDelivery.ReliableFragmentedSequenced);
        }

        /// <summary>Un bloque de bytes con el número de escena delante.</summary>
        void EnviarBytes(string mensaje, IReadOnlyList<ulong> a, int numero, byte[] datos, int largo, NetworkDelivery entrega)
        {
            if (a.Count == 0 || largo <= 0) return;
            using var w = new FastBufferWriter(largo + 16, Allocator.Temp);
            w.WriteValueSafe(numero);
            w.WriteValueSafe(largo);
            w.WriteBytesSafe(datos, largo);
            red.CustomMessagingManager.SendNamedMessage(mensaje, a, w, entrega);
        }

        static byte[] LeerBytes(FastBufferReader r, out int numero, out int largo)
        {
            r.ReadValueSafe(out numero);
            r.ReadValueSafe(out largo);
            var datos = new byte[Mathf.Max(0, largo)];
            if (largo > 0) r.ReadBytesSafe(ref datos, largo);
            return datos;
        }

        void RecibirCambios(ulong de, FastBufferReader r)
        {
            if (red.IsServer) return;
            var datos = LeerBytes(r, out int numero, out int largo);
            if (numero == espejo.Numero) espejo.RecibirCambios(datos, largo);
        }

        void RecibirMovimientos(ulong de, FastBufferReader r)
        {
            if (red.IsServer) return;
            var datos = LeerBytes(r, out _, out int largo);
            espejo.RecibirMovimientos(datos, largo); // el número de escena va dentro
        }

        /// <summary>En el anfitrión, cada vuelta: lo que ha cambiado en la escena.</summary>
        void VigilarEscena()
        {
            if (!espejo.Activo) return;
            espejo.Vigilar((float)Time.unscaledTimeAsDouble, out var datos, out int largo, EnviarTrozoMovimientos);
            if (largo > 0 && espejo.HayListos)
                EnviarBytes(MensajeCambios, espejo.Listos, espejo.Numero, datos, largo, NetworkDelivery.ReliableFragmentedSequenced);
        }

        void EnviarTrozoMovimientos(byte[] datos, int largo)
        {
            if (espejo.HayListos) EnviarBytes(MensajeMovimientos, espejo.Listos, espejo.Numero, datos, largo, NetworkDelivery.UnreliableSequenced);
        }

        // --- La pantalla del anfitrión ---

        /// <summary>
        /// En el anfitrión, cada fotograma: manda lo que se dibujó en el anterior (unas 12 veces por
        /// segundo) y empieza a grabar el siguiente.
        /// </summary>
        void GrabarPantalla()
        {
            bool grabar = espejo.Activo && espejo.HayListos;
            float ahora = Time.unscaledTime;
            if (GrabadorPantalla.Grabando && grabar && ahora >= siguientePantalla && GrabadorPantalla.Longitud > 0)
            {
                siguientePantalla = ahora + 1f / PantallasPorSegundo;
                int largo = GrabadorPantalla.Longitud;
                using var w = new FastBufferWriter(largo + 32, Allocator.Temp);
                w.WriteValueSafe(espejo.Numero);
                w.WriteValueSafe((float)Time.unscaledTimeAsDouble);
                w.WriteValueSafe((short)Screen.width);
                w.WriteValueSafe((short)Screen.height);
                w.WriteValueSafe(largo);
                w.WriteBytesSafe(GrabadorPantalla.Buffer, largo);
                red.CustomMessagingManager.SendNamedMessage(MensajePantalla, espejo.Listos, w, NetworkDelivery.ReliableFragmentedSequenced);
            }
            GrabadorPantalla.Grabando = grabar;
            GrabadorPantalla.EmpezarFotograma();
        }

        void RecibirPantalla(ulong de, FastBufferReader r)
        {
            if (red.IsServer) return;
            r.ReadValueSafe(out int numero);
            r.ReadValueSafe(out float tiempo);
            r.ReadValueSafe(out short ancho);
            r.ReadValueSafe(out short alto);
            r.ReadValueSafe(out int largo);
            if (numero != espejo.Numero || largo <= 0) return;
            var datos = new byte[largo];
            r.ReadBytesSafe(ref datos, largo);
            pantallas.Add(new FotogramaPantalla { tiempo = tiempo, datos = datos, ancho = Mathf.Max(1, ancho), alto = Mathf.Max(1, alto) });
            if (pantallas.Count > 12) pantallas.RemoveAt(0);
        }

        void OnGUI()
        {
            if (EsAnfitrion) DibujarCarteles();
            if (!EsInvitado || !espejo.Activo || pantallas.Count == 0) return;
            // El fotograma que toca: el último que no se adelante a lo que se ve (los bichos van 0,1 s en el pasado).
            float t = (float)TiempoDibujo;
            FotogramaPantalla elegido = pantallas[0];
            foreach (var p in pantallas) if (p.tiempo <= t) elegido = p;
            if (t - elegido.tiempo > 3f) return; // el anfitrión ha dejado de mandar
            GrabadorPantalla.Reproducir(elegido.datos, elegido.datos.Length,
                                        Screen.width / (float)elegido.ancho, Screen.height / (float)elegido.alto);
        }

        // --- Sonidos, efectos y música ---

        /// <summary>
        /// Lo llama Sonidos.Reproducir. Devuelve si se debe oír aquí. En el anfitrión se apunta para
        /// los invitados; en un invitado solo suena lo que llega del anfitrión (y los ruiditos de su menú).
        /// </summary>
        public static bool SonidoLocal(Sonido sonido, Vector3? posicion, float volumen, float tono, float variacion)
        {
            bool deMenu = sonido == Sonido.MenuMover || sonido == Sonido.MenuAceptar;
            if (ModoActual == Modo.Local || instancia == null) return true;
            if (EsAnfitrion)
            {
                if (!deMenu)
                {
                    var w = instancia.Avisos();
                    w.Write(AvisoSonido);
                    w.Write((byte)sonido);
                    w.Write(posicion.HasValue);
                    if (posicion.HasValue) { w.Write(posicion.Value.x); w.Write(posicion.Value.y); w.Write(posicion.Value.z); }
                    w.Write(Mathf.FloatToHalf(volumen));
                    w.Write(Mathf.FloatToHalf(tono));
                    w.Write(Mathf.FloatToHalf(variacion));
                }
                return true;
            }
            return reproduciendoRed || deMenu;
        }

        /// <summary>Lo llaman las partículas y la cámara lenta de Efectos (igual que SonidoLocal).</summary>
        public static bool EfectoLocal(byte tipo, Vector3 punto, Color color, float a, float b)
        {
            if (ModoActual == Modo.Local || instancia == null) return true;
            if (EsAnfitrion)
            {
                var w = instancia.Avisos();
                w.Write(AvisoEfecto);
                w.Write(tipo);
                w.Write(punto.x); w.Write(punto.y); w.Write(punto.z);
                Color32 c = color;
                w.Write(c.r); w.Write(c.g); w.Write(c.b); w.Write(c.a);
                w.Write(a);
                w.Write(b);
                return true;
            }
            return reproduciendoRed;
        }

        /// <summary>Lo llama Musica.Poner: en el anfitrión, los invitados ponen la misma.</summary>
        public static void AnotarMusica(Musica.Tema tema)
        {
            if (!EsAnfitrion || instancia == null) return;
            var w = instancia.Avisos();
            w.Write(AvisoMusica);
            w.Write((byte)tema);
        }

        System.IO.BinaryWriter Avisos()
        {
            if (avisos == null) avisos = new System.IO.BinaryWriter(memAvisos);
            return avisos;
        }

        void EnviarAvisos()
        {
            if (avisos == null) return;
            avisos.Flush();
            int largo = (int)memAvisos.Length;
            if (largo > 0 && PrepararDestinos())
                EnviarBytes(MensajeAvisos, destinos, espejo.Numero, memAvisos.GetBuffer(), largo, NetworkDelivery.ReliableFragmentedSequenced);
            memAvisos.SetLength(0);
        }

        void RecibirAvisos(ulong de, FastBufferReader r)
        {
            if (red.IsServer) return;
            var datos = LeerBytes(r, out _, out int largo);
            reproduciendoRed = true;
            using var l = new System.IO.BinaryReader(new System.IO.MemoryStream(datos, 0, largo, false));
            try
            {
                while (l.BaseStream.Position < largo)
                {
                    byte tipo = l.ReadByte();
                    switch (tipo)
                    {
                        case AvisoSonido:
                        {
                            var sonido = (Sonido)l.ReadByte();
                            Vector3? pos = null;
                            if (l.ReadBoolean()) pos = new Vector3(l.ReadSingle(), l.ReadSingle(), l.ReadSingle());
                            float volumen = Mathf.HalfToFloat(l.ReadUInt16());
                            float tono = Mathf.HalfToFloat(l.ReadUInt16());
                            float variacion = Mathf.HalfToFloat(l.ReadUInt16());
                            Sonidos.Reproducir(sonido, pos, volumen, tono, variacion);
                            break;
                        }
                        case AvisoEfecto:
                        {
                            byte efecto = l.ReadByte();
                            var punto = new Vector3(l.ReadSingle(), l.ReadSingle(), l.ReadSingle());
                            Color color = new Color32(l.ReadByte(), l.ReadByte(), l.ReadByte(), l.ReadByte());
                            float a = l.ReadSingle(), b = l.ReadSingle();
                            if (efecto == EfectoPolvo) Efectos.Polvo(punto, a);
                            else if (efecto == EfectoImpacto) Efectos.Impacto(punto, color, a);
                            else if (efecto == EfectoCamaraLenta) Efectos.CamaraLenta(a, b, 0f);
                            break;
                        }
                        case AvisoMusica:
                            Musica.Poner((Musica.Tema)l.ReadByte());
                            break;
                        default:
                            return;
                    }
                }
            }
            catch (System.IO.EndOfStreamException) { }
            finally
            {
                reproduciendoRed = false;
            }
        }

        // ---------------------------------------------------------------------
        // Conexiones que se cortan
        // ---------------------------------------------------------------------

        /// <summary>Si a un jugador de fuera no le llega el mando este rato, un bot le lleva el bicho.</summary>
        const float SinMandoParaSuplente = 1.5f;

        readonly Dictionary<ulong, string> jugadorDe = new Dictionary<ulong, string>();
        readonly List<EntradaRemota> huerfanos = new List<EntradaRemota>();
        readonly HashSet<EntradaRemota> conRetraso = new HashSet<EntradaRemota>();
        readonly List<(string texto, Color color, float hasta)> carteles = new List<(string, Color, float)>();
        RondaBase rondaActual;

        /// <summary>Código de la última sala (para volver a entrar si se corta).</summary>
        public static string UltimoCodigo { get; private set; }

        static bool ShowEnMarcha => ShowBicho.Instancia != null && ShowBicho.Instancia.Estado != ShowBicho.EstadoShow.Inactivo;

        static string NombreDe(EntradaRemota e) =>
            e != null && e.TryGetComponent(out ControladorBicho b) ? $"J{b.IndiceJugador + 1}" : "Un jugador";

        /// <summary>Un cartel arriba, en la pantalla del anfitrión (y, en el show, en la de todos).</summary>
        void Cartel(string texto, Color color) => carteles.Add((texto, color, Time.unscaledTime + 4f));

        /// <summary>El invitado se presenta con su id de jugador: si ya estuvo en este show, recupera su bicho.</summary>
        void EnviarHola()
        {
            string id;
            try { id = AuthenticationService.Instance.PlayerId ?? ""; } catch { id = ""; }
            using var w = new FastBufferWriter(256, Allocator.Temp);
            w.WriteValueSafe(id);
            red.CustomMessagingManager.SendNamedMessage(MensajeHola, NetworkManager.ServerClientId, w, NetworkDelivery.ReliableSequenced);
        }

        void RecibirHola(ulong cliente, FastBufferReader r)
        {
            if (!EsAnfitrion) return;
            r.ReadValueSafe(out string id);
            if (string.IsNullOrEmpty(id)) return;
            jugadorDe[cliente] = id;
            foreach (var par in entradas) if (par.Key.Item1 == cliente && par.Value != null) par.Value.Jugador = id;

            // ¿Vuelve alguien que se fue a mitad del show?
            for (int i = huerfanos.Count - 1; i >= 0; i--)
            {
                var e = huerfanos[i];
                if (e == null) { huerfanos.RemoveAt(i); continue; }
                if (e.Jugador != id) continue;
                huerfanos.RemoveAt(i);
                e.Cliente = cliente;
                e.Desconectado = false;
                entradas[(cliente, e.Local)] = e;
                Cartel($"¡{NombreDe(e)} ha vuelto!", EstiloShow.Verde);
                Sonidos.Reproducir(Sonido.Aparecer);
            }
        }

        /// <summary>
        /// Un invitado se ha ido de la sala. En el menú, sus bichos se van con él; con el show en
        /// marcha se quedan, llevados por un bot, por si vuelve con el mismo código.
        /// </summary>
        void AlIrseCliente(ulong cliente)
        {
            if (!EsAnfitrion || red == null || !red.IsServer) return;
            espejo.Quitar(cliente);
            conEscena.Remove(cliente);
            jugadorDe.TryGetValue(cliente, out string id);
            jugadorDe.Remove(cliente);

            var gestor = GestorJugadores.Instancia;
            var claves = new List<(ulong, byte)>(entradas.Keys);
            foreach (var clave in claves)
            {
                if (clave.Item1 != cliente) continue;
                var e = entradas[clave];
                entradas.Remove(clave);
                if (e == null) continue;
                if (ShowEnMarcha && !string.IsNullOrEmpty(e.Jugador))
                {
                    e.Desconectado = true;
                    conRetraso.Remove(e);
                    huerfanos.Add(e);
                    Cartel($"{NombreDe(e)} se ha desconectado: juega un bot por él hasta que vuelva", EstiloShow.Rosa);
                }
                else
                {
                    if (ShowEnMarcha) Cartel($"{NombreDe(e)} se ha ido del show", EstiloShow.Rosa);
                    if (gestor != null) gestor.QuitarRemoto(e.GetComponent<ControladorBicho>());
                }
            }

            // Que deje su sitio libre en la sala (si no, al volver podría estar "llena").
            if (!string.IsNullOrEmpty(id) && sesion != null && sesion.IsHost) _ = QuitarDeLaSala(id);
        }

        async Task QuitarDeLaSala(string id)
        {
            try { await sesion.AsHost().RemovePlayerAsync(id); }
            catch (Exception e) { Debug.Log($"Bicho Show: no se ha podido sacar de la sala a {id} ({e.Message})."); }
        }

        /// <summary>
        /// En el anfitrión, cada vuelta: a quien no le llega el mando (red lenta o cortada) le
        /// lleva el bicho un bot de la prueba hasta que vuelva.
        /// </summary>
        void VigilarConexiones()
        {
            if (!ShowEnMarcha) return;
            if (rondaActual == null) rondaActual = FindAnyObjectByType<RondaBase>();
            bool jugando = rondaActual != null && (rondaActual.EstadoActual == RondaBase.Estado.Jugando ||
                                                   rondaActual.EstadoActual == RondaBase.Estado.CuentaAtras);
            foreach (var e in EntradaRemota.Todas)
            {
                if (e == null) continue;
                if (e.Activo)
                {
                    if (conRetraso.Remove(e)) Cartel($"{NombreDe(e)} vuelve a tener conexión", EstiloShow.Verde);
                    continue;
                }
                if (e.SinMando < SinMandoParaSuplente || !jugando) continue;
                if (e.Suplente == null || (e.Suplente as Behaviour) == null)
                    e.PonerSuplente(rondaActual.CrearSuplente(e.gameObject));
                if (!e.Desconectado && conRetraso.Add(e))
                    Cartel($"A {NombreDe(e)} no le llega la conexión: le echa una mano un bot", EstiloShow.Oro);
            }
        }

        /// <summary>De vuelta al menú: los que se fueron y no han vuelto ya no tienen bicho.</summary>
        void SoltarHuerfanos()
        {
            var gestor = GestorJugadores.Instancia;
            foreach (var e in huerfanos)
                if (e != null && gestor != null) gestor.QuitarRemoto(e.GetComponent<ControladorBicho>());
            huerfanos.Clear();
            conRetraso.Clear();
        }

        void DibujarCarteles()
        {
            carteles.RemoveAll(c => Time.unscaledTime > c.hasta);
            float y = EstiloShow.Px(150f);
            foreach (var c in carteles)
            {
                float queda = c.hasta - Time.unscaledTime;
                var color = c.color.WithAlpha(Mathf.Clamp01(queda * 2f));
                EstiloShow.TextoEnPastilla(new Vector2(Screen.width * 0.5f, y), c.texto, Mathf.RoundToInt(EstiloShow.Px(30f)), Color.white.WithAlpha(color.a),
                                           Color.Lerp(color, EstiloShow.MoradoOscuro, 0.35f).WithAlpha(0.92f * color.a));
                y += EstiloShow.Px(64f);
            }
        }

        /// <summary>¿Esta marioneta es de un jugador de este ordenador?</summary>
        public static bool EsMia(MarionetaBicho m) =>
            instancia != null && instancia.red != null && m != null && !m.EsBot && m.Dueno == instancia.red.LocalClientId;

        void OnDestroy()
        {
            if (instancia == this) instancia = null;
        }
    }
}
