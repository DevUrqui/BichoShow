using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Prueba "Lluvia de muebles" (todos contra todos):
    ///  - Por oleadas, caen del cielo sofás, teles, pianos, yunques… Antes, su sombra avisa en
    ///    el suelo (crece hasta que llega).
    ///  - A quien le cae uno encima, se queda plano y sin punto en esa oleada; los demás, +1.
    ///  - Cada vez caen más y más seguido; en los últimos segundos, ¡tormenta!
    /// Lo gracioso: empujar a los demás debajo de las sombras.
    /// </summary>
    public class RondaLluvia : RondaBase
    {
        [Header("Lluvia de muebles")]
        [Tooltip("Plantillas de los muebles (apagadas en la escena): se copian al caer.")]
        public GameObject[] muebles;
        [Tooltip("Plantilla de la sombra que avisa (un disco oscuro, apagado).")]
        public GameObject sombra;
        public Transform centroArena;
        [Tooltip("Medio ancho y medio fondo del suelo.")]
        public Vector2 medioTamano = new Vector2(9.5f, 9.5f);

        [Tooltip("Segundos que avisa la sombra antes de que empiece a caer.")]
        public float aviso = 1.3f;
        public float alturaCaida = 14f;
        public float velocidadCaida = 15f;
        [Tooltip("Segundos entre oleadas: al principio y al final de la ronda.")]
        public Vector2 intervalo = new Vector2(5f, 2.8f);
        [Tooltip("Muebles por oleada: al principio y al final.")]
        public Vector2Int porOleada = new Vector2Int(2, 5);
        public float segundosTormenta = 15f;
        [Tooltip("Probabilidad de que un mueble apunte a un jugador (si no, cae al azar).")]
        [Range(0f, 1f)] public float punteria = 0.55f;

        class Caida
        {
            public Vector3 punto;
            public float radio;
            public float caeEn;      // cuándo empieza a caer
            public int oleada;
            public GameObject plantilla;
            public Transform sombra;
            public MuebleCayendo mueble;
            public bool soltado;
        }

        readonly int[] puntos = new int[4];
        readonly List<Caida> caidas = new List<Caida>();
        readonly Dictionary<int, HashSet<ControladorBicho>> aplastados = new Dictionary<int, HashSet<ControladorBicho>>();
        readonly Dictionary<int, float> resolverOleadaEn = new Dictionary<int, float>();
        readonly List<GameObject> enEscena = new List<GameObject>();
        float siguienteOleada;
        int oleada;
        bool avisoTormenta;

        public static RondaLluvia Instancia { get; private set; }

        public override string Titulo => "LLUVIA DE MUEBLES";
        protected override string Descripcion => "¡Que no te caiga nada encima! Mira las sombras del suelo… y empuja a los demás debajo.";
        protected override string ConsejoInicio => "¡Las sombras avisan! Apártate… o empuja a otro debajo";
        protected override string AccionX => "placaje (¡empújalos debajo!)";
        public override bool Individual => true;
        public override int PuntosJugador(int indice) => indice >= 0 && indice < puntos.Length ? puntos[indice] : 0;
        public override string NombreEquipo(int equipo) => equipo == 0 ? "Rojo" : "Azul";
        public override Color ColorEquipo(int equipo) => equipo == 0 ? new Color(1f, 0.38f, 0.35f) : new Color(0.3f, 0.6f, 1f);
        public override int PuntosEquipo(int equipo) => puntos[equipo] + puntos[equipo + 2];
        public Vector3 Centro => centroArena != null ? centroArena.position : transform.position;
        bool Tormenta => TiempoRestante <= segundosTormenta;

        /// <summary>Sombras activas (para que los bots huyan): punto y radio.</summary>
        public IEnumerable<(Vector3 punto, float radio)> Peligros()
        {
            foreach (var c in caidas)
                if (c.mueble == null || !c.mueble.EnElSuelo) yield return (c.punto, c.radio);
        }

        public override IEnumerable<Vector3> ObjetivosSabotaje()
        {
            foreach (var c in caidas) yield return c.punto;
        }

        void Awake() => Instancia = this;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instancia == this) Instancia = null;
        }

        protected override void CrearBot(int indice)
        {
            var bot = gestor.CrearBot<BotLluvia>(indice);
            if (bot != null) bot.Configurar(this, habilidadBots);
        }

        protected override void AlPrepararRonda()
        {
            for (int i = 0; i < puntos.Length; i++) puntos[i] = 0;
            Limpiar();
            oleada = 0;
            avisoTormenta = false;
        }

        protected override void AlEmpezarRonda()
        {
            siguienteOleada = Time.time + 1.5f;
            Mensaje("¡MIRAD ARRIBA!", Color.white, 1.5f);
        }

        protected override void ActualizarJugando()
        {
            if (Time.time >= siguienteOleada) LanzarOleada();

            if (!avisoTormenta && Tormenta)
            {
                avisoTormenta = true;
                Mensaje("¡TORMENTA DE MUEBLES!", new Color(1f, 0.6f, 0.2f), 3f);
                Sonidos.Reproducir(Sonido.Alarma);
            }

            // Sombras que crecen; al acabar el aviso, el mueble empieza a caer.
            for (int i = caidas.Count - 1; i >= 0; i--)
            {
                var c = caidas[i];
                if (!c.soltado)
                {
                    float k = Mathf.Clamp01(1f - (c.caeEn - Time.time) / aviso);
                    float latido = 1f + Mathf.Sin(Time.time * 18f) * 0.04f * k;
                    if (c.sombra != null)
                        c.sombra.localScale = new Vector3(c.radio * 2f * Mathf.Lerp(0.35f, 1f, k) * latido, 0.01f, c.radio * 2f * Mathf.Lerp(0.35f, 1f, k) * latido);
                    if (Time.time >= c.caeEn) Soltar(c);
                }
                else if (c.mueble == null || c.mueble.EnElSuelo) // aterrizó (o se perdió por ahí)
                {
                    if (c.sombra != null) Destroy(c.sombra.gameObject);
                    caidas.RemoveAt(i);
                }
            }

            // Oleadas resueltas: +1 a quien no aplastaron.
            var resueltas = new List<int>();
            foreach (var par in resolverOleadaEn)
                if (Time.time >= par.Value && !QuedanEnOleada(par.Key)) resueltas.Add(par.Key);
                else if (Time.time >= par.Value + 2.5f) resueltas.Add(par.Key);
            foreach (int ola in resueltas) ResolverOleada(ola);
        }

        protected override bool PuedeTerminar()
        {
            foreach (int ola in new List<int>(resolverOleadaEn.Keys)) ResolverOleada(ola);
            return true;
        }

        protected override void AlTerminarRonda() => Limpiar();

        // ---------------------------------------------------------------------
        // Oleadas
        // ---------------------------------------------------------------------

        void LanzarOleada()
        {
            float progreso = Mathf.Clamp01(TiempoJugado / Mathf.Max(1f, duracionRonda));
            siguienteOleada = Time.time + Mathf.Lerp(intervalo.x, intervalo.y, progreso) * (Tormenta ? 0.7f : 1f);
            if (muebles == null || muebles.Length == 0) return;

            oleada++;
            aplastados[oleada] = new HashSet<ControladorBicho>();
            int cuantos = Mathf.RoundToInt(Mathf.Lerp(porOleada.x, porOleada.y, progreso)) + (Tormenta ? 2 : 0);
            var activos = new List<ControladorBicho>();
            foreach (var b in ControladorBicho.Todos) if (!b.Retirado) activos.Add(b);

            float ultimaCaida = 0f;
            for (int i = 0; i < cuantos; i++)
            {
                var plantilla = muebles[Random.Range(0, muebles.Length)];
                float radio = Radio(plantilla);
                Vector3 punto;
                if (activos.Count > 0 && Random.value < punteria)
                {
                    var b = activos[Random.Range(0, activos.Count)];
                    Vector2 r = Random.insideUnitCircle * 1.3f;
                    punto = b.transform.position + new Vector3(r.x, 0f, r.y);
                }
                else
                {
                    punto = Centro + new Vector3(Random.Range(-medioTamano.x, medioTamano.x), 0f, Random.Range(-medioTamano.y, medioTamano.y));
                }
                Vector3 c = Centro;
                punto.x = Mathf.Clamp(punto.x, c.x - medioTamano.x + radio, c.x + medioTamano.x - radio);
                punto.z = Mathf.Clamp(punto.z, c.z - medioTamano.y + radio, c.z + medioTamano.y - radio);
                punto.y = c.y;

                float retraso = i * 0.25f; // no todos a la vez
                var caida = new Caida { punto = punto, radio = radio, caeEn = Time.time + aviso + retraso, oleada = oleada, plantilla = plantilla };
                if (sombra != null)
                {
                    var s = Instantiate(sombra, punto + Vector3.up * 0.02f, Quaternion.identity);
                    s.SetActive(true);
                    caida.sombra = s.transform;
                    enEscena.Add(s);
                }
                caidas.Add(caida);
                ultimaCaida = Mathf.Max(ultimaCaida, caida.caeEn);
            }
            resolverOleadaEn[oleada] = ultimaCaida + 1.2f;
            Sonidos.Reproducir(Sonido.Caida, Centro, 0.5f, 1.3f);
        }

        static float Radio(GameObject plantilla)
        {
            var caja = plantilla.GetComponent<BoxCollider>();
            return caja != null ? Mathf.Max(caja.size.x, caja.size.z) * 0.5f * plantilla.transform.localScale.x + 0.15f : 1f;
        }

        void Soltar(Caida c)
        {
            c.soltado = true;
            var go = Instantiate(c.plantilla, c.punto + Vector3.up * alturaCaida, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            go.name = c.plantilla.name;
            go.SetActive(true);
            enEscena.Add(go);
            c.mueble = go.GetComponent<MuebleCayendo>();
            c.mueble.Preparar(this, c.oleada, velocidadCaida);
        }

        bool QuedanEnOleada(int ola)
        {
            foreach (var c in caidas) if (c.oleada == ola) return true;
            return false;
        }

        void ResolverOleada(int ola)
        {
            resolverOleadaEn.Remove(ola);
            if (!aplastados.TryGetValue(ola, out var planos)) return;
            aplastados.Remove(ola);
            int salvados = 0;
            foreach (var b in ControladorBicho.Todos)
            {
                if (b.Retirado || planos.Contains(b)) continue;
                puntos[b.IndiceJugador]++;
                salvados++;
            }
            if (salvados > 0) Sonidos.Reproducir(Sonido.Click, Centro, 0.6f, 1.4f);
        }

        // ---------------------------------------------------------------------
        // Lo que avisan los muebles
        // ---------------------------------------------------------------------

        public void Aplastado(ControladorBicho bicho, MuebleCayendo mueble)
        {
            if (EstadoActual != Estado.Jugando) return;
            if (!aplastados.TryGetValue(mueble.Oleada, out var planos) || planos.Contains(bicho)) return;
            planos.Add(bicho);
            bicho.Tropezar(1.8f);
            if (bicho.TryGetComponent(out JugoBicho jugo)) jugo.Aplastar();
            Vector3 p = bicho.Cuerpo.worldCenterOfMass;
            Sonidos.Reproducir(Sonido.Golpe, p, 1f, 0.7f);
            Sonidos.Reproducir(Sonido.Derribo, p, 1f, 0.8f);
            Sonidos.Reproducir(Sonido.Ooh, p);
            Efectos.Impacto(p + Vector3.up * 0.5f, bicho.ColorJugador, 1f);
            Efectos.Sacudir(0.35f);
            if (!bicho.EsBot) Efectos.CamaraLenta(0.35f, 0.35f, 3f);
            string nombre = mueble.name; // las plantillas se llaman "un piano", "una tele"…
            Mensaje($"¡{NombreJugador(bicho)} aplastado por {nombre}!", gestor != null ? gestor.ColorDe(bicho.IndiceJugador) : Color.white, 2f);
            Estadisticas.Evento("aplastado", bicho, nombre);
        }

        public void Aterrizo(MuebleCayendo mueble, Vector3 punto)
        {
            Sonidos.Reproducir(Sonido.Aterrizaje, punto, 1f, 0.55f);
            Sonidos.Reproducir(Sonido.Golpe, punto, 0.6f, 0.6f);
            Efectos.Polvo(punto, 1f);
            Efectos.Sacudir(0.18f);
        }

        void Limpiar()
        {
            foreach (var go in enEscena) if (go != null) Destroy(go);
            enEscena.Clear();
            caidas.Clear();
            aplastados.Clear();
            resolverOleadaEn.Clear();
        }
    }
}
