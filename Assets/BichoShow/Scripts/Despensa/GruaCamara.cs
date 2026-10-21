using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Grúa de cámara de tele del plató: el brazo gira despacio para seguir al pelotón (la media
    /// de los bichos que corren). Es decorado; la "pegatina" en la pantalla cuando un bicho sale
    /// volando contra ella la pinta la ronda (RondaDespensa).
    /// </summary>
    public class GruaCamara : MonoBehaviour
    {
        [Tooltip("La parte que gira (brazo y cámara) sobre el eje vertical.")]
        public Transform brazo;
        [Tooltip("Lo más que gira a cada lado de su posición de partida (grados).")]
        public float giroMaximo = 70f;
        public float suavizado = 1.2f;

        Quaternion inicio;
        float giro, velocidad;

        void Awake()
        {
            if (brazo != null) inicio = brazo.localRotation;
        }

        void Update()
        {
            if (brazo == null) return;
            Vector3 suma = Vector3.zero;
            int n = 0;
            foreach (var b in ControladorBicho.Todos)
                if (b != null && !b.Retirado) { suma += b.transform.position; n++; }
            float objetivo = 0f;
            if (n > 0)
            {
                Vector3 hacia = brazo.parent != null ? brazo.parent.InverseTransformPoint(suma / n) : suma / n;
                hacia -= brazo.localPosition;
                objetivo = Mathf.Clamp(Mathf.Atan2(hacia.x, hacia.z) * Mathf.Rad2Deg, -giroMaximo, giroMaximo);
            }
            giro = Mathf.SmoothDampAngle(giro, objetivo, ref velocidad, suavizado);
            brazo.localRotation = inicio * Quaternion.Euler(0f, giro, 0f);
        }
    }
}
