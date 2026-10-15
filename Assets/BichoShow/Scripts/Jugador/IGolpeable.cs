using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Algo que el bicho golpea con el botón de lanzarse si lo tiene cerca (la pelota del voley…),
    /// en vez de tirarse en plancha.
    /// </summary>
    public interface IGolpeable
    {
        /// <param name="quien">El bicho que golpea.</param>
        /// <param name="direccion">Hacia dónde apunta con el stick (en el suelo).</param>
        /// <param name="enElAire">Si está saltando (para rematar).</param>
        /// <returns>Si el golpe se ha dado.</returns>
        bool Golpear(ControladorBicho quien, Vector3 direccion, bool enElAire);
    }

    /// <summary>Marca un objeto que los bichos no pueden agarrar (p. ej. la pelota).</summary>
    public class NoAgarrable : MonoBehaviour { }
}
