using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Va junto al PlayerInputManager. Cada vez que alguien pulsa un botón en un mando
    /// (o en el teclado) nuevo, el PlayerInputManager crea un bicho; aquí lo colocamos
    /// en su punto de aparición y le damos su color.
    ///
    /// En el menú es "persistente": los jugadores sobreviven a los cambios de escena. Cada
    /// prueba tiene su propio GestorJugadores (para poder jugarla suelta); si al cargarla ya
    /// existe uno persistente, este le pasa sus puntos de aparición y colores y se retira.
    /// </summary>
    [DefaultExecutionOrder(-1000)] // antes que el PlayerInputManager, para retirarse sin que se active
    [RequireComponent(typeof(PlayerInputManager))]
    public class GestorJugadores : MonoBehaviour
    {
        /// <summary>El gestor activo (el persistente, si venimos del menú).</summary>
        public static GestorJugadores Instancia { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos() => Instancia = null;

        [Tooltip("Los jugadores (y este gestor) sobreviven a los cambios de escena. Activado en el menú.")]
        public bool persistente;

        [Tooltip("Un punto por jugador. Si hay menos puntos que jugadores, se reutilizan.")]
        public Transform[] puntosAparicion;

        [Tooltip("Color de cada jugador, en orden de llegada.")]
        public Color[] colores =
        {
            new Color(1.00f, 0.42f, 0.42f), // rojo coral
            new Color(0.35f, 0.65f, 1.00f), // azul
            new Color(1.00f, 0.85f, 0.25f), // amarillo
            new Color(0.45f, 0.85f, 0.45f), // verde
        };

        [Tooltip("Muestra en pantalla quién está jugando y cómo unirse.")]
        public bool mostrarAyuda = true;

        readonly List<ControladorBicho> jugadores = new List<ControladorBicho>();
        public IReadOnlyList<ControladorBicho> Jugadores => jugadores;

        PlayerInputManager manager;
        GUIStyle estiloAyuda;

        void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                // Venimos del menú con jugadores: que el gestor persistente use esta escena.
                Instancia.AdoptarEscena(this);
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            Instancia = this;
            manager = GetComponent<PlayerInputManager>();
            // Necesario para que funcionen los eventos onPlayerJoined / onPlayerLeft de C#.
            manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            if (persistente) DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        /// <summary>Toma los puntos de aparición y colores de la escena nueva y recoloca a los jugadores.</summary>
        void AdoptarEscena(GestorJugadores deLaEscena)
        {
            // Los puntos de aparición son hijos del gestor de la escena, que se va a borrar:
            // se sacan antes para que sigan existiendo (se quedan en su escena y se van con ella).
            if (deLaEscena.puntosAparicion != null)
                foreach (var punto in deLaEscena.puntosAparicion)
                    if (punto != null && punto.IsChildOf(deLaEscena.transform)) punto.SetParent(null, true);

            puntosAparicion = deLaEscena.puntosAparicion;
            if (deLaEscena.colores != null && deLaEscena.colores.Length > 0) colores = deLaEscena.colores;
            mostrarAyuda = deLaEscena.mostrarAyuda;

            jugadores.RemoveAll(j => j == null);
            bots.Clear(); // se destruyeron con la escena anterior
            foreach (var j in jugadores)
            {
                j.Retirar(false);
                Recolocar(j, j.IndiceJugador);
            }
        }

        /// <summary>
        /// Antes de cambiar de escena: quita bots y roles de la ronda anterior (saboteadores…)
        /// para que los jugadores lleguen limpios a la siguiente.
        /// </summary>
        public void PrepararCambioEscena()
        {
            EliminarBots();
            jugadores.RemoveAll(j => j == null);
            foreach (var j in jugadores)
            {
                foreach (var s in j.GetComponents<Saboteador>()) Destroy(s);
                // El bot que suplía a un jugador de fuera era de la prueba que se acaba.
                if (j.TryGetComponent(out EntradaRemota remota)) remota.QuitarSuplente();
                j.Retirar(false);
            }
        }

        /// <summary>Punto de aparición de ese número de jugador en la escena actual (puede ser null).</summary>
        public Transform PuntoAparicion(int indice) =>
            puntosAparicion != null && puntosAparicion.Length > 0 ? puntosAparicion[indice % puntosAparicion.Length] : null;

        public Color ColorDe(int indice) =>
            colores != null && colores.Length > 0 ? colores[indice % colores.Length] : Color.white;

        void OnEnable()
        {
            if (manager == null) return;
            manager.onPlayerJoined += AlUnirse;
            manager.onPlayerLeft += AlSalir;
        }

        void OnDisable()
        {
            if (manager == null) return;
            manager.onPlayerJoined -= AlUnirse;
            manager.onPlayerLeft -= AlSalir;
        }

        void AlUnirse(PlayerInput entrada)
        {
            var bicho = entrada.GetComponent<ControladorBicho>();
            if (bicho == null)
            {
                Debug.LogError("El prefab del jugador no tiene ControladorBicho.", entrada);
                return;
            }

            int indice = entrada.playerIndex;
            if (persistente) DontDestroyOnLoad(entrada.gameObject);
            Vestuario.PonerGuardado(bicho);
            // Invitado de una partida online: el bicho lo mueve el anfitrión; aquí solo se lee el mando.
            if (RedShow.EsInvitado)
            {
                bicho.gameObject.name = $"Mando J{indice + 1}";
                bicho.gameObject.AddComponent<MandoRemoto>();
                return;
            }
            Preparar(bicho, indice, $"J{indice + 1} ({entrada.currentControlScheme})");
        }

        void AlSalir(PlayerInput entrada)
        {
            var bicho = entrada.GetComponent<ControladorBicho>();
            if (bicho != null) jugadores.Remove(bicho);
        }

        void Preparar(ControladorBicho bicho, int indice, string nombre)
        {
            Recolocar(bicho, indice);
            bicho.gameObject.name = nombre;
            jugadores.Add(bicho);
        }

        void Recolocar(ControladorBicho bicho, int indice)
        {
            Transform punto = puntosAparicion != null && puntosAparicion.Length > 0
                ? puntosAparicion[indice % puntosAparicion.Length]
                : null;

            if (punto != null)
            {
                bicho.Colocar(punto.position, punto.rotation);
            }
            else
            {
                // Sin punto válido: no cortar (antes esto dejaba la ronda sin bots); colocarlo en fila.
                Debug.LogWarning($"Bicho Show: no hay punto de aparición para el jugador {indice + 1}.", this);
                bicho.Colocar(new Vector3(indice * 2f - 3f, 1f, 0f), Quaternion.identity);
            }

            if (colores != null && colores.Length > 0)
                bicho.AsignarColor(colores[indice % colores.Length]);
        }

        // ---------------------------------------------------------------------
        // Bots
        // ---------------------------------------------------------------------

        readonly List<ControladorBicho> bots = new List<ControladorBicho>();
        public IReadOnlyList<ControladorBicho> Bots => bots;

        /// <summary>Jugadores con mando o teclado (en este ordenador o, en una partida online, en otro).</summary>
        public int NumeroHumanos => PlayerInput.all.Count + remotos.Count;

        /// <summary>
        /// Crea un bicho controlado por la máquina con el componente T (que debe implementar
        /// IEntradaBicho). Usa el mismo prefab que los jugadores, pero sin PlayerInput, para
        /// que no se quede con ningún mando ni cuente como jugador del PlayerInputManager.
        /// </summary>
        public T CrearBot<T>(int indice) where T : Component, IEntradaBicho
        {
            var go = InstanciarSinMando(out T cerebro);
            if (go == null) return null;

            var bicho = go.GetComponent<ControladorBicho>();
            bicho.AsignarIndice(indice);
            Preparar(bicho, indice, $"Bot {indice + 1}");
            Vestuario.PonerAlAzar(bicho);
            bots.Add(bicho);
            return cerebro;
        }

        /// <summary>
        /// Instancia el prefab de los jugadores sin PlayerInput (para que no se quede con ningún
        /// mando ni cuente como jugador del PlayerInputManager) y con el componente T.
        /// </summary>
        GameObject InstanciarSinMando<T>(out T componente) where T : Component
        {
            componente = null;
            var prefab = manager != null ? manager.playerPrefab : null;
            if (prefab == null) return null;

            // Se crea dentro de un objeto desactivado para quitar el PlayerInput antes de que se active.
            var contenedor = new GameObject("CreandoBicho");
            contenedor.SetActive(false);
            var go = Instantiate(prefab, contenedor.transform);
            var entrada = go.GetComponent<PlayerInput>();
            if (entrada != null) DestroyImmediate(entrada);
            componente = go.AddComponent<T>();
            go.transform.SetParent(null, false); // aquí se activa
            Destroy(contenedor);
            return go;
        }

        // ---------------------------------------------------------------------
        // Partida online
        // ---------------------------------------------------------------------

        readonly List<ControladorBicho> remotos = new List<ControladorBicho>();
        /// <summary>En el anfitrión: los bichos de los jugadores que juegan desde otro ordenador.</summary>
        public IReadOnlyList<ControladorBicho> Remotos => remotos;

        /// <summary>Número de jugador más bajo que no usa nadie (ni los de aquí ni los de fuera).</summary>
        public int IndiceLibre()
        {
            jugadores.RemoveAll(j => j == null);
            for (int i = 0; ; i++)
            {
                bool usado = false;
                foreach (var j in jugadores) if (j.IndiceJugador == i) { usado = true; break; }
                if (!usado) foreach (var p in PlayerInput.all) if (p.playerIndex == i) { usado = true; break; }
                if (!usado) return i;
            }
        }

        /// <summary>En el anfitrión: crea el bicho de un jugador que juega desde otro ordenador.</summary>
        public EntradaRemota CrearRemoto(ulong cliente, byte local, int gorro, int complemento, int cuerpo)
        {
            int indice = IndiceLibre();
            var go = InstanciarSinMando(out EntradaRemota entrada);
            if (go == null) return null;
            entrada.Cliente = cliente;
            entrada.Local = local;
            if (persistente) DontDestroyOnLoad(go);

            var bicho = go.GetComponent<ControladorBicho>();
            bicho.AsignarIndice(indice);
            Preparar(bicho, indice, $"J{indice + 1} (online)");
            if (go.TryGetComponent(out Vestuario v)) v.Poner(gorro, complemento, cuerpo);
            remotos.Add(bicho);
            if (go.TryGetComponent(out AnimadorBicho a)) a.Celebrar(2.5f); // ¡hola!
            Sonidos.Reproducir(Sonido.Aparecer, bicho.transform.position);
            return entrada;
        }

        public void QuitarRemoto(ControladorBicho bicho)
        {
            if (bicho == null) return;
            remotos.Remove(bicho);
            jugadores.Remove(bicho);
            Destroy(bicho.gameObject);
        }

        /// <summary>En un invitado: crea la copia de un bicho del anfitrión.</summary>
        public MarionetaBicho CrearMarioneta(int indice, bool esBot)
        {
            var go = InstanciarSinMando(out MarionetaBicho marioneta);
            if (go == null) return null;
            DontDestroyOnLoad(go);
            var bicho = go.GetComponent<ControladorBicho>();
            bicho.AsignarIndice(indice);
            bicho.HacerMarioneta(esBot);
            go.name = esBot ? $"Bot {indice + 1} (red)" : $"J{indice + 1} (red)";
            return marioneta;
        }

        /// <summary>Al entrar como invitado: los jugadores de aquí pasan a ser solo mandos.</summary>
        public void ConvertirEnMandos()
        {
            jugadores.RemoveAll(j => j == null);
            foreach (var j in jugadores.ToArray())
            {
                if (!j.TryGetComponent(out PlayerInput _)) continue;
                jugadores.Remove(j);
                if (!j.TryGetComponent(out MandoRemoto _)) j.gameObject.AddComponent<MandoRemoto>();
            }
        }

        /// <summary>Al salir de la partida online: vuelven los bichos de aquí y se van los de fuera.</summary>
        public void VolverALocal()
        {
            foreach (var m in MandoRemoto.Todos.ToArray())
            {
                if (m == null) continue;
                var bicho = m.GetComponent<ControladorBicho>();
                var entrada = m.GetComponent<PlayerInput>();
                DestroyImmediate(m); // vuelve a ser visible y a tener física
                if (bicho != null && entrada != null)
                    Preparar(bicho, entrada.playerIndex, $"J{entrada.playerIndex + 1} ({entrada.currentControlScheme})");
            }
            foreach (var r in remotos.ToArray()) QuitarRemoto(r);
            remotos.Clear();
        }

        public void EliminarBots()
        {
            foreach (var bot in bots)
            {
                if (bot == null) continue;
                jugadores.Remove(bot);
                Destroy(bot.gameObject);
            }
            bots.Clear();
        }

        /// <summary>Abre o cierra la entrada de jugadores nuevos (se cierra durante la ronda con bots).</summary>
        public void PermitirUnirse(bool permitir)
        {
            if (permitir) manager.EnableJoining();
            else manager.DisableJoining();
        }

        void OnGUI()
        {
            if (!mostrarAyuda || RedShow.EsInvitado) return; // el invitado ve la pantalla del anfitrión

            if (estiloAyuda == null)
            {
                estiloAyuda = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                estiloAyuda.normal.textColor = Color.white;
            }

            float y = 12f;
            bool hayHueco = manager.maxPlayerCount < 0 || jugadores.Count < manager.maxPlayerCount;
            if (manager.joiningEnabled && hayHueco)
                Etiqueta(ref y, "Pulsa cualquier botón del mando (o una tecla) para unirte", Color.white);

            foreach (var bicho in jugadores)
            {
                if (bicho == null) continue;
                string estado = bicho.Tumbado ? "¡al suelo!"
                    : bicho.TeTienenAgarrado ? "¡atrapado!"
                    : bicho.Agarrando ? "agarrando" : "";
                Etiqueta(ref y, $"{bicho.name}  {estado}", bicho.ColorJugador);
            }

            Etiqueta(ref y, "Mando: stick mover · A saltar · gatillos agarrar · X lanzarse (o lanzar lo agarrado)", new Color(1f, 1f, 1f, 0.7f));
            Etiqueta(ref y, "Teclado: WASD mover · Espacio saltar · E agarrar · Q lanzarse (o lanzar lo agarrado)", new Color(1f, 1f, 1f, 0.7f));
        }

        void Etiqueta(ref float y, string texto, Color color)
        {
            var rect = new Rect(16f, y, 900f, 32f);
            estiloAyuda.normal.textColor = new Color(0f, 0f, 0f, 0.6f * color.a);
            EstiloShow.Etiqueta(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), texto, estiloAyuda);
            estiloAyuda.normal.textColor = color;
            EstiloShow.Etiqueta(rect, texto, estiloAyuda);
            y += 30f;
        }
    }
}
