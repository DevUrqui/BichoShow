using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Camino de baldosas sobre un hueco, por tramos. Cada tramo solo está mientras alguien pisa su
    /// placa (todas en la orilla de salida); al soltarla, parpadea en rojo un momento y desaparece.
    /// La placa del otro lado enciende todos los tramos a la vez. Con un solo jugador en el
    /// equipo, el camino está siempre.
    /// </summary>
    public class CaminoBaldosas : MonoBehaviour
    {
        public int equipo;
        [Tooltip("Los tramos, del más cercano al más lejano (se encienden y apagan enteros).")]
        public GameObject[] tramos;
        [Tooltip("Una placa por tramo, en la orilla de salida.")]
        public PlacaPresion[] placas;
        [Tooltip("Placa del otro lado: enciende todos los tramos.")]
        public PlacaPresion placaLejos;
        [Tooltip("Segundos que aguanta un tramo tras soltar su placa (parpadeando).")]
        public float gracia = 1.4f;
        public Color colorEncendido = new Color(0.4f, 0.95f, 1f);
        public Color colorAviso = new Color(1f, 0.3f, 0.25f);

        float[] hasta;
        Material[][] materiales;
        static readonly int IdColor = Shader.PropertyToID("_BaseColor");

        /// <summary>¿Está el tramo i? (para los bots)</summary>
        public bool Visible(int i) => tramos != null && i >= 0 && i < tramos.Length && tramos[i] != null && tramos[i].activeSelf;

        void Awake()
        {
            int n = tramos != null ? tramos.Length : 0;
            hasta = new float[n];
            materiales = new Material[n][];
            for (int i = 0; i < n; i++)
            {
                if (tramos[i] == null) continue;
                var rs = tramos[i].GetComponentsInChildren<Renderer>(true);
                materiales[i] = new Material[rs.Length];
                for (int k = 0; k < rs.Length; k++) materiales[i][k] = rs[k].material; // copia propia: parpadea solo
                tramos[i].SetActive(false);
            }
        }

        void Update()
        {
            if (tramos == null) return;
            var ronda = RondaFinal.Instancia;
            bool solo = ronda != null && ronda.MiembrosDe(equipo) <= 1;
            bool todos = solo || (placaLejos != null && placaLejos.Pulsada);
            for (int i = 0; i < tramos.Length; i++)
            {
                if (tramos[i] == null) continue;
                bool pisada = todos || (placas != null && i < placas.Length && placas[i] != null && placas[i].Pulsada);
                if (pisada) hasta[i] = Time.time + gracia;
                float queda = hasta[i] - Time.time;
                bool ver = queda > 0f;
                if (tramos[i].activeSelf != ver)
                {
                    tramos[i].SetActive(ver);
                    Sonidos.Reproducir(ver ? Sonido.Click : Sonido.Grieta, tramos[i].transform.position, 0.5f, ver ? 1.3f : 1.5f);
                }
                if (!ver) continue;
                // Pisada: encendido. Soltada: parpadea hacia rojo cada vez más deprisa.
                Color c = colorEncendido;
                if (!pisada)
                {
                    float k = 1f - queda / Mathf.Max(0.1f, gracia);
                    float latido = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(10f, 30f, k));
                    c = Color.Lerp(colorEncendido, colorAviso, Mathf.Lerp(0.4f, 1f, k) * latido);
                }
                foreach (var m in materiales[i]) if (m != null && m.HasProperty(IdColor)) m.SetColor(IdColor, c);
            }
        }
    }
}
