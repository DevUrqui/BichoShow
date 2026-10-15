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
        }

        [Header("Mudanza")]
        [Tooltip("Espera antes de que salga un objeto nuevo tras entregar o romper (s).")]
        public float esperaNuevoObjeto = 1.2f;
        [Tooltip("Puntos mínimos por entregar algo, aunque llegue hecho polvo.")]
        public int puntosMinimos = 10;
        [Tooltip("Puntos para el saboteador cuando su trampa rompe un objeto.")]
        public int puntosRotura = 40;

        [Header("Equipos y objetos")]
        public Equipo[] equipos = new Equipo[2];
        [Tooltip("Orden de objetos que le toca a cada equipo. Al romperse, se repite el mismo.")]
        public ObjetoFragil[] secuenciaObjetos;

        public override string Titulo => "MUDANZA FRÁGIL";
        protected override string Descripcion => "Llevad cada objeto de vuestro camión a vuestra casa… sin romperlo";
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

        protected override void AlEmpezarRonda()
        {
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
            }
        }

        protected override void AlTerminarRonda()
        {
            foreach (var e in equipos) e.siguienteObjetoEn = -1f;
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
            objeto.AlRecibirDano += AlDanarObjeto;
            e.objetoActual = objeto;
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
            Mensaje($"¡CRASH! {Mayuscula(objeto.nombreObjeto)} del equipo {e.nombre} {motivo}{autoria}", e.color, 3f);
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
