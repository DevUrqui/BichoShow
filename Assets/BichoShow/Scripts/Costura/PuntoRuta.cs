using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Punto del camino que siguen los bots en una carrera. Cada punto dice adónde ir después,
    /// si hay que saltar al llegar (borde de un hueco) y si hay que esperar a que un obstáculo
    /// deje pasar. Con dos caminos, el primero es el difícil y el segundo el fácil.
    /// </summary>
    public class PuntoRuta : MonoBehaviour
    {
        [Tooltip("Adónde ir después. Con dos: [0] la ruta difícil (y rápida), [1] la fácil.")]
        public PuntoRuta[] siguientes;
        [Tooltip("Al llegar aquí, saltar (borde de un hueco o escalón).")]
        public bool saltar;
        [Tooltip("Esperar aquí hasta que este obstáculo diga que es buen momento (IEsperaBot).")]
        public MonoBehaviour esperarA;
        [Tooltip("Ancho útil a cada lado (m): cada bot va por su carril, sin amontonarse.")]
        public float anchura = 1f;

        public IEsperaBot Espera => esperarA as IEsperaBot;

        void OnDrawGizmos()
        {
            Gizmos.color = saltar ? new Color(1f, 0.6f, 0.2f) : esperarA != null ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.9f, 1f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.3f, 0.3f);
            if (siguientes == null) return;
            foreach (var s in siguientes)
                if (s != null) Gizmos.DrawLine(transform.position + Vector3.up * 0.3f, s.transform.position + Vector3.up * 0.3f);
        }
    }
}
