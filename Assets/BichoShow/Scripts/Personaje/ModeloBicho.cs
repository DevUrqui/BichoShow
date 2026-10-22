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
        [Tooltip("Tamaño de la ropa en este bicho (1 = Bichete): gorros, gafas y capas se ajustan a su cuerpo.")]
        public float tallaRopa = 1f;
        [HideInInspector] public int version; // de la herramienta que lo montó (si es vieja, se vuelve a montar)
        [Tooltip("Renderers cuya piel (y caparazón) se tiñen con el color del jugador.")]
        public Renderer[] piel;
    }
}
