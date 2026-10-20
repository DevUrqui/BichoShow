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
            Preparar(bicho, indice, $"J{indice + 1} ({entrada.currentControlScheme})");
            Vestuario.PonerGuardado(bicho);
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

        /// <summary>Jugadores con mando o teclado.</summary>
        public int NumeroHumanos => PlayerInput.all.Count;

        /// <summary>
        /// Crea un bicho controlado por la máquina con el componente T (que debe implementar
        /// IEntradaBicho). Usa el mismo prefab que los jugadores, pero sin PlayerInput, para
        /// que no se quede con ningún mando ni cuente como jugador del PlayerInputManager.
        /// </summary>
        public T CrearBot<T>(int indice) where T : Component, IEntradaBicho
        {
            var prefab = manager.playerPrefab;
            if (prefab == null) return null;

            // Se crea dentro de un objeto desactivado para quitar el PlayerInput antes de que se active.
            var contenedor = new GameObject("CreandoBot");
            contenedor.SetActive(false);
            var go = Instantiate(prefab, contenedor.transform);
            var entrada = go.GetComponent<PlayerInput>();
            if (entrada != null) DestroyImmediate(entrada);
            var cerebro = go.AddComponent<T>();
            go.transform.SetParent(null, false); // aquí se activa
            Destroy(contenedor);

            var bicho = go.GetComponent<ControladorBicho>();
            bicho.AsignarIndice(indice);
            Preparar(bicho, indice, $"Bot {indice + 1}");
            Vestuario.PonerAlAzar(bicho);
            bots.Add(bicho);
            return cerebro;
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
            if (!mostrarAyuda) return;

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
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), texto, estiloAyuda);
            estiloAyuda.normal.textColor = color;
            GUI.Label(rect, texto, estiloAyuda);
            y += 30f;
        }
    }
}
