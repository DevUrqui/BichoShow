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

        static async Task IniciarServicios()
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
                try { GUIUtility.systemCopyBuffer = Codigo; } catch { /* sin portapapeles */ }
            }
            else if (gestor != null)
            {
                gestor.ConvertirEnMandos();
            }
            Debug.Log($"Bicho Show: sala {Codigo} ({(modo == Modo.Anfitrion ? "anfitrión" : "invitado")}).");
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
            Limpiar(EsInvitado ? "Se ha perdido la conexión con el anfitrión." : "Se ha cerrado la sala.");
        }

        /// <summary>Vuelve a la partida local: fuera marionetas y jugadores de fuera; vuelven los bichos de aquí.</summary>
        void Limpiar(string aviso)
        {
            foreach (var m in marionetas.Values) if (m != null) Destroy(m.gameObject);
            marionetas.Clear();
            entradas.Clear();
            ids.Clear();
            escuchados.Clear();

            ModoActual = Modo.Local;
            Codigo = null;
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
            manejadores = true;
        }

        void AlIrseCliente(ulong cliente)
        {
            if (!EsAnfitrion || red == null || !red.IsServer) return;
            // Se van sus bichos.
            var gestor = GestorJugadores.Instancia;
            var claves = new List<(ulong, byte)>(entradas.Keys);
            foreach (var clave in claves)
            {
                if (clave.Item1 != cliente) continue;
                var e = entradas[clave];
                entradas.Remove(clave);
                if (e != null && gestor != null) gestor.QuitarRemoto(e.GetComponent<ControladorBicho>());
            }
        }

        void Update()
        {
            if (!EnLinea || red == null || !red.IsListening || !manejadores) return;
            acumulado += Time.unscaledDeltaTime;
            if (acumulado < 1f / EnviosPorSegundo) return;
            acumulado = Mathf.Min(acumulado - 1f / EnviosPorSegundo, 0.1f);

            if (EsAnfitrion) EnviarEstado();
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
                w.WriteValueSafe((byte)m.Gorro);
                w.WriteValueSafe((byte)m.Complemento);
            }
            red.CustomMessagingManager.SendNamedMessage(MensajeMando, NetworkManager.ServerClientId, w, NetworkDelivery.UnreliableSequenced);
        }

        void RecibirMando(ulong cliente, FastBufferReader r)
        {
            if (!EsAnfitrion || cliente == red.LocalClientId) return;
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
                r.ReadValueSafe(out byte gorro);
                r.ReadValueSafe(out byte complemento);

                var clave = (cliente, local);
                entradas.TryGetValue(clave, out var entrada);
                if (entrada == null)
                {
                    // Jugador nuevo de fuera: bicho nuevo, si cabe.
                    if (gestor == null || gestor.Jugadores.Count - gestor.Bots.Count >= MaxJugadores) continue;
                    entrada = gestor.CrearRemoto(cliente, local, gorro, complemento);
                    if (entrada == null) continue;
                    entradas[clave] = entrada;
                }
                entrada.Recibir(new Vector2(dx, dz), (botones & 1) != 0, saltos, lanzamientos);
                if (entrada.TryGetComponent(out Vestuario v) && (v.Gorro != gorro || v.Complemento != complemento))
                    v.Poner(gorro, complemento);
            }
        }

        // --- Anfitrión → invitados ---

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
                m.Aspecto(new Color32(cr, cg, cb, 255), gorro, complemento);
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

        /// <summary>¿Esta marioneta es de un jugador de este ordenador?</summary>
        public static bool EsMia(MarionetaBicho m) =>
            instancia != null && instancia.red != null && m != null && !m.EsBot && m.Dueno == instancia.red.LocalClientId;

        void OnDestroy()
        {
            if (instancia == this) instancia = null;
        }
    }
}
