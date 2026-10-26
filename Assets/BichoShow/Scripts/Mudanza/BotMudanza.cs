using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Bot para la Mudanza frágil. Pulsa los mismos "botones" que un jugador (a través de
    /// IEntradaBicho), así que es igual de torpe: tropieza, se cae al río y lo barren.
    ///
    /// Qué hace:
    ///  - Si no hay objeto de su equipo, espera junto al camión.
    ///  - Si nadie lo lleva, se pone detrás del objeto (mirando hacia la casa) y lo agarra.
    ///  - Si un compañero ya lo lleva, se pone a un lado y ayuda.
    ///  - Con el objeto agarrado, sigue la ruta de su equipo hasta la casa.
    ///  - Si se atasca, salta y se desvía un poco.
    /// </summary>
    public class BotMudanza : MonoBehaviour, IEntradaBicho
    {
        [Tooltip("0 = muy torpe y lento de reflejos, 1 = bastante preciso.")]
        [Range(0f, 1f)] public float habilidad = 0.6f;

        public Vector2 Mover { get; private set; }
        public bool Agarrar { get; private set; }

        RondaMudanza ronda;
        ControladorBicho bicho;
        Camera camara;

        Vector3 objetivo;
        bool quiereAgarrar;
        bool llevandoObjeto;
        bool saltarPendiente;
        float siguientePensar;
        float semillaRuido;

        // Detección de atascos
        Vector3 posicionControl;
        float siguienteControl;
        Vector3 desvio;
        float finDesvio;

        public bool ConsumirSaltar()
        {
            bool s = saltarPendiente;
            saltarPendiente = false;
            return s;
        }

        public bool ConsumirLanzarse() => false;

        public void Configurar(RondaMudanza rondaMudanza, float nivel)
        {
            ronda = rondaMudanza;
            habilidad = nivel;
        }

        void Start()
        {
            bicho = GetComponent<ControladorBicho>();
            camara = Camera.main;
            if (ronda == null) ronda = FindAnyObjectByType<RondaMudanza>();
            semillaRuido = Random.value * 100f;
            posicionControl = transform.position;
        }

        int Equipo => RondaMudanza.EquipoDe(bicho);

        void Update()
        {
            if (ronda == null || bicho == null || ronda.EstadoActual != RondaMudanza.Estado.Jugando)
            {
                Mover = Vector2.zero;
                Agarrar = false;
                return;
            }

            if (Time.time >= siguientePensar)
            {
                Pensar();
                // Reflejos: los bots torpes deciden con menos frecuencia.
                siguientePensar = Time.time + Mathf.Lerp(0.45f, 0.12f, habilidad) * Random.Range(0.8f, 1.2f);
            }

            Conducir();
            ComprobarAtasco();
        }

        // ---------------------------------------------------------------------
        // Decidir a dónde ir
        // ---------------------------------------------------------------------

        void Pensar()
        {
            var equipo = ronda.equipos[Equipo];
            var objeto = equipo.objetoActual;
            Vector3 yo = transform.position;
            llevandoObjeto = false;

            // Sin objeto: esperar junto al camión a que salga el siguiente.
            if (objeto == null || objeto.Roto || objeto.Entregado)
            {
                quiereAgarrar = false;
                objetivo = equipo.aparicionObjeto != null
                    ? equipo.aparicionObjeto.position + new Vector3(Equipo == 0 ? -1.8f : 1.8f, 0f, 1.5f)
                    : yo;
                return;
            }

            Vector3 posObjeto = objeto.transform.position;
            Rigidbody agarrado = bicho.CuerpoAgarrado;

            // Si con uno basta y ya lo lleva otro, el segundo ayuda desde la placa del tramo
            // (para la cinta, frena la puerta) en vez de llevarlo también.
            var placa = objeto.NecesariosAhora <= 1 ? PlacaUtil(equipo, posObjeto) : null;
            bool loLlevoYo = agarrado != null && agarrado == objeto.Cuerpo;
            if (placa != null && AlguienMasLoLleva(objeto) && (!loLlevoYo || SoyElUltimo(objeto)))
            {
                quiereAgarrar = false;
                objetivo = placa.transform.position;
                return;
            }

            // Llevándolo: seguir la ruta.
            if (loLlevoYo)
            {
                llevandoObjeto = true;
                quiereAgarrar = true;
                // Desde el que vaya más adelantado (yo o el objeto): si tiro yo delante y ya he
                // llegado al punto, el objeto se quedaba justo detrás y no cambiábamos de punto.
                objetivo = SiguientePuntoRuta(equipo, yo.z > posObjeto.z ? yo : posObjeto);
                return;
            }

            // Tengo agarrada otra cosa (un cono, un compañero...): soltar.
            if (bicho.Agarrando)
            {
                quiereAgarrar = false;
                objetivo = posObjeto;
                return;
            }

            // Ir a agarrarlo. Si nadie lo lleva, por detrás (empujando hacia la casa);
            // si ya lo lleva un compañero, por el lado que me quede más cerca.
            Vector3 haciaCasa = Plano(SiguientePuntoRuta(equipo, posObjeto) - posObjeto);
            haciaCasa = haciaCasa.sqrMagnitude > 0.01f ? haciaCasa.normalized : Vector3.forward;
            Vector3 lado;
            if (AlguienMasLoLleva(objeto))
            {
                Vector3 derecha = Vector3.Cross(Vector3.up, haciaCasa);
                lado = Vector3.Dot(yo - posObjeto, derecha) >= 0f ? derecha : -derecha;
            }
            else
            {
                lado = -haciaCasa;
            }

            float distancia = Plano(posObjeto - yo).magnitude;
            if (distancia < 1.7f)
            {
                objetivo = posObjeto;   // pegado: mirar al objeto y agarrar
                quiereAgarrar = true;
            }
            else
            {
                objetivo = posObjeto + lado * 1.2f;
                quiereAgarrar = false;
            }
        }

        static PlacaPresion PlacaUtil(RondaMudanza.Equipo equipo, Vector3 posObjeto)
        {
            if (equipo.ayudas == null) return null;
            foreach (var a in equipo.ayudas)
                if (a != null && a.placa != null && posObjeto.z >= a.desdeZ && posObjeto.z <= a.hastaZ) return a.placa;
            return null;
        }

        /// <summary>De los que lo llevan, soy el de número más alto (el que suelta para ayudar).</summary>
        bool SoyElUltimo(ObjetoFragil objeto)
        {
            foreach (var otro in ControladorBicho.Todos)
                if (otro != bicho && otro.CuerpoAgarrado == objeto.Cuerpo && otro.IndiceJugador > bicho.IndiceJugador) return false;
            return true;
        }

        bool AlguienMasLoLleva(ObjetoFragil objeto)
        {
            foreach (var otro in ControladorBicho.Todos)
                if (otro != bicho && otro.CuerpoAgarrado == objeto.Cuerpo) return true;
            return false;
        }

        /// <summary>
        /// La ruta va siempre hacia delante (+Z), así que basta con buscar el primer punto que
        /// esté por delante del objeto. Si no queda ninguno, a la casa.
        /// </summary>
        static Vector3 SiguientePuntoRuta(RondaMudanza.Equipo equipo, Vector3 desde)
        {
            if (equipo.rutaBots != null)
            {
                foreach (var punto in equipo.rutaBots)
                    if (punto != null && punto.position.z > desde.z + 0.75f)
                        return punto.position;
            }
            return equipo.casa != null ? equipo.casa.transform.position : desde;
        }

        // ---------------------------------------------------------------------
        // Mover el stick
        // ---------------------------------------------------------------------

        void Conducir()
        {
            if (bicho.Tumbado)
            {
                Mover = Vector2.zero;
                Agarrar = false;
                return;
            }

            Vector3 haciaObjetivo = Plano(objetivo - transform.position);
            float distancia = haciaObjetivo.magnitude;
            Vector3 direccion = distancia > 0.01f ? haciaObjetivo / distancia : Vector3.zero;

            // Un poco de "pulso tembloroso": cuanto menos hábil, más se desvía.
            float ruidoMax = Mathf.Lerp(35f, 6f, habilidad);
            float ruido = (Mathf.PerlinNoise(Time.time * 0.8f, semillaRuido) - 0.5f) * 2f * ruidoMax;
            direccion = Quaternion.Euler(0f, ruido, 0f) * direccion;

            if (Time.time < finDesvio)
                direccion = (direccion + desvio * 1.3f).normalized;

            // Intensidad: despacio al acercarse para agarrar; con el objeto, a tope.
            float intensidad = 1f;
            if (!llevandoObjeto)
            {
                if (quiereAgarrar) intensidad = 0.35f;
                else if (distancia < 0.6f) intensidad = 0f;
            }

            Mover = AStick(direccion * intensidad);
            Agarrar = quiereAgarrar;
        }

        /// <summary>Convierte una dirección del mundo en el stick, igual que la lee el bicho (relativo a la cámara).</summary>
        Vector2 AStick(Vector3 direccionMundo)
        {
            Vector3 adelante = Vector3.forward, derecha = Vector3.right;
            if (camara != null)
            {
                adelante = Plano(camara.transform.forward).normalized;
                derecha = Plano(camara.transform.right).normalized;
            }
            return new Vector2(Vector3.Dot(direccionMundo, derecha), Vector3.Dot(direccionMundo, adelante));
        }

        void ComprobarAtasco()
        {
            if (Time.time < siguienteControl) return;
            siguienteControl = Time.time + 1.2f;

            Vector3 ahora = transform.position;
            float avance = Plano(ahora - posicionControl).magnitude;
            posicionControl = ahora;

            bool deberiaMoverse = Plano(objetivo - ahora).magnitude > 1.8f && !bicho.Tumbado;
            if (deberiaMoverse && avance < 0.5f)
            {
                saltarPendiente = true;
                Vector3 haciaObjetivo = Plano(objetivo - ahora).normalized;
                desvio = Vector3.Cross(Vector3.up, haciaObjetivo) * (Random.value < 0.5f ? -1f : 1f);
                finDesvio = Time.time + 0.7f;
            }
        }

        static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = llevandoObjeto ? Color.green : quiereAgarrar ? Color.yellow : Color.cyan;
            Gizmos.DrawLine(transform.position + Vector3.up, objetivo + Vector3.up);
            Gizmos.DrawWireSphere(objetivo + Vector3.up, 0.3f);
        }
    }
}
