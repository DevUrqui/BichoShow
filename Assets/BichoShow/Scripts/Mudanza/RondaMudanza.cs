using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Prueba "Mudanza frágil": dos equipos llevan objetos frágiles de su camión a su casa.
    /// Cada entrega suma puntos según lo entero que llegue. Si se rompe, sale otro del camión.
    /// Los saboteadores (de RondaBase) ganan puntos por el daño y las roturas que provoquen.
    /// </summary>
    public class RondaMudanza : RondaBase
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
            [HideInInspector] public float progresoMax; // 0 = en el camión, 1 = en la puerta de casa
        }

        [Header("Mudanza")]
        [Tooltip("Espera antes de que salga un objeto nuevo tras entregar o romper (s).")]
        public float esperaNuevoObjeto = 1.2f;
        [Tooltip("Puntos mínimos por entregar algo, aunque llegue hecho polvo.")]
        public int puntosMinimos = 10;
        [Tooltip("Puntos para el saboteador cuando su trampa rompe un objeto.")]
        public int puntosRotura = 40;
        [Tooltip("Si un objeto se rompe (o se acaba el tiempo) a medio camino, el equipo gana esta fracción de sus puntos multiplicada por lo que avanzó.")]
        [Range(0f, 1f)] public float fraccionEsfuerzo = 0.4f;
        [Tooltip("La mudanza dura al menos esto, aunque el show pida rondas más cortas (s): el recorrido es largo.")]
        public float duracionMinima = 150f;

        [Header("Equipos y objetos")]
        public Equipo[] equipos = new Equipo[2];
        [Tooltip("Orden de objetos que le toca a cada equipo. Al romperse, se repite el mismo.")]
        public ObjetoFragil[] secuenciaObjetos;

        public override string Titulo => "MUDANZA FRÁGIL";
        protected override string Descripcion => "Llevad cada objeto de vuestro camión a vuestra casa… sin romperlo";
        protected override string ConsejoInicio => "Mantén el GATILLO (o E) para agarrar · ¡Llevadlo entre dos y sin golpes!";
        public override string NombreEquipo(int equipo) => equipos[equipo].nombre;
        public override Color ColorEquipo(int equipo) => equipos[equipo].color;
        public override int PuntosEquipo(int equipo) => equipos[equipo].puntos;

        public override IEnumerable<Vector3> ObjetivosSabotaje()
        {
            foreach (var e in equipos)
                if (e.objetoActual != null) yield return e.objetoActual.transform.position;
        }

        protected override void Start()
        {
            base.Start();
            foreach (var e in equipos)
                if (e.casa != null) e.casa.AlEntrarObjeto += AlEntrarObjeto;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var e in equipos)
                if (e != null && e.casa != null) e.casa.AlEntrarObjeto -= AlEntrarObjeto;
        }

        // ---------------------------------------------------------------------
        // Ciclo de la ronda
        // ---------------------------------------------------------------------

        protected override void AlPrepararRonda()
        {
            foreach (var e in equipos)
            {
                if (e.objetoActual != null) Destroy(e.objetoActual.gameObject);
                e.objetoActual = null;
                e.puntos = 0;
                e.entregas = 0;
                e.siguienteObjetoEn = -1f;
            }
        }

        protected override void CrearBot(int indice)
        {
            var bot = gestor.CrearBot<BotMudanza>(indice);
            if (bot != null) bot.Configurar(this, habilidadBots);
        }

        protected override IEntradaBicho PonerCerebroBot(GameObject bicho)
        {
            var bot = bicho.AddComponent<BotMudanza>();
            bot.Configurar(this, habilidadBots);
            return bot;
        }

        protected override void AlEmpezarRonda()
        {
            TiempoRestante = Mathf.Max(TiempoRestante, duracionMinima);
            for (int i = 0; i < equipos.Length; i++)
                if (JugadoresEn(i) > 0) CrearObjeto(i);
            Mensaje("¡A MUDARSE!", Color.white, 1.5f);
        }

        protected override void ActualizarJugando()
        {
            for (int i = 0; i < equipos.Length; i++)
            {
                var e = equipos[i];
                if (e.objetoActual == null && e.siguienteObjetoEn >= 0f && Time.time >= e.siguienteObjetoEn)
                    CrearObjeto(i);
                if (e.objetoActual != null) e.progresoMax = Mathf.Max(e.progresoMax, Progreso(e, e.objetoActual));
            }
        }

        protected override void AlTerminarRonda()
        {
            foreach (var e in equipos)
            {
                e.siguienteObjetoEn = -1f;
                // Lo que estaba en camino cuando sonó la bocina también cuenta un poco.
                if (e.objetoActual == null) continue;
                int esfuerzo = PuntosEsfuerzo(e, e.objetoActual);
                if (esfuerzo <= 0) continue;
                e.puntos += esfuerzo;
                Mensaje($"Equipo {e.nombre}: +{esfuerzo} por {e.objetoActual.nombreObjeto} a medio camino", e.color, 3f);
            }
        }

        /// <summary>Lo avanzado con un objeto: 0 en el camión, 1 en la puerta de casa.</summary>
        static float Progreso(Equipo e, ObjetoFragil objeto)
        {
            if (e.casa == null || e.aparicionObjeto == null || objeto == null) return 0f;
            Vector3 casa = e.casa.transform.position;
            float total = Plano(casa - e.aparicionObjeto.position);
            if (total < 0.1f) return 0f;
            return Mathf.Clamp01(1f - Plano(casa - objeto.transform.position) / total);
        }

        static float Plano(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        /// <summary>Puntos de consolación por lo que se avanzó con un objeto que no llegó.</summary>
        int PuntosEsfuerzo(Equipo e, ObjetoFragil objeto)
        {
            float progreso = Mathf.Max(e.progresoMax, Progreso(e, objeto));
            if (progreso < 0.15f) return 0;
            return Mathf.CeilToInt(objeto.puntosBase * fraccionEsfuerzo * progreso);
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
            // Si falta alguno en la lista, se usa el siguiente que sí esté.
            for (int i = 1; prefab == null && i < secuenciaObjetos.Length; i++)
                prefab = secuenciaObjetos[(e.entregas + i) % secuenciaObjetos.Length];
            if (prefab == null)
            {
                Debug.LogError("Bicho Show: la Mudanza no tiene objetos que sacar (secuenciaObjetos vacía). Regenera la escena.");
                return;
            }
            var objeto = Instantiate(prefab, e.aparicionObjeto.position, e.aparicionObjeto.rotation);
            objeto.AsignarEquipo(indiceEquipo, e.color);
            objeto.AlRomperse += (o, motivo) => AlRomperse(indiceEquipo, o, motivo);
            objeto.AlRecibirDano += AlDanarObjeto;
            e.objetoActual = objeto;
            e.progresoMax = 0f;
        }

        void AlDanarObjeto(ObjetoFragil objeto, float dano)
        {
            if (EstadoActual != Estado.Jugando) return;
            var s = Responsable(objeto.transform.position);
            if (s != null) s.SumarPuntos(Mathf.CeilToInt(dano), "de daño");
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

            var culpable = Responsable(objeto.transform.position);
            string autoria = "";
            if (culpable != null)
            {
                culpable.SumarPuntos(puntosRotura, "¡ROTO!");
                autoria = $"  (obra de {culpable.Etiqueta})";
            }
            int esfuerzo = PuntosEsfuerzo(e, objeto);
            e.puntos += esfuerzo;
            string consuelo = esfuerzo > 0 ? $"   (+{esfuerzo} por el camino recorrido)" : "";
            Mensaje($"¡CRASH! {Mayuscula(objeto.nombreObjeto)} del equipo {e.nombre} {motivo}{autoria}{consuelo}", e.color, 3f);
            e.siguienteObjetoEn = Time.time + esperaNuevoObjeto;
        }

        // ---------------------------------------------------------------------
        // Pantalla
        // ---------------------------------------------------------------------

        protected override void DibujarInfoEquipo(int equipo, float x, float ancho, float h)
        {
            var objeto = equipos[equipo].objetoActual;
            if (objeto == null) return;
            float integridad = objeto.Integridad / 100f;
            Barra(new Rect(x, h * 0.095f, ancho, h * 0.025f), integridad,
                  Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(0.4f, 0.95f, 0.45f), integridad));
            Texto(new Rect(x, h * 0.125f, ancho, h * 0.04f), $"{objeto.nombreObjeto}  {Mathf.CeilToInt(objeto.Integridad)}%", estiloPequeno, Color.white);
        }
    }
}
