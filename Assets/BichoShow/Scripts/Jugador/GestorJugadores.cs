using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Va junto al PlayerInputManager. Cada vez que alguien pulsa un botón en un mando
    /// (o en el teclado) nuevo, el PlayerInputManager crea un bicho; aquí lo colocamos
    /// en su punto de aparición y le damos su color.
    /// </summary>
    [RequireComponent(typeof(PlayerInputManager))]
    public class GestorJugadores : MonoBehaviour
    {
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
            manager = GetComponent<PlayerInputManager>();
            // Necesario para que funcionen los eventos onPlayerJoined / onPlayerLeft de C#.
            manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
        }

        void OnEnable()
        {
            manager.onPlayerJoined += AlUnirse;
            manager.onPlayerLeft += AlSalir;
        }

        void OnDisable()
        {
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
            Preparar(bicho, indice, $"Bicho {indice + 1} ({entrada.currentControlScheme})");
        }

        void AlSalir(PlayerInput entrada)
        {
            var bicho = entrada.GetComponent<ControladorBicho>();
            if (bicho != null) jugadores.Remove(bicho);
        }

        void Preparar(ControladorBicho bicho, int indice, string nombre)
        {
            Transform punto = puntosAparicion != null && puntosAparicion.Length > 0
                ? puntosAparicion[indice % puntosAparicion.Length]
                : transform;

            bicho.Colocar(punto.position, punto.rotation);
            if (colores != null && colores.Length > 0)
                bicho.AsignarColor(colores[indice % colores.Length]);

            bicho.gameObject.name = nombre;
            jugadores.Add(bicho);
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
