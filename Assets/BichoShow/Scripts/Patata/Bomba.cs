using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// La bomba de "Patata caliente": flota sobre la cabeza de quien la lleva, con la mecha
    /// encendida. Late y pita cada vez más rápido y se pone roja al final. Quién la lleva y
    /// cuándo explota lo decide RondaPatata; esto es solo su aspecto y su cuenta atrás.
    /// </summary>
    public class Bomba : MonoBehaviour
    {
        [Tooltip("Modelo de la bomba (se escala y se tiñe).")]
        public Transform modelo;
        [Tooltip("Altura sobre el centro del bicho a la que flota.")]
        public float altura = 1.35f;

        public ControladorBicho Portador { get; private set; }
        public bool Encendida => gameObject.activeSelf && Portador != null;
        public float Restante => fin - Time.time;
        /// <summary>0 al encenderse, 1 al explotar.</summary>
        public float Progreso => duracion > 0f ? Mathf.Clamp01(1f - Restante / duracion) : 0f;

        float fin, duracion, siguientePitido, inicioCambio = -10f;
        Vector3 escalaModelo = Vector3.one;
        Material[] materiales;
        Color[] coloresBase;
        Vector3 posicionSuave;

        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            if (modelo == null && transform.childCount > 0) modelo = transform.GetChild(0);
            if (modelo != null)
            {
                escalaModelo = modelo.localScale;
                var r = modelo.GetComponentInChildren<Renderer>();
                if (r != null)
                {
                    materiales = r.materials; // instancias propias
                    coloresBase = new Color[materiales.Length];
                    for (int i = 0; i < materiales.Length; i++)
                        coloresBase[i] = materiales[i].HasProperty(IdBaseColor) ? materiales[i].GetColor(IdBaseColor) : Color.white;
                }
            }
        }

        public void Encender(ControladorBicho portador, float segundos)
        {
            duracion = segundos;
            fin = Time.time + segundos;
            siguientePitido = Time.time + 1f;
            gameObject.SetActive(true);
            Pasar(portador);
            posicionSuave = Objetivo();
            transform.position = posicionSuave;
        }

        public void Pasar(ControladorBicho nuevo)
        {
            Portador = nuevo;
            inicioCambio = Time.time;
        }

        public void Apagar()
        {
            Portador = null;
            gameObject.SetActive(false);
        }

        Vector3 Objetivo()
        {
            if (Portador == null) return transform.position;
            Vector3 centro = Portador.Cuerpo != null ? Portador.Cuerpo.worldCenterOfMass : Portador.transform.position;
            return centro + Vector3.up * altura;
        }

        void LateUpdate()
        {
            if (Portador == null) return;
            float t = Progreso;

            // Vuela hacia la cabeza del nuevo portador en un arco rápido; luego, le sigue.
            float desdeCambio = Time.time - inicioCambio;
            Vector3 objetivo = Objetivo() + Vector3.up * Mathf.Sin(Time.time * 6f) * 0.06f;
            posicionSuave = Vector3.Lerp(posicionSuave, objetivo, 1f - Mathf.Exp(-(desdeCambio < 0.25f ? 18f : 30f) * Time.deltaTime));
            if (desdeCambio < 0.25f) posicionSuave += Vector3.up * Mathf.Sin(desdeCambio / 0.25f * Mathf.PI) * 0.12f;
            transform.position = posicionSuave;
            transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 3f) * 12f, Time.time * 90f, Mathf.Cos(Time.time * 2.5f) * 12f);

            // Late cada vez más rápido y se hincha al final.
            float ritmo = Mathf.Lerp(2f, 16f, t * t);
            float latido = 1f + Mathf.Max(0f, Mathf.Sin(Time.time * ritmo)) * Mathf.Lerp(0.05f, 0.18f, t) + t * 0.25f;
            if (modelo != null) modelo.localScale = escalaModelo * latido;

            // Al final, destellos rojos.
            if (materiales != null)
            {
                bool rojo = t > 0.6f && Mathf.Repeat(Time.time * ritmo * 0.5f, 1f) < 0.5f;
                for (int i = 0; i < materiales.Length; i++)
                {
                    if (!materiales[i].HasProperty(IdBaseColor)) continue;
                    Color c = i == 0 && rojo ? new Color(0.9f, 0.1f, 0.1f) : coloresBase[i];
                    materiales[i].SetColor(IdBaseColor, c);
                }
            }

            // Pitidos que se aceleran.
            if (Time.time >= siguientePitido)
            {
                Sonidos.Reproducir(Sonido.Pitido, transform.position, Mathf.Lerp(0.35f, 0.9f, t), Mathf.Lerp(0.8f, 1.6f, t), 0f);
                siguientePitido = Time.time + Mathf.Lerp(0.9f, 0.12f, t);
            }
        }
    }
}
