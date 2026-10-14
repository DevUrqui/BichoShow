using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Cámara compartida tipo concurso. Mira siempre con el mismo rumbo (así "arriba en el
    /// stick" es "hacia el fondo de la pantalla" para todos) y:
    ///  - calcula la distancia justa para que quepan todos, según el campo de visión y la
    ///    forma de la pantalla;
    ///  - se inclina hacia una vista más cenital cuando el grupo se estira a lo largo del
    ///    recorrido, para que quepa más mapa sin alejarse tanto;
    ///  - prioriza a los jugadores humanos: los bots no la obligan a alejarse;
    ///  - quien se quede fuera de plano aparece como una flecha de su color en el borde.
    ///
    /// Más adelante se puede cambiar por Cinemachine (Target Group) sin tocar a los bichos.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CamaraGrupo : MonoBehaviour
    {
        [Header("Ángulo")]
        [Tooltip("Desde dónde mira la cámara, relativo al centro del grupo. Marca el rumbo y la inclinación base.")]
        public Vector3 direccion = new Vector3(0f, 1.1f, -1f);
        [Tooltip("Grados extra de inclinación (más cenital) cuando el grupo está muy estirado.")]
        public float inclinacionExtraMaxima = 22f;
        [Tooltip("Separación a lo largo (m) con la que se alcanza la inclinación extra máxima.")]
        public float separacionParaInclinacionMaxima = 35f;

        [Header("Distancia")]
        public float distanciaMinima = 11f;
        public float distanciaMaxima = 45f;
        [Tooltip("Margen alrededor de los jugadores (m), para que no queden pegados al borde.")]
        public float margen = 2.5f;
        [Tooltip("Tiempo de suavizado del movimiento (s).")]
        public float suavizado = 0.35f;

        [Header("A quién encuadrar")]
        [Tooltip("Si hay algún humano, los bots no cuentan para el encuadre.")]
        public bool priorizarHumanos = true;
        [Tooltip("Dónde mirar cuando todavía no hay nadie jugando.")]
        public Vector3 centroSinJugadores = Vector3.zero;

        [Header("Indicadores fuera de plano")]
        public bool mostrarIndicadores = true;

        Camera cam;
        Vector3 velocidad;
        float inclinacionActual;
        float velocidadInclinacion;
        readonly List<Vector3> puntos = new List<Vector3>();
        GUIStyle estiloIndicador;

        void Awake() => cam = GetComponent<Camera>();

        void Start() => ColocarAlInstante();

        void LateUpdate()
        {
            Calcular(out Vector3 posicion, out float inclinacionObjetivo);
            inclinacionActual = Mathf.SmoothDamp(inclinacionActual, inclinacionObjetivo, ref velocidadInclinacion, suavizado * 1.5f);
            transform.rotation = Rotacion(inclinacionActual);

            // La posición se recalcula con la inclinación ya suavizada para que no haya saltos.
            Calcular(out posicion, out _, inclinacionActual);
            transform.position = Vector3.SmoothDamp(transform.position, posicion, ref velocidad, suavizado);
        }

        public void ColocarAlInstante()
        {
            if (cam == null) cam = GetComponent<Camera>();
            Calcular(out _, out inclinacionActual);
            transform.rotation = Rotacion(inclinacionActual);
            Calcular(out Vector3 posicion, out _, inclinacionActual);
            transform.position = posicion;
            velocidad = Vector3.zero;
            velocidadInclinacion = 0f;
        }

        // ---------------------------------------------------------------------
        // Encuadre
        // ---------------------------------------------------------------------

        float InclinacionBase
        {
            get
            {
                float horizontal = new Vector2(direccion.x, direccion.z).magnitude;
                return Mathf.Atan2(direccion.y, Mathf.Max(0.001f, horizontal)) * Mathf.Rad2Deg;
            }
        }

        Quaternion Rotacion(float inclinacion)
        {
            Vector3 rumbo = new Vector3(-direccion.x, 0f, -direccion.z);
            if (rumbo.sqrMagnitude < 0.0001f) rumbo = Vector3.forward;
            return Quaternion.LookRotation(rumbo.normalized, Vector3.up) * Quaternion.Euler(inclinacion, 0f, 0f);
        }

        void RecogerPuntos()
        {
            puntos.Clear();
            var bichos = ControladorBicho.Todos;

            bool hayHumanos = false;
            if (priorizarHumanos)
                foreach (var b in bichos)
                    if (!b.EsBot) { hayHumanos = true; break; }

            foreach (var b in bichos)
                if (!hayHumanos || !b.EsBot)
                    puntos.Add(b.transform.position + Vector3.up * 0.6f);
        }

        /// <summary>Posición e inclinación para encuadrar a todos. Si se pasa una inclinación, usa esa.</summary>
        void Calcular(out Vector3 posicion, out float inclinacionObjetivo, float? inclinacionFija = null)
        {
            RecogerPuntos();

            Vector3 centro = centroSinJugadores;
            float largo = 0f;
            if (puntos.Count > 0)
            {
                var limites = new Bounds(puntos[0], Vector3.zero);
                for (int i = 1; i < puntos.Count; i++) limites.Encapsulate(puntos[i]);
                centro = limites.center;

                // "Largo" = cuánto se estira el grupo en el sentido en que mira la cámara.
                Vector3 rumbo = new Vector3(-direccion.x, 0f, -direccion.z).normalized;
                float min = float.MaxValue, max = float.MinValue;
                foreach (var p in puntos)
                {
                    float d = Vector3.Dot(p, rumbo);
                    min = Mathf.Min(min, d);
                    max = Mathf.Max(max, d);
                }
                largo = max - min;
            }

            float t = Mathf.Clamp01(largo / Mathf.Max(1f, separacionParaInclinacionMaxima));
            inclinacionObjetivo = Mathf.Min(85f, InclinacionBase + inclinacionExtraMaxima * t);
            float inclinacion = inclinacionFija ?? inclinacionObjetivo;

            Quaternion rot = Rotacion(inclinacion);
            Vector3 adelante = rot * Vector3.forward, derecha = rot * Vector3.right, arriba = rot * Vector3.up;

            // Distancia mínima para que cada punto quepa en el frustum (con margen).
            float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * Mathf.Max(0.1f, cam.aspect);
            float distancia = distanciaMinima;
            foreach (var p in puntos)
            {
                Vector3 o = p - centro;
                float profundidad = Vector3.Dot(o, adelante);
                float x = Mathf.Abs(Vector3.Dot(o, derecha)) + margen;
                float y = Mathf.Abs(Vector3.Dot(o, arriba)) + margen;
                distancia = Mathf.Max(distancia, x / tanH - profundidad, y / tanV - profundidad);
            }
            distancia = Mathf.Min(distancia, distanciaMaxima);

            posicion = centro - adelante * distancia;
        }

        // ---------------------------------------------------------------------
        // Flechas para quien se sale de plano
        // ---------------------------------------------------------------------

        void OnGUI()
        {
            if (!mostrarIndicadores || cam == null) return;

            if (estiloIndicador == null)
                estiloIndicador = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            estiloIndicador.fontSize = Mathf.Max(12, Screen.height / 40);

            float tam = Screen.height * 0.055f;
            float borde = tam * 0.9f;
            var centroPantalla = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            foreach (var b in ControladorBicho.Todos)
            {
                Vector3 vp = cam.WorldToViewportPoint(b.transform.position + Vector3.up * 0.6f);
                bool detras = vp.z < 0f;
                bool dentro = !detras && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f;
                if (dentro) continue;

                // Dirección desde el centro de la pantalla (en píxeles de GUI, con Y hacia abajo).
                Vector2 pantalla = new Vector2(vp.x * Screen.width, (1f - vp.y) * Screen.height);
                Vector2 dir = pantalla - centroPantalla;
                if (detras) dir = -dir;
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.down;

                // Llevar el indicador al borde de la pantalla en esa dirección.
                float sx = (Screen.width * 0.5f - borde) / Mathf.Max(0.001f, Mathf.Abs(dir.x));
                float sy = (Screen.height * 0.5f - borde) / Mathf.Max(0.001f, Mathf.Abs(dir.y));
                Vector2 pos = centroPantalla + dir * Mathf.Min(sx, sy);

                Color color = b.ColorJugador;
                string etiqueta = b.EsBot ? $"B{b.IndiceJugador + 1}" : $"J{b.IndiceJugador + 1}";
                DibujarIndicador(pos, dir, tam, color, etiqueta);
            }
        }

        void DibujarIndicador(Vector2 pos, Vector2 dir, float tam, Color color, string etiqueta)
        {
            var antes = GUI.color;
            var matriz = GUI.matrix;

            // Flecha: un "▲" girado hacia donde está el bicho, un poco por fuera del círculo.
            float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
            Vector2 puntaFlecha = pos + dir.normalized * tam * 0.7f;
            GUIUtility.RotateAroundPivot(angulo, puntaFlecha);
            estiloIndicador.normal.textColor = color;
            GUI.Label(new Rect(puntaFlecha.x - tam * 0.5f, puntaFlecha.y - tam * 0.5f, tam, tam), "▲", estiloIndicador);
            GUI.matrix = matriz;

            // Círculo (cuadrado redondeado de GUI) con el número del jugador.
            var r = new Rect(pos.x - tam * 0.5f, pos.y - tam * 0.5f, tam, tam);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = antes;

            estiloIndicador.normal.textColor = Color.black;
            GUI.Label(r, etiqueta, estiloIndicador);
        }
    }
}
