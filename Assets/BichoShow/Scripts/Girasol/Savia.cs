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

        float alturaInicial, altura;
        float inicio = -1f;
        float multiplicador = 1f;

        /// <summary>Altura de la superficie de la savia.</summary>
        public float Altura => transform.position.y;
        public bool Subiendo => inicio >= 0f && Time.time - inicio > retraso;

        void Awake() => alturaInicial = transform.position.y;

        public void Empezar()
        {
            inicio = Time.time;
            multiplicador = 1f;
            altura = alturaInicial;
            Colocar(altura);
        }

        public void Parar() => inicio = -1f;

        /// <summary>Sube más deprisa (por ejemplo, cuando ya ha llegado alguien a la meta).</summary>
        public void Acelerar(float veces) => multiplicador = veces;

        void Update()
        {
            if (inicio < 0f) return;
            if (!Subiendo) return;
            altura = Mathf.Min(alturaMaxima, altura + velocidad * multiplicador * Time.deltaTime);
            Colocar(altura);
        }

        void Colocar(float y)
        {
            var p = transform.position;
            transform.position = new Vector3(p.x, y, p.z);
        }
    }
}
