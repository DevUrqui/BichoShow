using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "La Cumbre del Girasol": la savia pegajosa que inunda el invernadero. Tras unos segundos de
    /// gracia sube sin parar; quien se queda por debajo de ella, se pega y queda eliminado
    /// (RondaCumbre lo comprueba). Es el reloj de la final: no deja quedarse quieto.
    /// </summary>
    public class Savia : MonoBehaviour
    {
        [Tooltip("Metros por segundo que sube.")]
        public float velocidad = 0.38f;
        [Tooltip("Segundos de gracia antes de empezar a subir.")]
        public float retraso = 8f;
        [Tooltip("Altura máxima (la cima).")]
        public float alturaMaxima = 46f;

        float alturaInicial;
        float inicio = -1f;

        /// <summary>Altura de la superficie de la savia.</summary>
        public float Altura => transform.position.y;
        public bool Subiendo => inicio >= 0f && Time.time - inicio > retraso;

        void Awake() => alturaInicial = transform.position.y;

        public void Empezar()
        {
            inicio = Time.time;
            Colocar(alturaInicial);
        }

        public void Parar() => inicio = -1f;

        void Update()
        {
            if (inicio < 0f) return;
            float t = Time.time - inicio - retraso;
            if (t <= 0f) return;
            Colocar(Mathf.Min(alturaMaxima, alturaInicial + t * velocidad));
        }

        void Colocar(float y)
        {
            var p = transform.position;
            transform.position = new Vector3(p.x, y, p.z);
        }
    }
}
