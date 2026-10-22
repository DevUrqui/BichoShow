using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Ajustes que se aplican solos al arrancar el juego (antes de cargar la primera escena).
    /// Para jugar por Parsec: quien hospeda suele tener abiertos Parsec, Discord… y la ventana
    /// del juego puede perder el foco. Con esto el juego sigue funcionando y sigue leyendo los
    /// mandos (los de los invitados son mandos virtuales en el PC de quien hospeda).
    /// </summary>
    static class AjustesJuego
    {
        const string NombreCopia = "Ajustes de entrada de Bicho Show";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Aplicar()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;

            // Una copia de los ajustes de entrada (para no tocar ningún archivo del proyecto). Se
            // marca para que Unity no la borre al parar el juego en el editor: si la borrara, el
            // Input System se quedaría apuntando a algo que ya no existe.
            var actuales = InputSystem.settings;
            if (actuales != null && actuales.name == NombreCopia) return; // ya está puesta
            InputSystem.settings = Copia(actuales);
        }

        static InputSettings Copia(InputSettings desde)
        {
            var ajustes = desde != null ? Object.Instantiate(desde) : ScriptableObject.CreateInstance<InputSettings>();
            ajustes.name = NombreCopia;
            ajustes.hideFlags = HideFlags.HideAndDontSave;
            ajustes.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            return ajustes;
        }

#if UNITY_EDITOR
        /// <summary>
        /// En el editor: si los ajustes de entrada se han quedado apuntando a una copia borrada
        /// (de versiones anteriores de este script), se ponen unos nuevos antes de darle a Play.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        static void RepararEnEditor()
        {
            Reparar();
            UnityEditor.EditorApplication.playModeStateChanged -= AlCambiarModo;
            UnityEditor.EditorApplication.playModeStateChanged += AlCambiarModo;
        }

        static void AlCambiarModo(UnityEditor.PlayModeStateChange cambio)
        {
            if (cambio == UnityEditor.PlayModeStateChange.ExitingEditMode) Reparar();
        }

        static void Reparar()
        {
            var actuales = InputSystem.settings;
            // "== null" de Unity: también es cierto si el objeto existe en C# pero se ha borrado.
            if (actuales == null) InputSystem.settings = Copia(null);
        }
#endif
    }
}
