using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Trampa de prueba: una barra que gira y tira al suelo a quien la toca.
    /// Usa un Rigidbody cinemático para que la física calcule bien los golpes.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BarraGiratoria : MonoBehaviour
    {
        [Tooltip("Grados por segundo. Negativo para girar al revés.")]
        public float velocidadGiro = 90f;

        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void FixedUpdate()
        {
            Quaternion paso = Quaternion.Euler(0f, velocidadGiro * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(paso * rb.rotation);
        }
    }
}
