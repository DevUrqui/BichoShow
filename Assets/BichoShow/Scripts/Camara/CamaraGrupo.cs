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
    ///  - quien se quede fuera de plano aparece como una flecha de su color en el borde;
    ///  - si los humanos se separan demasiado, DirectorCamaras parte la pantalla en dos.
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
        [Tooltip("En carreras: además de a los jugadores, encuadra este trozo por delante de ellos (m, hacia donde mira la cámara).")]
        public float adelanto;
        [Tooltip("Encuadra también dónde estará cada bicho dentro de estos segundos (saltos y lanzamientos rápidos no se salen de plano).")]
        public float anticipacion;
        [Header("Órbita (pruebas alrededor de una torre: la cumbre del girasol)")]
        [Tooltip("La cámara gira alrededor del eje, siempre por fuera de los jugadores y mirando hacia dentro.")]
        public bool orbitar;
        public Vector3 ejeOrbita;
        [Tooltip("Altura de la cámara respecto a lo que se aleja hacia fuera (más = más desde arriba).")]
        public float alturaOrbita = 0.75f;
        [Tooltip("Mientras el grupo no se aleje más de esto (grados) del centro del plano, la cámara no gira: así no marea.")]
        public float margenOrbita = 16f;
        [Tooltip("Lo más rápido que gira la cámara alrededor del eje (grados por segundo).")]
        public float velocidadMaximaOrbita = 24f;
        [Tooltip("Grados que la cámara se adelanta al grupo en el sentido de la carrera (negativo = hacia ángulos menores).")]
        public float adelantoOrbita;
        float anguloOrbita, velocidadAnguloOrbita;
        bool hayAnguloOrbita;
        // En órbita se suaviza el punto al que se mira y la distancia (no la posición): así, al
        // girar, los bichos siguen en el centro del plano en vez de irse hacia un borde.
        Vector3 focoSuave, velocidadFoco;
        float distanciaSuave, velocidadDistancia;
        [Tooltip("Dónde mirar cuando todavía no hay nadie jugando.")]
        public Vector3 centroSinJugadores = Vector3.zero;

        [Header("Cámaras por jugador")]
        [Tooltip("En las carreras (escenas con RondaRuta), cada jugador con su cámara detrás de su bicho.")]
        public bool camaraPorJugadorEnCarreras = true;

        [Header("Pantalla dividida")]
        [Tooltip("Si los humanos se separan tanto que no caben, la pantalla se parte en dos.")]
        public bool pantallaDividida = true;

        [Header("Indicadores fuera de plano")]
        public bool mostrarIndicadores = true;

        /// <summary>Si no es null, la cámara encuadra solo a estos bichos (lo usa la pantalla dividida).</summary>
        [System.NonSerialized] public List<ControladorBicho> objetivosForzados;

        public Camera Camara => cam;

        /// <summary>Bichos que la cámara no tiene en cuenta (p. ej. los que celebran en el podio).</summary>
        public static readonly HashSet<ControladorBicho> SinEncuadrar = new HashSet<ControladorBicho>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Limpiar() => SinEncuadrar.Clear();

        Camera cam;
        Vector3 velocidad;
        float inclinacionActual;
        float velocidadInclinacion;
        readonly List<Vector3> puntos = new List<Vector3>();
        GUIStyle estiloIndicador;

        Vector3 posicionSinSacudida;

        void Awake()
        {
            cam = GetComponent<Camera>();
            posicionSinSacudida = transform.position;
            // En las carreras, cada jugador con su cámara detrás de su bicho (como en Fall Guys).
            // En Awake (no en Start) para que también se añada en los invitados del online, donde
            // esta cámara se apaga nada más cargar la escena.
            if (camaraPorJugadorEnCarreras && CompareTag("MainCamera") && FindAnyObjectByType<RondaRuta>() != null && GetComponent<CamarasPorJugador>() == null)
                gameObject.AddComponent<CamarasPorJugador>();
        }

        void Start()
        {
            if (pantallaDividida && GetComponent<DirectorCamaras>() == null)
                gameObject.AddComponent<DirectorCamaras>();
            ColocarAlInstante();
        }

        void LateUpdate()
        {
            RecogerPuntos(puntos);
            if (orbitar) Orbitar(false);
            Encuadre(puntos, cam.aspect, null, out _, out float inclinacionObjetivo, out _);
            inclinacionActual = Mathf.SmoothDamp(inclinacionActual, inclinacionObjetivo, ref velocidadInclinacion, suavizado * 1.5f);
            Quaternion rotacion = Rotacion(inclinacionActual);

            // La posición se calcula con la inclinación ya suavizada para que no haya saltos.
            Calcular(puntos, cam.aspect, inclinacionActual, out Vector3 posicion, out Vector3 foco, out float distancia, out _, out _);
            if (orbitar)
            {
                focoSuave = Vector3.SmoothDamp(focoSuave, foco, ref velocidadFoco, suavizado);
                distanciaSuave = Mathf.SmoothDamp(distanciaSuave, distancia, ref velocidadDistancia, suavizado);
                posicionSinSacudida = focoSuave - rotacion * Vector3.forward * distanciaSuave;
            }
            else posicionSinSacudida = Vector3.SmoothDamp(posicionSinSacudida, posicion, ref velocidad, suavizado);

            // Sacudida (golpes, roturas): se suma encima sin afectar al seguimiento.
            Efectos.Sacudida(out Vector3 temblor, out float giroTemblor);
            transform.SetPositionAndRotation(posicionSinSacudida + rotacion * temblor, rotacion * Quaternion.Euler(0f, 0f, giroTemblor));
        }

        /// <summary>Coloca la cámara en su sitio sin suavizado. Se puede forzar la forma de pantalla.</summary>
        public void ColocarAlInstante(float? aspecto = null)
        {
            if (cam == null) cam = GetComponent<Camera>();
            float a = aspecto ?? cam.aspect;
            RecogerPuntos(puntos);
            if (orbitar) Orbitar(true);
            Encuadre(puntos, a, null, out _, out inclinacionActual, out _);
            transform.rotation = Rotacion(inclinacionActual);
            Calcular(puntos, a, inclinacionActual, out Vector3 posicion, out focoSuave, out distanciaSuave, out _, out _);
            transform.position = posicion;
            posicionSinSacudida = posicion;
            velocidad = velocidadFoco = Vector3.zero;
            velocidadDistancia = 0f;
            velocidadInclinacion = 0f;
        }

        /// <summary>
        /// Coloca la "dirección" de la cámara por fuera de los jugadores, en el ángulo medio en el
        /// que están alrededor del eje (con vectores, para que no salte al dar la vuelta).
        /// Para que no maree: no gira mientras el grupo siga cerca del centro del plano, gira
        /// despacio y con un tope de velocidad, y no hace caso a quien está casi en el eje.
        /// </summary>
        void Orbitar(bool alInstante)
        {
            Vector3 suma = Vector3.zero;
            foreach (var p in puntos)
            {
                var fuera = new Vector3(p.x - ejeOrbita.x, 0f, p.z - ejeOrbita.z);
                float r = fuera.magnitude;
                if (r > 0.1f) suma += fuera / r * Mathf.Clamp01((r - 2f) / 6f);
            }
            if (puntos.Count == 0) suma = new Vector3(centroSinJugadores.x - ejeOrbita.x, 0f, centroSinJugadores.z - ejeOrbita.z);
            if (suma.sqrMagnitude > 0.04f)
            {
                float objetivo = Mathf.Atan2(suma.x, suma.z) * Mathf.Rad2Deg + adelantoOrbita;
                if (alInstante || !hayAnguloOrbita) { anguloOrbita = objetivo; velocidadAnguloOrbita = 0f; hayAnguloOrbita = true; }
                else
                {
                    float delta = Mathf.DeltaAngle(anguloOrbita, objetivo);
                    float sobra = Mathf.Sign(delta) * Mathf.Max(0f, Mathf.Abs(delta) - margenOrbita);
                    // Si el grupo se ha ido muy lejos de golpe (un trampolín, un atajo), se permite ir más deprisa.
                    float tope = velocidadMaximaOrbita * (1f + Mathf.Abs(sobra) / 30f);
                    anguloOrbita = Mathf.SmoothDampAngle(anguloOrbita, anguloOrbita + sobra, ref velocidadAnguloOrbita,
                                                         suavizado * 3f, tope);
                }
            }
            float rad = anguloOrbita * Mathf.Deg2Rad;
            direccion = new Vector3(Mathf.Sin(rad), alturaOrbita, Mathf.Cos(rad));
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

        Vector3 Rumbo
        {
            get
            {
                Vector3 rumbo = new Vector3(-direccion.x, 0f, -direccion.z);
                return rumbo.sqrMagnitude < 0.0001f ? Vector3.forward : rumbo.normalized;
            }
        }

        Quaternion Rotacion(float inclinacion) =>
            Quaternion.LookRotation(Rumbo, Vector3.up) * Quaternion.Euler(inclinacion, 0f, 0f);

        /// <summary>Hacia la derecha de la pantalla, en el suelo (sirve para decidir qué mitad es cuál).</summary>
        public Vector3 DerechaPantalla => Vector3.Cross(Vector3.up, Rumbo);

        void RecogerPuntos(List<Vector3> destino)
        {
            destino.Clear();

            if (objetivosForzados != null)
            {
                foreach (var b in objetivosForzados)
                    if (b != null) Anotar(destino, b);
                return;
            }

            var bichos = ControladorBicho.Todos;
            bool hayHumanos = false;
            if (priorizarHumanos)
                foreach (var b in bichos)
                    if (!b.EsBot) { hayHumanos = true; break; }

            foreach (var b in bichos)
                if ((!hayHumanos || !b.EsBot) && !SinEncuadrar.Contains(b))
                    Anotar(destino, b);

            // Lo que viene por delante también tiene que verse; y un poco por detrás, para que los
            // bichos no queden pegados al borde de abajo (ahí van los carteles de ayuda).
            if (adelanto > 0f)
                for (int i = destino.Count - 1; i >= 0; i--)
                {
                    destino.Add(destino[i] + Rumbo * adelanto);
                    destino.Add(destino[i] - Rumbo * (adelanto * 0.4f));
                }
        }

        public static Vector3 PuntoDe(ControladorBicho b) => b.transform.position + Vector3.up * 0.6f;

        /// <summary>El bicho y, si hay anticipación, dónde estará en un momento (la subida cuenta la mitad, para no dar botes).</summary>
        void Anotar(List<Vector3> destino, ControladorBicho b)
        {
            Vector3 p = PuntoDe(b);
            destino.Add(p);
            if (anticipacion <= 0f) return;
            Vector3 v = b.Velocidad;
            if (v.y > 0f) v.y *= 0.5f;
            destino.Add(p + Vector3.ClampMagnitude(v, 14f) * anticipacion);
        }

        /// <summary>
        /// Calcula dónde poner la cámara para que quepan estos puntos con esta forma de pantalla.
        /// Si se pasa una inclinación, usa esa; si no, la calcula. "distanciaNecesaria" es la
        /// distancia que haría falta sin aplicar el máximo (sirve para decidir si dividir).
        /// </summary>
        public void Encuadre(List<Vector3> pts, float aspecto, float? inclinacionFija,
                             out Vector3 posicion, out float inclinacionObjetivo, out float distanciaNecesaria) =>
            Calcular(pts, aspecto, inclinacionFija, out posicion, out _, out _, out inclinacionObjetivo, out distanciaNecesaria);

        /// <summary>Como Encuadre, y además el punto al que se mira ("foco") y la distancia a él.</summary>
        void Calcular(List<Vector3> pts, float aspecto, float? inclinacionFija, out Vector3 posicion, out Vector3 foco,
                      out float distanciaFinal, out float inclinacionObjetivo, out float distanciaNecesaria)
        {
            Vector3 centro = centroSinJugadores;
            float largo = 0f;
            Vector3 rumbo = Rumbo;
            if (pts.Count > 0)
            {
                var limites = new Bounds(pts[0], Vector3.zero);
                for (int i = 1; i < pts.Count; i++) limites.Encapsulate(pts[i]);
                centro = limites.center;

                // "Largo" = cuánto se estira el grupo en el sentido en que mira la cámara.
                float min = float.MaxValue, max = float.MinValue;
                foreach (var p in pts)
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
            float tanH = tanV * Mathf.Max(0.1f, aspecto);
            float distancia = distanciaMinima;
            foreach (var p in pts)
            {
                Vector3 o = p - centro;
                float profundidad = Vector3.Dot(o, adelante);
                float x = Mathf.Abs(Vector3.Dot(o, derecha)) + margen;
                float y = Mathf.Abs(Vector3.Dot(o, arriba)) + margen;
                distancia = Mathf.Max(distancia, x / tanH - profundidad, y / tanV - profundidad);
            }
            distanciaNecesaria = distancia;
            distancia = Mathf.Min(distancia, distanciaMaxima);

            foco = centro;
            distanciaFinal = distancia;
            posicion = centro - adelante * distancia;
        }

        // ---------------------------------------------------------------------
        // Flechas para quien se sale de plano
        // ---------------------------------------------------------------------

        void OnGUI()
        {
            if (!mostrarIndicadores || cam == null || !cam.enabled) return;

            if (estiloIndicador == null)
                estiloIndicador = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            estiloIndicador.fontSize = Mathf.Max(12, Screen.height / 40);

            // Zona de esta cámara en coordenadas de GUI (Y hacia abajo).
            Rect px = cam.pixelRect;
            var zona = new Rect(px.x, Screen.height - px.yMax, px.width, px.height);
            if (zona.width < 50f) return;

            float tam = Screen.height * 0.055f;
            float borde = tam * 0.9f;
            Vector2 centroZona = zona.center;

            IEnumerable<ControladorBicho> lista = objetivosForzados ?? (IEnumerable<ControladorBicho>)ControladorBicho.Todos;
            foreach (var b in lista)
            {
                if (b == null) continue;
                Vector3 vp = cam.WorldToViewportPoint(PuntoDe(b));
                bool detras = vp.z < 0f;
                bool dentro = !detras && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f;
                if (dentro) continue;

                Vector2 enPantalla = new Vector2(zona.x + vp.x * zona.width, zona.y + (1f - vp.y) * zona.height);
                Vector2 dir = enPantalla - centroZona;
                if (detras) dir = -dir;
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.down;

                // Llevar el indicador al borde de la zona en esa dirección.
                float sx = (zona.width * 0.5f - borde) / Mathf.Max(0.001f, Mathf.Abs(dir.x));
                float sy = (zona.height * 0.5f - borde) / Mathf.Max(0.001f, Mathf.Abs(dir.y));
                Vector2 pos = centroZona + dir * Mathf.Max(0f, Mathf.Min(sx, sy));

                string etiqueta = b.EsBot ? $"B{b.IndiceJugador + 1}" : $"J{b.IndiceJugador + 1}";
                DibujarIndicador(pos, dir, tam, b.ColorJugador, etiqueta);
            }
        }

        void DibujarIndicador(Vector2 pos, Vector2 dir, float tam, Color color, string etiqueta)
        {
            var matriz = GUI.matrix;

            // Flecha: un "▲" girado hacia donde está el bicho, un poco por fuera del cuadro.
            float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
            Vector2 puntaFlecha = pos + dir.normalized * tam * 0.7f;
            GUIUtility.RotateAroundPivot(angulo, puntaFlecha);
            estiloIndicador.normal.textColor = color;
            EstiloShow.Etiqueta(new Rect(puntaFlecha.x - tam * 0.5f, puntaFlecha.y - tam * 0.5f, tam, tam), "▲", estiloIndicador);
            GUI.matrix = matriz;

            var r = new Rect(pos.x - tam * 0.5f, pos.y - tam * 0.5f, tam, tam);
            EstiloShow.Rectangulo(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), new Color(0f, 0f, 0f, 0.55f));
            EstiloShow.Rectangulo(r, color);

            estiloIndicador.normal.textColor = Color.black;
            EstiloShow.Etiqueta(r, etiqueta, estiloIndicador);
        }
    }
}
