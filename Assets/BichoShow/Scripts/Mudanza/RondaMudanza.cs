using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Prueba "Mudanza frágil": dos equipos llevan objetos frágiles de su camión a su casa.
    /// Cada entrega suma puntos según lo entero que llegue. Si se rompe, sale otro del camión.
    ///
    /// Equipos: jugadores 1 y 3 son el equipo 0; jugadores 2 y 4, el equipo 1.
    /// Estados: Esperando (unirse y practicar) → CuentaAtras → Jugando → Fin → (START) otra vez.
    /// </summary>
    public class RondaMudanza : MonoBehaviour
    {
        [System.Serializable]
        public class Equipo
        {
            public string nombre = "Rojo";
            public Color color = Color.red;
            [Tooltip("Donde aparece cada objeto nuevo (en la puerta del camión).")]
            public Transform aparicionObjeto;
            public ZonaEntrega casa;
            [Tooltip("Puntos que siguen los bots con el objeto, en orden desde el camión hasta la casa.")]
            public Transform[] rutaBots;

            [HideInInspector] public int puntos;
            [HideInInspector] public int entregas;
            [HideInInspector] public ObjetoFragil objetoActual;
            [HideInInspector] public float siguienteObjetoEn = -1f;
        }

        public enum Estado { Esperando, CuentaAtras, Jugando, Fin }

        [Header("Ronda")]
        public float duracionRonda = 90f;
        public float duracionCuentaAtras = 3f;
        [Tooltip("Espera antes de que salga un objeto nuevo tras entregar o romper (s).")]
        public float esperaNuevoObjeto = 1.2f;
        [Tooltip("Puntos mínimos por entregar algo, aunque llegue hecho polvo.")]
        public int puntosMinimos = 10;

        [Header("Bots")]
        [Tooltip("Al empezar, rellenar con bots los huecos hasta llegar a 'Jugadores Con Bots'.")]
        public bool rellenarConBots = true;
        [Range(2, 4)] public int jugadoresConBots = 4;
        [Range(0f, 1f)] public float habilidadBots = 0.55f;
        public GestorJugadores gestor;

        [Header("Equipos y objetos")]
        public Equipo[] equipos = new Equipo[2];
        [Tooltip("Orden de objetos que le toca a cada equipo. Al romperse, se repite el mismo.")]
        public ObjetoFragil[] secuenciaObjetos;

        public Estado EstadoActual { get; private set; } = Estado.Esperando;
        public float TiempoRestante { get; private set; }

        float finCuentaAtras;
        readonly List<(string texto, Color color, float hasta)> mensajes = new List<(string, Color, float)>();
        GUIStyle estiloGrande, estiloMedio, estiloPequeno;

        void Start()
        {
            if (gestor == null) gestor = FindAnyObjectByType<GestorJugadores>();
            foreach (var e in equipos)
                if (e.casa != null) e.casa.AlEntrarObjeto += AlEntrarObjeto;
        }

        void OnDestroy()
        {
            foreach (var e in equipos)
                if (e != null && e.casa != null) e.casa.AlEntrarObjeto -= AlEntrarObjeto;
        }

        public static int EquipoDe(ControladorBicho bicho) => bicho.IndiceJugador % 2;

        int JugadoresEn(int equipo)
        {
            int n = 0;
            foreach (var b in ControladorBicho.Todos)
                if (EquipoDe(b) == equipo) n++;
            return n;
        }

        // ---------------------------------------------------------------------
        // Bucle de la ronda
        // ---------------------------------------------------------------------

        void Update()
        {
            switch (EstadoActual)
            {
                case Estado.Esperando:
                case Estado.Fin:
                    if (ControladorBicho.Todos.Count > 0 && AlguienPulsoEmpezar())
                        EmpezarCuentaAtras();
                    break;

                case Estado.CuentaAtras:
                    if (Time.time >= finCuentaAtras) EmpezarRonda();
                    break;

                case Estado.Jugando:
                    TiempoRestante -= Time.deltaTime;
                    for (int i = 0; i < equipos.Length; i++)
                    {
                        var e = equipos[i];
                        if (e.objetoActual == null && e.siguienteObjetoEn >= 0f && Time.time >= e.siguienteObjetoEn)
                            CrearObjeto(i);
                    }
                    if (TiempoRestante <= 0f) TerminarRonda();
                    break;
            }

            mensajes.RemoveAll(m => Time.time > m.hasta);
        }

        static bool AlguienPulsoEmpezar()
        {
            foreach (var entrada in PlayerInput.all)
            {
                var accion = entrada.actions != null ? entrada.actions.FindAction("Empezar") : null;
                if (accion != null && accion.WasPressedThisFrame()) return true;
            }
            return false;
        }

        void EmpezarCuentaAtras()
        {
            EstadoActual = Estado.CuentaAtras;
            finCuentaAtras = Time.time + duracionCuentaAtras;

            foreach (var e in equipos)
            {
                if (e.objetoActual != null) Destroy(e.objetoActual.gameObject);
                e.objetoActual = null;
                e.puntos = 0;
                e.entregas = 0;
                e.siguienteObjetoEn = -1f;
            }

            RellenarConBots();
            foreach (var b in ControladorBicho.Todos) b.Reaparecer();
        }

        void RellenarConBots()
        {
            if (gestor == null) return;
            gestor.EliminarBots();
            if (!rellenarConBots)
            {
                gestor.PermitirUnirse(true);
                return;
            }

            // Los humanos ocupan los primeros números; los bots, el resto.
            int humanos = gestor.NumeroHumanos;
            for (int i = humanos; i < jugadoresConBots; i++)
            {
                var bot = gestor.CrearBot<BotMudanza>(i);
                if (bot != null) bot.Configurar(this, habilidadBots);
            }

            // Con bots en la partida, nadie más puede unirse hasta el final (le tocaría su número).
            gestor.PermitirUnirse(humanos >= jugadoresConBots);
        }

        void EmpezarRonda()
        {
            EstadoActual = Estado.Jugando;
            TiempoRestante = duracionRonda;
            for (int i = 0; i < equipos.Length; i++)
                if (JugadoresEn(i) > 0) CrearObjeto(i);
            Mensaje("¡A MUDARSE!", Color.white, 1.5f);
        }

        void TerminarRonda()
        {
            EstadoActual = Estado.Fin;
            TiempoRestante = 0f;
            foreach (var e in equipos) e.siguienteObjetoEn = -1f;

            // Se puede volver a unir gente; los bots se rehacen al empezar la siguiente ronda.
            if (gestor != null) gestor.PermitirUnirse(true);
        }

        // ---------------------------------------------------------------------
        // Objetos
        // ---------------------------------------------------------------------

        void CrearObjeto(int indiceEquipo)
        {
            var e = equipos[indiceEquipo];
            e.siguienteObjetoEn = -1f;
            if (secuenciaObjetos == null || secuenciaObjetos.Length == 0 || e.aparicionObjeto == null) return;

            var prefab = secuenciaObjetos[e.entregas % secuenciaObjetos.Length];
            var objeto = Instantiate(prefab, e.aparicionObjeto.position, e.aparicionObjeto.rotation);
            objeto.AsignarEquipo(indiceEquipo, e.color);
            objeto.AlRomperse += (o, motivo) => AlRomperse(indiceEquipo, o, motivo);
            e.objetoActual = objeto;
        }

        void AlEntrarObjeto(ZonaEntrega zona, ObjetoFragil objeto)
        {
            if (EstadoActual != Estado.Jugando) return;
            int i = objeto.Equipo;
            if (i < 0 || i >= equipos.Length) return;
            var e = equipos[i];

            int puntos = Mathf.Max(puntosMinimos, Mathf.CeilToInt(objeto.puntosBase * objeto.Integridad / 100f));
            e.puntos += puntos;
            e.entregas++;
            string calidad = objeto.Integridad >= 95f ? "¡PERFECTO!" : objeto.Integridad >= 60f ? "¡Bien!" : objeto.Integridad >= 25f ? "Regulín..." : "¿Eso es una tele?";
            Mensaje($"Equipo {e.nombre} entrega {objeto.nombreObjeto} ({Mathf.CeilToInt(objeto.Integridad)}%)  +{puntos}  {calidad}", e.color, 3f);

            zona.Celebrar();
            objeto.Entregar();
            if (e.objetoActual == objeto) e.objetoActual = null;
            e.siguienteObjetoEn = Time.time + esperaNuevoObjeto;
        }

        void AlRomperse(int indiceEquipo, ObjetoFragil objeto, string motivo)
        {
            var e = equipos[indiceEquipo];
            if (e.objetoActual == objeto) e.objetoActual = null;
            if (EstadoActual != Estado.Jugando) return;

            Mensaje($"¡CRASH! {Mayuscula(objeto.nombreObjeto)} del equipo {e.nombre} {motivo}", e.color, 3f);
            e.siguienteObjetoEn = Time.time + esperaNuevoObjeto;
        }

        void Mensaje(string texto, Color color, float duracion) => mensajes.Add((texto, color, Time.time + duracion));

        static string Mayuscula(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        // ---------------------------------------------------------------------
        // Marcador en pantalla (provisional, con OnGUI)
        // ---------------------------------------------------------------------

        void OnGUI()
        {
            PrepararEstilos();
            float w = Screen.width, h = Screen.height;

            switch (EstadoActual)
            {
                case Estado.Esperando:
                    Texto(new Rect(0, h * 0.12f, w, h * 0.1f), "MUDANZA FRÁGIL", estiloGrande, Color.white);
                    Texto(new Rect(0, h * 0.23f, w, h * 0.05f), "Llevad cada objeto de vuestro camión a vuestra casa… sin romperlo", estiloPequeno, Color.white);
                    Texto(new Rect(0, h * 0.30f, w, h * 0.05f), "Pulsa cualquier botón para unirte   ·   Jugadores 1 y 3: equipo " + equipos[0].nombre + "   ·   2 y 4: equipo " + equipos[1].nombre, estiloPequeno, new Color(1f, 1f, 1f, 0.85f));
                    if (rellenarConBots && ControladorBicho.Todos.Count < jugadoresConBots)
                        Texto(new Rect(0, h * 0.35f, w, h * 0.05f), $"Los huecos hasta {jugadoresConBots} jugadores los ocupará la máquina", estiloPequeno, new Color(0.8f, 0.95f, 1f, 0.9f));
                    if (ControladorBicho.Todos.Count > 0)
                        Texto(new Rect(0, h * 0.78f, w, h * 0.07f), "START / ENTER para empezar", estiloMedio, Parpadeo(Color.yellow));
                    Texto(new Rect(0, h * 0.88f, w, h * 0.05f), "Stick mover · A saltar · Gatillo agarrar · X lanzarse / lanzar lo agarrado", estiloPequeno, new Color(1f, 1f, 1f, 0.7f));
                    break;

                case Estado.CuentaAtras:
                    int n = Mathf.CeilToInt(finCuentaAtras - Time.time);
                    float pulso = 1f - Mathf.Repeat(finCuentaAtras - Time.time, 1f);
                    Texto(new Rect(0, h * 0.3f, w, h * 0.25f), n.ToString(), estiloGrande, new Color(1f, 1f, 1f, 1f - pulso * 0.6f));
                    break;

                case Estado.Jugando:
                    DibujarMarcador(w, h);
                    break;

                case Estado.Fin:
                    DibujarMarcador(w, h);
                    var ganador = equipos[0].puntos == equipos[1].puntos ? null : equipos[0].puntos > equipos[1].puntos ? equipos[0] : equipos[1];
                    Texto(new Rect(0, h * 0.28f, w, h * 0.12f), ganador == null ? "¡EMPATE!" : $"¡GANA EL EQUIPO {ganador.nombre.ToUpper()}!", estiloGrande, ganador == null ? Color.white : ganador.color);
                    Texto(new Rect(0, h * 0.42f, w, h * 0.06f), $"{equipos[0].nombre} {equipos[0].puntos}  —  {equipos[1].puntos} {equipos[1].nombre}", estiloMedio, Color.white);
                    Texto(new Rect(0, h * 0.78f, w, h * 0.07f), "START / ENTER para otra ronda", estiloMedio, Parpadeo(Color.yellow));
                    break;
            }

            // Mensajes recientes, apilados bajo el reloj
            float y = h * 0.14f;
            for (int i = mensajes.Count - 1; i >= 0 && i >= mensajes.Count - 4; i--)
            {
                float vida = Mathf.Clamp01((mensajes[i].hasta - Time.time) / 0.4f);
                var c = mensajes[i].color; c.a = vida;
                Texto(new Rect(0, y, w, h * 0.05f), mensajes[i].texto, estiloPequeno, c);
                y += h * 0.045f;
            }
        }

        void DibujarMarcador(float w, float h)
        {
            // Reloj
            int segundos = Mathf.CeilToInt(Mathf.Max(0f, TiempoRestante));
            bool urgente = EstadoActual == Estado.Jugando && segundos <= 10;
            Color colorReloj = urgente ? Parpadeo(new Color(1f, 0.35f, 0.3f)) : Color.white;
            Texto(new Rect(0, h * 0.02f, w, h * 0.1f), $"{segundos / 60}:{segundos % 60:00}", estiloGrande, colorReloj);

            // Equipos a izquierda y derecha
            for (int i = 0; i < equipos.Length && i < 2; i++)
            {
                var e = equipos[i];
                float x = i == 0 ? w * 0.03f : w * 0.67f;
                var anclaje = i == 0 ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
                var r = new Rect(x, h * 0.03f, w * 0.3f, h * 0.06f);

                estiloMedio.alignment = anclaje;
                Texto(r, $"{e.nombre.ToUpper()}  {e.puntos}", estiloMedio, e.color);
                estiloMedio.alignment = TextAnchor.MiddleCenter;

                if (JugadoresEn(i) == 0)
                {
                    estiloPequeno.alignment = anclaje;
                    Texto(new Rect(x, h * 0.09f, w * 0.3f, h * 0.04f), "sin jugadores", estiloPequeno, new Color(1f, 1f, 1f, 0.5f));
                    estiloPequeno.alignment = TextAnchor.MiddleCenter;
                    continue;
                }

                var objeto = e.objetoActual;
                if (objeto == null) continue;
                float integridad = objeto.Integridad / 100f;
                var barra = new Rect(x, h * 0.095f, w * 0.3f, h * 0.025f);
                Barra(barra, integridad, Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(0.4f, 0.95f, 0.45f), integridad));
                estiloPequeno.alignment = anclaje;
                Texto(new Rect(x, h * 0.125f, w * 0.3f, h * 0.04f), $"{objeto.nombreObjeto}  {Mathf.CeilToInt(objeto.Integridad)}%", estiloPequeno, Color.white);
                estiloPequeno.alignment = TextAnchor.MiddleCenter;
            }
        }

        void PrepararEstilos()
        {
            int tamano = Mathf.Max(12, Screen.height / 28);
            if (estiloGrande == null)
            {
                estiloGrande = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
                estiloMedio = new GUIStyle(estiloGrande);
                estiloPequeno = new GUIStyle(estiloGrande) { fontStyle = FontStyle.Normal };
            }
            estiloGrande.fontSize = tamano * 3;
            estiloMedio.fontSize = Mathf.RoundToInt(tamano * 1.3f);
            estiloPequeno.fontSize = Mathf.RoundToInt(tamano * 0.8f);
        }

        static void Texto(Rect r, string texto, GUIStyle estilo, Color color)
        {
            var sombra = new Color(0f, 0f, 0f, 0.65f * color.a);
            estilo.normal.textColor = sombra;
            float d = Mathf.Max(1f, estilo.fontSize * 0.06f);
            GUI.Label(new Rect(r.x + d, r.y + d, r.width, r.height), texto, estilo);
            estilo.normal.textColor = color;
            GUI.Label(r, texto, estilo);
        }

        static void Barra(Rect r, float valor, Color color)
        {
            var antes = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(valor), r.height - 4), Texture2D.whiteTexture);
            GUI.color = antes;
        }

        static Color Parpadeo(Color c) => Mathf.Repeat(Time.time * 2f, 1f) < 0.6f ? c : new Color(c.r, c.g, c.b, 0.35f);
    }
}
