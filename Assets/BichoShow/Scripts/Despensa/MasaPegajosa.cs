using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Despensa Deslizante", zona 1: masa de pan cruda extendida sobre el mantel. Pega: quien la
    /// pisa va muy despacio (y saltar no ayuda, se sigue pegado). El último rodillo rueda por
    /// encima de ella: hay que cruzarla cuando el rodillo se va. El pivote está en el centro de
    /// la masa, a ras del suelo.
    /// </summary>
    public class MasaPegajosa : MonoBehaviour
    {
        [Tooltip("Ancho y largo de la masa (m).")]
        public Vector2 tamano = new Vector2(22f, 10f);
        [Tooltip("Velocidad máxima de lado y hacia delante dentro de la masa (m/s).")]
        public float velocidadMaxima = 2.2f;

        void FixedUpdate()
        {
            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado) continue;
                Vector3 local = transform.InverseTransformPoint(b.transform.position);
                if (Mathf.Abs(local.x) > tamano.x * 0.5f || Mathf.Abs(local.z) > tamano.y * 0.5f || local.y < -0.5f || local.y > 1.2f) continue;
                var cuerpo = b.Cuerpo;
                var v = cuerpo.linearVelocity;
                var plano = new Vector3(v.x, 0f, v.z);
                if (plano.magnitude > velocidadMaxima)
                {
                    plano = plano.normalized * velocidadMaxima;
                    cuerpo.linearVelocity = new Vector3(plano.x, Mathf.Min(v.y, 3f), plano.z);
                }
            }
        }
    }
}
