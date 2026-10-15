using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Prueba "Mudanza frágil": dos equipos llevan objetos frágiles de su camión a su casa.
    /// Cada entrega suma puntos según lo entero que llegue. Si se rompe, sale otro del camión.
    /// Quien esté en la plataforma morada al empezar hace de saboteador y activa las trampas.
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

        [Header("Saboteadores")]
        [Tooltip("Quien esté dentro de esta zona al pulsar START será saboteador esa ronda.")]
        public Collider zonaSaboteadores;
        [Tooltip("Si nadie quiere sabotear, la máquina activa trampas.")]
        public bool saboteadorMaquina = true;
        [Range(0f, 1f)] public float agresividadMaquina = 0.5f;
        [Tooltip("Puntos para el saboteador cuando su trampa rompe un objeto.")]
        public int puntosRotura = 40;
        [Tooltip("Puntos para el saboteador por cada bicho que su trampa tira al suelo.")]
        public int puntosDerribo = 5;
        [Tooltip("Lo que pase hasta estos segundos después de activar una trampa cuenta para el saboteador.")]
        public float segundosAtribucion = 4f;

        [Header("Equipos y objetos")]
        public Equipo[] equipos = new Equipo[2];
        [Tooltip("Orden de objetos que le toca a cada equipo. Al romperse, se repite el mismo.")]
        public ObjetoFragil[] secuenciaObjetos;

        readonly List<Saboteador> saboteadores = new List<Saboteador>();

        protected override string Titulo => "MUDANZA FRÁGIL";
        protected override string Descripcion => "Llevad cada objeto de vuestro camión a vuestra casa… sin romperlo";
        public override string NombreEquipo(int equipo) => equipos[equipo].nombre;
        public override Color ColorEquipo(int equipo) => equipos[equipo].color;
        public override int PuntosEquipo(int equipo) => equipos[equipo].puntos;

        protected override void Start()
        {
            base.Start();
            foreach (var e in equipos)
                if (e.casa != null) e.casa.AlEntrarObjeto += AlEntrarObjeto;
            ControladorBicho.AlgunoDerribado += AlDerribar;
        }

        void OnDestroy()
        {
            foreach (var e in equipos)
                if (e != null && e.casa != null) e.casa.AlEntrarObjeto -= AlEntrarObjeto;
            ControladorBicho.AlgunoDerribado -= AlDerribar;
            TrampaSaboteable.ModoManual = false;
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
            PrepararSaboteadores();
        }

        protected override void CrearBot(int indice)
        {
            var bot = gestor.CrearBot<BotMudanza>(indice);
            if (bot != null) bot.Configurar(this, habilidadBots);
        }

        protected override void AlDespuesDeBots()
        {
            if (saboteadores.Count == 0 && saboteadorMaquina) CrearSaboteadorMaquina();
            TrampaSaboteable.ModoManual = saboteadores.Count > 0;
        }

        protected override bool BloquearUniones => saboteadores.Count > 0;

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

            // Los saboteadores humanos recuperan su bicho (su marcador sigue en pantalla).
            TrampaSaboteable.ModoManual = false;
            foreach (var s in saboteadores)
            {
                if (s == null || s.EsMaquina) continue;
                var bicho = s.GetComponent<ControladorBicho>();
                if (bicho != null) bicho.Retirar(false);
            }
        }

        // ---------------------------------------------------------------------
        // Saboteadores
        // ---------------------------------------------------------------------

        bool EnZonaSaboteadores(ControladorBicho b) =>
            zonaSaboteadores != null && zonaSaboteadores.bounds.Contains(b.transform.position + Vector3.up * 0.5f);

        /// <summary>Quita los saboteadores de la ronda anterior y convierte en saboteador a quien esté en la plataforma.</summary>
        void PrepararSaboteadores()
        {
            foreach (var s in saboteadores)
            {
                if (s == null) continue;
                if (s.EsMaquina) Destroy(s.gameObject);
                else
                {
                    var bichoAnterior = s.GetComponent<ControladorBicho>();
                    if (bichoAnterior != null) bichoAnterior.Retirar(false);
                    Destroy(s);
                }
            }
            saboteadores.Clear();

            var elegidos = new List<ControladorBicho>();
            foreach (var b in ControladorBicho.Todos)
                if (!b.EsBot && EnZonaSaboteadores(b)) elegidos.Add(b);

            foreach (var b in elegidos)
            {
                var s = b.gameObject.AddComponent<Saboteador>();
                s.Configurar(this, b.GetComponent<PlayerInput>(), b.ColorJugador, $"Saboteador J{b.IndiceJugador + 1}",
                             agresividadMaquina, saboteadores.Count);
                b.Retirar(true);
                saboteadores.Add(s);
            }
        }

        void CrearSaboteadorMaquina()
        {
            var go = new GameObject("SaboteadorMaquina");
            var s = go.AddComponent<Saboteador>();
            s.Configurar(this, null, new Color(0.8f, 0.45f, 1f), "Saboteador máquina", agresividadMaquina, 0);
            saboteadores.Add(s);
        }

        /// <summary>El saboteador cuya trampa, activada hace poco, tiene este punto dentro de su alcance.</summary>
        Saboteador Responsable(Vector3 punto)
        {
            Saboteador mejor = null;
            float mejorDistancia = float.MaxValue;
            foreach (var t in TrampaSaboteable.Todas)
            {
                if (t.UltimoSaboteador == null || Time.time - t.UltimoUso > segundosAtribucion) continue;
                Vector3 d = t.CentroEfecto - punto;
                d.y = 0f;
                float distancia = d.magnitude;
                if (distancia < t.radioEfecto && distancia < mejorDistancia)
                {
                    mejorDistancia = distancia;
                    mejor = t.UltimoSaboteador;
                }
            }
            return mejor;
        }

        void AlDanarObjeto(ObjetoFragil objeto, float dano)
        {
            if (EstadoActual != Estado.Jugando) return;
            var s = Responsable(objeto.transform.position);
            if (s != null) s.SumarPuntos(Mathf.CeilToInt(dano), "de daño");
        }

        void AlDerribar(ControladorBicho bicho)
        {
            if (EstadoActual != Estado.Jugando) return;
            var s = Responsable(bicho.transform.position);
            if (s != null) s.SumarPuntos(puntosDerribo, "¡al suelo!");
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

        protected override void DibujarEsperandoExtra(float w, float h)
        {
            if (zonaSaboteadores == null) return;
            string enPlataforma = "";
            foreach (var b in ControladorBicho.Todos)
                if (!b.EsBot && EnZonaSaboteadores(b)) enPlataforma += $" J{b.IndiceJugador + 1}";
            string linea = enPlataforma.Length > 0
                ? $"Serán SABOTEADORES:{enPlataforma}"
                : "Súbete a la plataforma morada entre los camiones para ser SABOTEADOR";
            Texto(new Rect(0, h * 0.40f, w, h * 0.05f), linea, estiloPequeno, new Color(0.85f, 0.6f, 1f));
        }

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
