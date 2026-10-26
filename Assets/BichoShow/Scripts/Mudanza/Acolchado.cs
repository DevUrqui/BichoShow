using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Marca una pieza como acolchada (las hojas de la puerta giratoria): los objetos frágiles
    /// que chocan con ella (o con algo que cuelgue de ella) reciben solo una parte del daño.
    /// </summary>
    public class Acolchado : MonoBehaviour
    {
        [Tooltip("Parte del daño que llega al objeto. 0 = nada, 1 = como un golpe normal.")]
        [Range(0f, 1f)] public float factorDano = 0.35f;
    }
}
