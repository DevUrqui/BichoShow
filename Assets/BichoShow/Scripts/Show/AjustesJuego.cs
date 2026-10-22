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

            // Una copia de los ajustes de entrada (para no tocar ningún archivo del proyecto).
            // En el editor, la copia de la partida anterior ya no existe (Unity la destruye al
            // parar): entonces se parte de unos ajustes nuevos.
            var actuales = InputSystem.settings;
            InputSettings ajustes;
            if (actuales != null && actuales.name == NombreCopia) ajustes = actuales;
            else ajustes = actuales != null ? Object.Instantiate(actuales) : ScriptableObject.CreateInstance<InputSettings>();
            ajustes.name = NombreCopia;
            ajustes.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = ajustes;

#if UNITY_EDITOR
            // Al parar, se vuelven a poner los ajustes del proyecto (la copia deja de existir).
            if (actuales != null && actuales.name != NombreCopia) original = actuales;
            UnityEditor.EditorApplication.playModeStateChanged -= AlCambiarModo;
            UnityEditor.EditorApplication.playModeStateChanged += AlCambiarModo;
#endif
        }

#if UNITY_EDITOR
        static InputSettings original;

        static void AlCambiarModo(UnityEditor.PlayModeStateChange cambio)
        {
            if (cambio != UnityEditor.PlayModeStateChange.EnteredEditMode || original == null) return;
            InputSystem.settings = original;
        }
#endif
    }
}
