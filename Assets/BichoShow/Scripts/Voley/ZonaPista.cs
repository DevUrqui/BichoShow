using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Suelo de la pista de voley. Si la pelota lo toca, se decide el punto:
    /// en el campo de un equipo, punto para el otro; fuera (-1), pierde quien la tocó último.
    /// </summary>
    public class ZonaPista : MonoBehaviour
    {
        [Tooltip("Equipo dueño de este campo (0 o 1). -1 = fuera de la pista.")]
        public int equipo = -1;
    }
}
