using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Pantalla dividida dinámica para jugar varios en el mismo PC.
    /// Mientras los humanos caben en una cámara, pantalla completa. Si se separan tanto que
    /// ya no caben, la pantalla se parte en dos mitades (izquierda / derecha) y cada una sigue
    /// a su grupo. Al juntarse, vuelve a ser una sola.
    ///
    /// Lo añade solo CamaraGrupo (con "Pantalla Dividida" activado). Crea una segunda cámara
    /// copiando la principal.
    /// </summary>
    [DefaultExecutionOrder(-50)] // decide los grupos antes de que las cámaras se muevan
    [RequireComponent(typeof(CamaraGrupo))]
    public class DirectorCamaras : MonoBehaviour
    {
        [Tooltip("Se divide cuando, para que quepan todos, haría falta esta fracción de la distancia máxima.")]
        [Range(0.5f, 1.5f)] public float factorDividir = 0.9f;
        [Tooltip("Se vuelve a unir cuando bastaría con esta fracción (más baja, para que no parpadee).")]
        [Range(0.3f, 1f)] public float factorUnir = 0.65f;
        [Tooltip("Duración de la animación de partir / unir (s).")]
        public float duracionTransicion = 0.35f;
        [Tooltip("Grosor de la línea que separa las dos mitades (píxeles).")]
        public float grosorLinea = 6f;

        CamaraGrupo principal, secundaria;
        Camera camPrincipal, camSecundaria;

        bool dividido;
        float division = 1f; // dónde está la línea: 1 = pantalla completa, 0.5 = mitades
        readonly List<ControladorBicho> humanos = new List<ControladorBicho>();
        readonly List<ControladorBicho> grupoIzq = new List<ControladorBicho>();
        readonly List<ControladorBicho> grupoDer = new List<ControladorBicho>();
        readonly List<Vector3> puntos = new List<Vector3>();

        void Awake()
        {
            principal = GetComponent<CamaraGrupo>();
            camPrincipal = GetComponent<Camera>();
            CrearSecundaria();
        }

        void OnDestroy()
        {
            if (secundaria != null) Destroy(secundaria.gameObject);
        }

        void CrearSecundaria()
        {
            var go = new GameObject($"{name} (mitad derecha)");
            camSecundaria = go.AddComponent<Camera>();
            camSecundaria.CopyFrom(camPrincipal);
            secundaria = go.AddComponent<CamaraGrupo>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(principal), secundaria);
            secundaria.pantallaDividida = false; // que no cree otra a su vez
            secundaria.objetivosForzados = grupoDer;
            camSecundaria.enabled = false;
            secundaria.enabled = false;
        }

        void LateUpdate()
        {
            humanos.Clear();
            foreach (var b in ControladorBicho.Todos)
                if (!b.EsBot) humanos.Add(b);

            if (humanos.Count < 2)
            {
                dividido = false;
            }
            else if (!dividido)
            {
                if (DistanciaParaTodos() > principal.distanciaMaxima * factorDividir) Dividir();
            }
            else
            {
                MantenerGrupos();
                if (DistanciaParaTodos() < principal.distanciaMaxima * factorUnir) dividido = false;
            }

            AplicarDivision();
        }

        float DistanciaParaTodos()
        {
            puntos.Clear();
            foreach (var h in humanos) puntos.Add(CamaraGrupo.PuntoDe(h));
            float aspectoCompleto = Screen.width / (float)Mathf.Max(1, Screen.height);
            principal.Encuadre(puntos, aspectoCompleto, null, out _, out _, out float distancia);
            return distancia;
        }

        /// <summary>
        /// Reparte a los humanos en dos grupos: se buscan los dos más alejados entre sí y cada
        /// uno va con el más cercano de esos dos. El grupo más a la izquierda, a la mitad izquierda.
        /// </summary>
        void Dividir()
        {
            ControladorBicho a = humanos[0], b = humanos[1];
            float mayor = -1f;
            for (int i = 0; i < humanos.Count; i++)
            for (int j = i + 1; j < humanos.Count; j++)
            {
                float d = (humanos[i].transform.position - humanos[j].transform.position).sqrMagnitude;
                if (d > mayor) { mayor = d; a = humanos[i]; b = humanos[j]; }
            }

            grupoIzq.Clear();
            grupoDer.Clear();
            foreach (var h in humanos)
            {
                float da = (h.transform.position - a.transform.position).sqrMagnitude;
                float db = (h.transform.position - b.transform.position).sqrMagnitude;
                (da <= db ? grupoIzq : grupoDer).Add(h);
            }

            Vector3 derecha = principal.DerechaPantalla;
            if (Vector3.Dot(Centro(grupoIzq), derecha) > Vector3.Dot(Centro(grupoDer), derecha))
            {
                var tmp = new List<ControladorBicho>(grupoIzq);
                grupoIzq.Clear(); grupoIzq.AddRange(grupoDer);
                grupoDer.Clear(); grupoDer.AddRange(tmp);
            }

            dividido = true;
        }

        /// <summary>
        /// Una vez divididos, cada uno se queda en su mitad (para que las cámaras no se
        /// intercambien). Los que se van salen; los nuevos van al grupo más cercano.
        /// </summary>
        void MantenerGrupos()
        {
            grupoIzq.RemoveAll(h => h == null || h.EsBot || !humanos.Contains(h));
            grupoDer.RemoveAll(h => h == null || h.EsBot || !humanos.Contains(h));
            if (grupoIzq.Count == 0 || grupoDer.Count == 0)
            {
                Dividir();
                return;
            }

            foreach (var h in humanos)
            {
                if (grupoIzq.Contains(h) || grupoDer.Contains(h)) continue;
                Vector3 p = h.transform.position;
                bool izq = (p - Centro(grupoIzq)).sqrMagnitude <= (p - Centro(grupoDer)).sqrMagnitude;
                (izq ? grupoIzq : grupoDer).Add(h);
            }
        }

        static Vector3 Centro(List<ControladorBicho> grupo)
        {
            if (grupo.Count == 0) return Vector3.zero;
            Vector3 suma = Vector3.zero;
            foreach (var h in grupo) suma += h.transform.position;
            return suma / grupo.Count;
        }

        void AplicarDivision()
        {
            float objetivo = dividido ? 0.5f : 1f;
            float velocidad = 0.5f / Mathf.Max(0.01f, duracionTransicion);
            division = Mathf.MoveTowards(division, objetivo, velocidad * Time.unscaledDeltaTime);

            bool hayMitades = division < 0.999f;

            // Mientras haya mitades, la principal sigue a la izquierda; si no, a todos.
            principal.objetivosForzados = hayMitades ? grupoIzq : null;
            camPrincipal.rect = new Rect(0f, 0f, division, 1f);

            if (hayMitades && !secundaria.enabled)
            {
                secundaria.enabled = true;
                camSecundaria.enabled = true;
                camSecundaria.rect = new Rect(division, 0f, 1f - division, 1f);
                secundaria.ColocarAlInstante(Screen.width * 0.5f / Mathf.Max(1, Screen.height));
            }
            else if (!hayMitades && secundaria.enabled)
            {
                secundaria.enabled = false;
                camSecundaria.enabled = false;
            }
            camSecundaria.rect = new Rect(division, 0f, 1f - division, 1f);
        }

        void OnGUI()
        {
            if (division >= 0.999f) return;
            float x = division * Screen.width;
            var antes = GUI.color;
            GUI.color = new Color(0.08f, 0.06f, 0.12f, 1f);
            GUI.DrawTexture(new Rect(x - grosorLinea * 0.5f, 0f, grosorLinea, Screen.height), Texture2D.whiteTexture);
            GUI.color = antes;
        }
    }
}
