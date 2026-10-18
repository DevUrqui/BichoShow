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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Aplicar()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;

            // Una copia de los ajustes de entrada (para no tocar ningún archivo del proyecto).
            var ajustes = Object.Instantiate(InputSystem.settings);
            ajustes.name = "Ajustes de entrada de Bicho Show";
            ajustes.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = ajustes;
        }
    }
}
