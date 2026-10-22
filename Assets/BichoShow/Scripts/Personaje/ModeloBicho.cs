using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Va en cada personaje (prefab de Resources/Personajes, lo crea la herramienta del personaje):
    /// apunta sus huesos, sus anclajes del vestuario y lo que se tiñe del color del jugador, para
    /// que PersonajeBicho pueda ponerlo en un bicho y reconectarlo todo.
    /// </summary>
    public class ModeloBicho : MonoBehaviour
    {
        public Animator animador;
        public Transform ojoIzq, ojoDer;
        public Transform brazoIzq, brazoDer, antebrazoIzq, antebrazoDer, manoIzq, manoDer;
        public Transform antenaIzq, antenaDer, puntaAntenaIzq, puntaAntenaDer;
        [Tooltip("Alas (abeja, luciérnaga): aletean por código.")]
        public Transform alaIzq, alaDer;
        public Transform anclaGorro, anclaCara, anclaPecho;
        [Tooltip("Renderers cuya piel (y caparazón) se tiñen con el color del jugador.")]
        public Renderer[] piel;
    }
}
