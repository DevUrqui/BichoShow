using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "La Cumbre del Girasol": la Corona Dorada flota sobre el géiser de polen del centro del
    /// girasol, subiendo y bajando despacio. Gana quien la toca con la mano estirada (el botón
    /// de agarrar) — normalmente saltando hacia ella en el momento justo.
    /// </summary>
    public class CoronaCumbre : MonoBehaviour
    {
        public float amplitud = 1f;
        public float periodo = 3.4f;
        [Tooltip("Distancia (desde el centro del bicho o desde su mano) a la que se coge.")]
        public float alcance = 1.5f;
        public float giro = 60f;

        Vector3 inicio;
        bool cogida;

        /// <summary>Alguien la ha cogido (RondaCumbre lo escucha).</summary>
        public event System.Action<ControladorBicho> AlCoger;

        void Awake() => inicio = transform.position;

        public void Reiniciar()
        {
            cogida = false;
            gameObject.SetActive(true);
        }

        void Update()
        {
            if (cogida) return;
            transform.position = inicio + Vector3.up * amplitud * Mathf.Sin(Time.time / periodo * Mathf.PI * 2f);
            transform.Rotate(0f, giro * Time.deltaTime, 0f, Space.World);

            foreach (var b in ControladorBicho.Todos)
            {
                if (b == null || b.Retirado || !b.ManoEstirada || b.Tumbado) continue;
                float d = Mathf.Min(Vector3.Distance(b.PuntoMano, transform.position),
                                    Vector3.Distance(b.Cuerpo.worldCenterOfMass, transform.position));
                if (d > alcance) continue;
                cogida = true;
                AlCoger?.Invoke(b);
                return;
            }
        }
    }
}
