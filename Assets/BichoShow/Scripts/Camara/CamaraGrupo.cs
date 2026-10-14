using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Cámara compartida tipo concurso: mira siempre desde el mismo ángulo y se aleja o se
    /// acerca para que quepan todos los bichos. El ángulo fijo hace que "arriba en el stick"
    /// sea siempre "hacia el fondo de la pantalla" para todos los jugadores.
    ///
    /// Más adelante se puede cambiar por Cinemachine (Target Group) sin tocar a los bichos.
    /// </summary>
    public class CamaraGrupo : MonoBehaviour
    {
        [Tooltip("Desde dónde mira la cámara, relativo al centro del grupo.")]
        public Vector3 direccion = new Vector3(0f, 1.1f, -1f);
        public float distanciaMinima = 11f;
        public float distanciaMaxima = 28f;
        [Tooltip("Cuánto se aleja por cada metro de separación entre jugadores.")]
        public float factorSeparacion = 1.1f;
        [Tooltip("Tiempo de suavizado del movimiento (s).")]
        public float suavizado = 0.35f;
        [Tooltip("Dónde mirar cuando todavía no hay nadie jugando.")]
        public Vector3 centroSinJugadores = Vector3.zero;

        Vector3 velocidad;

        void Start()
        {
            ColocarAlInstante();
        }

        void LateUpdate()
        {
            Vector3 objetivo = CalcularPosicion();
            transform.position = Vector3.SmoothDamp(transform.position, objetivo, ref velocidad, suavizado);
            transform.rotation = Quaternion.LookRotation(-direccion.normalized, Vector3.up);
        }

        public void ColocarAlInstante()
        {
            transform.position = CalcularPosicion();
            transform.rotation = Quaternion.LookRotation(-direccion.normalized, Vector3.up);
            velocidad = Vector3.zero;
        }

        Vector3 CalcularPosicion()
        {
            var bichos = ControladorBicho.Todos;
            Vector3 centro = centroSinJugadores;
            float separacion = 0f;

            if (bichos.Count > 0)
            {
                var limites = new Bounds(bichos[0].transform.position, Vector3.zero);
                for (int i = 1; i < bichos.Count; i++)
                    limites.Encapsulate(bichos[i].transform.position);

                centro = limites.center;
                separacion = Mathf.Max(limites.size.x, limites.size.z);
            }

            float distancia = Mathf.Clamp(distanciaMinima + separacion * factorSeparacion,
                                          distanciaMinima, distanciaMaxima);
            return centro + direccion.normalized * distancia;
        }
    }
}
