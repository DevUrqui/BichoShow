using System;
using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "El Cortacésped Mecha": el robot cortacésped del centro de la arena. Gira sobre sí mismo y
    /// va lanzando ataques, que anuncia antes con la cara de su pantalla LED. La pelea va por
    /// fases (las pone RondaCortacesped según lo que va de ronda):
    ///  1. Calentamiento: onda láser (aros a ras de suelo que hay que saltar), aspiración (tira
    ///     hacia el foso: hay que correr hacia fuera) y lluvia de tuercas y pilas que rebotan.
    ///  2. Cuchillas: además, el barrido de cuchillas (una cuchilla a ras de suelo que gira; hay
    ///     que saltarla) y el corte de césped (porciones del suelo que se ponen rojas, tiemblan y se
    ///     abren como una trampilla). Y
    ///     variantes: aros láser altos (te dan si saltas) y la aspiración que de golpe sopla.
    ///     También, a veces, dos ataques a la vez; la cuchilla puede pararse y cambiar de sentido.
    ///  3. Modo furia: descansos mínimos, casi siempre dos ataques a la vez, la aspiración que
    ///     sopla y vuelve a aspirar, un segundo corte de césped en las otras porciones y tuercas
    ///     de dos en dos. En el último asalto (el final de la ronda), hasta tres ataques a la vez.
    ///
    /// Villano: si el que va último maneja el robot (villano), elige entre tres cartas el
    /// siguiente ataque (X cambia de carta, A lo lanza ya) y en cada ataque tiene tres poderes
    /// con A (girar la cuchilla al revés, soplar o aspirar, soltar un aro alto, ráfaga de tuercas,
    /// otro corte) y apunta con el stick las tuercas y el corte de césped. Sin villano (o si se le
    /// corta la conexión), el robot va solo.
    /// El pivote está en el centro, a la altura del césped.
    /// </summary>
    public class CortacespedMecha : MonoBehaviour
    {
        public enum Ataque { OndaLaser, Aspiracion, LluviaTuercas, Barrido, CorteCesped }
        public enum Cara { Normal, Enfadado, Risa, Mareado, Laser, Furia, Corte }

        /// <summary>Un ataque que se puede anunciar (con su combo, si lo lleva).</summary>
        public struct Opcion
        {
            public Ataque a;
            public Ataque? b, c;
        }

        [Header("Piezas")]
        [Tooltip("Lo que gira (el robot entero).")]
        public Transform cuerpo;
        [Tooltip("Una pantalla por cara, en el orden de Cara: se enciende la que toca.")]
        public GameObject[] caras;
        [Tooltip("Aros del láser a ras de suelo (hay que saltarlos), radio 1 a escala 1, apagados.")]
        public Transform[] aros;
        [Tooltip("Aros del láser altos (te dan si saltas), apagados.")]
        public Transform[] arosAltos;
        [Tooltip("Cuchillas del barrido (pivote en el centro, la hoja hacia +X), apagadas.")]
        public Transform[] cuchillas;
        [Tooltip("Las porciones del césped (se caen en el corte), con un Rigidbody cinemático.")]
        public Rigidbody[] porciones;
        public Transform turbina;
        [Tooltip("De donde salen las tuercas (gira con el robot).")]
        public Transform boca;
        [Tooltip("Plantillas de tuercas y pilas (apagadas): se copian al escupirlas.")]
        public GameObject[] proyectiles;

        [Header("Viento (se ve de dónde viene el aire)")]
        [Tooltip("Rachas de aire (líneas finas, apagadas): van en espiral hacia el robot al aspirar y hacia fuera al soplar.")]
        public Transform[] rafagas;
        [Tooltip("Flechas en el césped (la punta hacia +Z), apagadas: se mueven con el aire.")]
        public Transform[] flechasViento;
        [Tooltip("Recortes de césped (apagados) que vuelan arrastrados por el aire, dando vueltas.")]
        public Transform[] recortes;
        [Tooltip("Remolino en espiral pintado en el suelo (gira hacia donde va el aire).")]
        public Renderer remolino;
        [Tooltip("Ciclón alrededor del robot que sube hacia la turbina.")]
        public Renderer embudo;

        [Header("Arena")]
        public float radioRobot = 4.2f;
        public float radioFoso = 7f;
        public float radioPista = 22f;

        [Header("Ritmo")]
        public float giro = 18f;
        [Tooltip("Velocidad de los aros láser (m/s): al principio y al final de la ronda.")]
        public Vector2 velocidadAro = new Vector2(9f, 14f);
        [Tooltip("Lo que arrastra la aspiración (m/s): al principio y al final.")]
        public Vector2 aspiracion = new Vector2(4f, 5.2f);
        [Tooltip("Velocidad de las cuchillas (grados/s): al principio y al final.")]
        public Vector2 velocidadCuchilla = new Vector2(85f, 140f);
        public float anuncio = 1.15f;
        [Tooltip("Lo que va de ronda (0-1) desde el que, en el modo furia, puede lanzar tres ataques a la vez.")]
        public float ultimoAsalto = 0.85f;

        /// <summary>El robot avisa de un ataque (o de dos, en el modo furia: el segundo, si no es null).</summary>
        public event Action<Ataque, Ataque?> AlAnunciar;
        /// <summary>Han terminado los ataques anunciados.</summary>
        public event Action AlTerminar;
        /// <summary>Algo que contar a mitad de un ataque ("¡AHORA SOPLA!").</summary>
        public event Action<string, Color> AlAviso;
        /// <summary>Un ataque ha tumbado a un bicho (aros, cuchillas o tuercas): puntos del villano.</summary>
        public event Action<ControladorBicho> AlDerribar;

        /// <summary>El jugador que maneja el robot (lo pone la ronda), si lo hay.</summary>
        [NonSerialized] public Saboteador villano;
        /// <summary>Lo maneja un jugador (si se le corta la conexión, vuelve a ir solo).</summary>
        public bool Pilotado => villano != null && !villano.EsMaquina;
        /// <summary>Las cartas entre las que elige el villano el siguiente ataque.</summary>
        public IReadOnlyList<Opcion> Opciones => opciones;
        /// <summary>La carta que tiene elegida el villano.</summary>
        public int Elegida => elegida;

        /// <summary>Lo enciende la ronda mientras se juega.</summary>
        [NonSerialized] public bool activo;
        /// <summary>Cuánto va de ronda (0-1): todo va más rápido.</summary>
        [NonSerialized] public float progreso;
        /// <summary>Fase de la pelea (1-3): qué ataques puede hacer.</summary>
        [NonSerialized] public int fase = 1;

        public Vector3 Centro => transform.position;
        public bool Anunciando => estado == Estado.Anuncio;
        public bool Anunciado(Ataque a) => estado == Estado.Anuncio && (principal == a || segundo == a || tercero == a);
        /// <summary>El tercer ataque del anuncio (solo en el último asalto), si lo hay.</summary>
        public Ataque? Tercero => tercero;
        /// <summary>El final de la ronda en el modo furia: hasta tres ataques a la vez.</summary>
        public bool UltimoAsalto => fase >= 3 && progreso >= ultimoAsalto;
        public bool Aspirando => aspirando && !soplando;
        public bool Soplando => aspirando && soplando;
        public float VelocidadAros => Mathf.Lerp(velocidadAro.x, velocidadAro.y, progreso);
        /// <summary>Aros láser activos (radio y si es alto), para los bots.</summary>
        public IEnumerable<(float radio, bool alto)> Aros
        {
            get { foreach (var a in arosVivos) yield return (a.radio, a.alto); }
        }
        /// <summary>Las cuchillas que giran (ángulo en grados y velocidad), para los bots.</summary>
        public IEnumerable<(float angulo, float velocidad)> Cuchillas
        {
            get
            {
                if (!barriendo) yield break;
                for (int i = 0; i < cuchillasEnUso; i++) yield return (anguloCuchilla + i * 180f, velocidadCuchillaActual);
            }
        }
        /// <summary>Las tuercas y pilas que andan sueltas (para los bots).</summary>
        public IEnumerable<Rigidbody> Proyectiles
        {
            get { foreach (var p in sueltos) if (p != null) yield return p; }
        }

        enum Estado { Descanso, Anuncio, Ataque }

        class AroVivo
        {
            public Transform t;
            public float radio;
            public bool alto;
            public readonly HashSet<ControladorBicho> tocados = new HashSet<ControladorBicho>();
        }

        class Porcion
        {
            public Rigidbody rb;
            public Renderer render;
            public Material material;     // su copia (para ponerla roja)
            public Color color;
            public Vector3 inicio;
            public Quaternion giroInicio;
            public Vector3 bisagra, eje;  // la trampilla se abre por el borde del foso
            public float aviso, cae, sube; // momentos (Time.time); 0 = tranquila
        }

        static readonly int IdColor = Shader.PropertyToID("_BaseColor");
        static readonly Color ColorAviso = new Color(1f, 0.22f, 0.15f);

        class Racha
        {
            public float angulo, radio, altura, velocidad;
        }

        Racha[] rachas, flechas, hojas;
        Material materialRemolino, materialEmbudo;
        Color colorRemolino, colorEmbudo;
        float giroRemolino, giroEmbudo;
        float intensidadViento;
        readonly List<AroVivo> arosVivos = new List<AroVivo>();
        readonly List<Rigidbody> sueltos = new List<Rigidbody>();
        readonly Dictionary<ControladorBicho, float> tocadosCuchilla = new Dictionary<ControladorBicho, float>();
        Porcion[] trozos;
        Estado estado = Estado.Descanso;
        Ataque principal;
        Ataque? segundo, tercero;
        float finEstado, siguientePolvo, giroTurbina;

        // Lo que está pasando ahora (puede haber dos ataques a la vez).
        int arosPorSalir;
        float siguienteAro;
        bool aspirando, soplando;
        float finAspiracion, cambioSoplido, vueltaAspirar;
        bool tuercas;
        float finTuercas, siguienteTuerca;
        bool barriendo;
        int cuchillasEnUso;
        float anguloCuchilla, giradoCuchilla, vueltasCuchilla, velocidadCuchillaActual, invertirEn, pausaCuchilla;
        bool cortando;
        int salidaCorte;
        float otraOleada; // cuándo empieza el segundo corte (0 = no hay)
        float finPrimeraOleada;

        // El villano.
        readonly List<Opcion> opciones = new List<Opcion>();
        int elegida, poderes;
        float siguientePoder, inicioDescanso;
        bool proximoAroAlto, otroCorteUsado;

        void Awake()
        {
            if (porciones != null)
            {
                trozos = new Porcion[porciones.Length];
                for (int i = 0; i < porciones.Length; i++)
                {
                    if (porciones[i] == null) continue;
                    porciones[i].isKinematic = true;
                    var p = new Porcion { rb = porciones[i], render = porciones[i].GetComponentInChildren<Renderer>(), inicio = porciones[i].position, giroInicio = porciones[i].rotation };
                    if (p.render != null)
                    {
                        p.material = p.render.material; // copia propia: se puede poner roja sola
                        p.color = p.material.HasProperty(IdColor) ? p.material.GetColor(IdColor) : Color.white;
                        // La bisagra, en el borde del foso, en medio de la porción; el eje, de través.
                        Vector3 medio = p.render.bounds.center - Centro;
                        medio.y = 0f;
                        Vector3 radial = medio.sqrMagnitude > 0.01f ? medio.normalized : Vector3.forward;
                        p.bisagra = Centro + radial * radioFoso;
                        p.eje = Vector3.Cross(Vector3.up, radial); // con ángulo positivo, lo de fuera baja
                    }
                    trozos[i] = p;
                }
            }
        }

        void Start()
        {
            rachas = NuevasRachas(rafagas);
            flechas = NuevasRachas(flechasViento);
            hojas = NuevasRachas(recortes);
            if (remolino != null) { materialRemolino = remolino.material; colorRemolino = materialRemolino.GetColor(IdColor); remolino.gameObject.SetActive(false); }
            if (embudo != null) { materialEmbudo = embudo.material; colorEmbudo = materialEmbudo.GetColor(IdColor); embudo.gameObject.SetActive(false); }
            PonerCara(Cara.Normal);
            Apagar(aros);
            Apagar(arosAltos);
            Apagar(cuchillas);
            finEstado = Time.time + 2f;
        }

        static void Apagar(Transform[] lista)
        {
            if (lista == null) return;
            foreach (var t in lista) if (t != null) t.gameObject.SetActive(false);
        }

        public void PonerCara(Cara cara)
        {
            if (caras == null) return;
            int indice = (int)cara;
            if (indice >= caras.Length) indice = (int)Cara.Enfadado;
            for (int i = 0; i < caras.Length; i++)
                if (caras[i] != null && caras[i].activeSelf != (i == indice)) caras[i].SetActive(i == indice);
        }

        /// <summary>Para todo (fin de ronda): fuera aros, cuchillas y tuercas, el césped en su sitio y cara de mareo.</summary>
        public void Parar()
        {
            activo = false;
            estado = Estado.Descanso;
            foreach (var a in arosVivos) if (a.t != null) a.t.gameObject.SetActive(false);
            arosVivos.Clear();
            arosPorSalir = 0;
            aspirando = tuercas = barriendo = cortando = false;
            otraOleada = 0f;
            segundo = tercero = null;
            opciones.Clear();
            Apagar(cuchillas);
            foreach (var p in sueltos) if (p != null) Destroy(p.gameObject);
            sueltos.Clear();
            if (trozos != null)
                foreach (var t in trozos)
                {
                    if (t == null) continue;
                    t.aviso = t.cae = t.sube = 0f;
                    Colocar(t, 0f, 0f, 0f);
                    Pintar(t, 0f);
                }
            PonerCara(Cara.Mareado);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool atacando = estado == Estado.Ataque;
            if (cuerpo != null) cuerpo.Rotate(0f, giro * (atacando ? 1.8f : 1f) * (fase >= 3 ? 1.4f : 1f) * dt, 0f, Space.World);
            if (turbina != null)
            {
                giroTurbina = Mathf.MoveTowards(giroTurbina, aspirando ? (soplando ? -1400f : 1400f) : 180f, 1200f * dt);
                turbina.Rotate(0f, giroTurbina * dt, 0f, Space.Self);
            }
            ActualizarViento(dt);
            if (!activo) return;

            if (Pilotado)
            {
                if (opciones.Count == 0 && estado != Estado.Anuncio) PrepararCartas();
                if (villano.PulsoCambiar && opciones.Count > 0)
                {
                    elegida = (elegida + 1) % opciones.Count;
                    Sonidos.Reproducir(Sonido.Click, Centro, 0.6f, 1.2f);
                }
                villano.AyudaVillano = Ayuda();
            }

            switch (estado)
            {
                case Estado.Descanso:
                    // El villano puede lanzar el ataque antes (A), tras un respiro mínimo.
                    if (Time.time >= finEstado || (Pilotado && villano.PulsoAccion && Time.time - inicioDescanso > 0.4f)) Anunciar();
                    break;
                case Estado.Anuncio:
                    if (Time.time >= finEstado) Empezar();
                    break;
                case Estado.Ataque:
                    if (Pilotado) Poderes();
                    ActualizarAtaques();
                    if (!(arosPorSalir > 0 || arosVivos.Count > 0 || aspirando || tuercas || barriendo || cortando)) Terminar();
                    break;
            }
        }

        // ---------------------------------------------------------------------
        // Viento: rachas y flechas que enseñan hacia dónde tira (o sopla) el robot
        // ---------------------------------------------------------------------

        Racha[] NuevasRachas(Transform[] lista)
        {
            if (lista == null) return new Racha[0];
            var r = new Racha[lista.Length];
            for (int i = 0; i < r.Length; i++)
            {
                r[i] = new Racha();
                Recolocar(r[i], true, false);
                if (lista[i] != null) lista[i].gameObject.SetActive(false);
            }
            return r;
        }

        /// <summary>Una racha nueva: al aspirar nace fuera; al soplar, junto al robot (o en cualquier sitio, al principio).</summary>
        void Recolocar(Racha r, bool enCualquierSitio, bool sopla)
        {
            r.angulo = UnityEngine.Random.value * Mathf.PI * 2f;
            r.radio = enCualquierSitio ? UnityEngine.Random.Range(radioRobot + 1f, radioPista + 3f)
                    : sopla ? radioRobot + UnityEngine.Random.Range(0.5f, 2f) : radioPista + UnityEngine.Random.Range(1f, 5f);
            r.altura = UnityEngine.Random.Range(0.25f, 2.6f);
            r.velocidad = UnityEngine.Random.Range(9f, 14f);
        }

        void ActualizarViento(float dt)
        {
            // Fuerte mientras aspira o sopla; flojito durante el aviso (para que se vea de dónde viene).
            float objetivo = aspirando ? 1f : Anunciado(Ataque.Aspiracion) ? 0.45f : 0f;
            intensidadViento = Mathf.MoveTowards(intensidadViento, objetivo, dt * 1.5f);
            bool sopla = aspirando && soplando;
            float sentido = sopla ? 1f : -1f;
            MoverRachas(rafagas, rachas, dt, sentido, sopla, true);
            MoverRachas(flechasViento, flechas, dt, sentido, sopla, false);
            MoverHojas(dt, sentido, sopla);

            // El remolino del suelo y el ciclón: giran hacia donde va el aire y se encienden poco a poco.
            // (Girar en Y positivo hace que los brazos de la espiral parezcan ir hacia dentro.)
            giroRemolino += -sentido * 70f * intensidadViento * dt;
            giroEmbudo += -sentido * 260f * intensidadViento * dt;
            Mostrar(remolino, materialRemolino, colorRemolino, Quaternion.Euler(0f, giroRemolino, 0f), Vector3.one);
            float pulso = 1f + Mathf.Sin(Time.time * 5f) * 0.04f;
            Mostrar(embudo, materialEmbudo, colorEmbudo, Quaternion.Euler(0f, giroEmbudo, 0f), new Vector3(pulso, 1f, pulso));
        }

        void Mostrar(Renderer r, Material m, Color color, Quaternion giro, Vector3 escala)
        {
            if (r == null || m == null) return;
            bool ver = intensidadViento > 0.01f;
            if (r.gameObject.activeSelf != ver) r.gameObject.SetActive(ver);
            if (!ver) return;
            r.transform.localRotation = giro;
            r.transform.localScale = escala;
            Color c = color;
            c.a *= Mathf.SmoothStep(0f, 1f, intensidadViento);
            m.SetColor(IdColor, c);
        }

        /// <summary>Los recortes de césped: a ras de suelo, arrastrados por el aire y dando vueltas.</summary>
        void MoverHojas(float dt, float sentido, bool sopla)
        {
            if (recortes == null || hojas == null) return;
            int visibles = Mathf.RoundToInt(recortes.Length * Mathf.Clamp01(intensidadViento * 1.3f - 0.2f));
            for (int i = 0; i < recortes.Length; i++)
            {
                var t = recortes[i];
                if (t == null) continue;
                bool ver = i < visibles;
                if (t.gameObject.activeSelf != ver) t.gameObject.SetActive(ver);
                if (!ver) continue;
                var r = hojas[i];
                r.radio += sentido * r.velocidad * 0.7f * dt;
                r.angulo += -sentido * dt * 4f / Mathf.Max(3f, r.radio);
                if (!sopla && r.radio < radioRobot + 0.3f) { Recolocar(r, false, false); r.altura = UnityEngine.Random.Range(0.15f, 1.4f); }
                if (sopla && r.radio > radioPista + 6f) { Recolocar(r, false, true); r.altura = UnityEngine.Random.Range(0.15f, 1.4f); }
                float bote = Mathf.Abs(Mathf.Sin(Time.time * 3f + i)) * 0.4f;
                Vector3 radial = new Vector3(Mathf.Cos(r.angulo), 0f, Mathf.Sin(r.angulo));
                t.position = Centro + radial * r.radio + Vector3.up * (Mathf.Min(r.altura, 1.4f) + bote);
                t.rotation = Quaternion.Euler(Time.time * (200f + i * 37f), Time.time * (150f + i * 23f), i * 40f);
            }
        }

        void MoverRachas(Transform[] lista, Racha[] datos, float dt, float sentido, bool sopla, bool enElAire)
        {
            if (lista == null || datos == null) return;
            int visibles = Mathf.RoundToInt(lista.Length * intensidadViento);
            for (int i = 0; i < lista.Length; i++)
            {
                var t = lista[i];
                if (t == null) continue;
                bool ver = i < visibles;
                if (t.gameObject.activeSelf != ver) t.gameObject.SetActive(ver);
                if (!ver) continue;
                var r = datos[i];
                if (enElAire)
                {
                    // En espiral: se cierran (o se abren) dando vueltas, más deprisa cerca del robot.
                    r.radio += sentido * r.velocidad * dt * Mathf.Lerp(0.5f, 1f, intensidadViento);
                    r.angulo += -sentido * dt * 6f / Mathf.Max(3f, r.radio);
                    if (!sopla && r.radio < radioRobot + 0.3f) Recolocar(r, false, false);
                    if (sopla && r.radio > radioPista + 6f) Recolocar(r, false, true);
                }
                else
                {
                    // Las flechas del suelo se quedan en el césped: van y, al salir, vuelven por el otro lado.
                    r.radio += sentido * 4f * dt;
                    if (r.radio < radioFoso + 1f) r.radio = radioPista - 1f;
                    if (r.radio > radioPista - 1f) r.radio = radioFoso + 1f;
                }
                Vector3 radial = new Vector3(Mathf.Cos(r.angulo), 0f, Mathf.Sin(r.angulo));
                Vector3 tangente = Vector3.Cross(Vector3.up, radial) * -sentido;
                Vector3 hacia = enElAire ? (radial * sentido + tangente * 0.35f).normalized : radial * sentido;
                t.SetPositionAndRotation(Centro + radial * r.radio + Vector3.up * (enElAire ? r.altura : 0.04f), Quaternion.LookRotation(hacia, Vector3.up));
            }
        }

        // ---------------------------------------------------------------------
        // Elegir, anunciar, empezar y terminar
        // ---------------------------------------------------------------------

        void Anunciar()
        {
            // El villano lanza la carta que tiene elegida; si no, el robot sortea.
            var o = Pilotado && opciones.Count > 0 ? opciones[Mathf.Clamp(elegida, 0, opciones.Count - 1)] : Sortear(null);
            opciones.Clear();
            principal = o.a;
            segundo = o.b;
            tercero = o.c;
            ultimo = principal;
            estado = Estado.Anuncio;
            finEstado = Time.time + anuncio * (fase >= 3 ? 0.85f : 1f) * (tercero.HasValue ? 1.45f : segundo.HasValue ? 1.25f : 1f);
            PonerCara(CaraDe(principal));
            Sonidos.Reproducir(Sonido.Alarma, Centro, 0.7f, principal == Ataque.OndaLaser ? 1.3f : principal == Ataque.Aspiracion ? 0.8f : 1f);
            AlAnunciar?.Invoke(principal, segundo);
        }

        /// <summary>
        /// Un ataque al azar de los de esta fase (nunca el mismo que el anterior ni, si se puede, uno
        /// de "yaSalen"), con su combo: a veces en la fase 2, casi siempre en la furia; en el último
        /// asalto, a menudo un tercero.
        /// </summary>
        Opcion Sortear(List<Ataque> yaSalen)
        {
            var posibles = new List<Ataque> { Ataque.OndaLaser, Ataque.Aspiracion, Ataque.LluviaTuercas };
            if (fase >= 2) { posibles.Add(Ataque.Barrido); posibles.Add(Ataque.CorteCesped); }
            // Los nuevos, más a menudo al llegar a su fase.
            if (fase == 2) { posibles.Add(Ataque.Barrido); posibles.Add(Ataque.CorteCesped); }
            if (yaSalen != null && posibles.Exists(p => !yaSalen.Contains(p) && p != ultimo)) posibles.RemoveAll(p => yaSalen.Contains(p));
            var o = new Opcion { a = Elegir(posibles) };
            float combo = fase >= 3 ? 0.8f : fase == 2 ? 0.25f : 0f;
            if (UnityEngine.Random.value < combo)
            {
                o.b = Compatible(o.a, null);
                if (o.b.HasValue && UltimoAsalto && UnityEngine.Random.value < 0.55f) o.c = Compatible(o.a, o.b);
            }
            return o;
        }

        Ataque ultimo = (Ataque)(-1);

        Ataque Elegir(List<Ataque> posibles)
        {
            Ataque elegido;
            int intentos = 0;
            do elegido = posibles[UnityEngine.Random.Range(0, posibles.Count)];
            while (elegido == ultimo && ++intentos < 10);
            return elegido;
        }

        /// <summary>Un ataque que se pueda juntar con los ya elegidos (null si no hay ninguno).</summary>
        static Ataque? Compatible(Ataque a, Ataque? b)
        {
            var lista = new List<Ataque>();
            foreach (Ataque x in Enum.GetValues(typeof(Ataque)))
                if (x != a && x != b && SeJuntan(a, x) && (!b.HasValue || SeJuntan(b.Value, x))) lista.Add(x);
            return lista.Count > 0 ? lista[UnityEngine.Random.Range(0, lista.Count)] : (Ataque?)null;
        }

        /// <summary>Qué ataques se pueden hacer a la vez (sin que sea imposible).</summary>
        static bool SeJuntan(Ataque a, Ataque b)
        {
            bool Par(Ataque x, Ataque y) => (a == x && b == y) || (a == y && b == x);
            return Par(Ataque.OndaLaser, Ataque.LluviaTuercas) || Par(Ataque.Barrido, Ataque.LluviaTuercas)
                || Par(Ataque.CorteCesped, Ataque.OndaLaser) || Par(Ataque.CorteCesped, Ataque.Aspiracion)
                || Par(Ataque.Barrido, Ataque.CorteCesped) || Par(Ataque.CorteCesped, Ataque.LluviaTuercas);
        }

        static Cara CaraDe(Ataque a) => a switch
        {
            Ataque.OndaLaser => Cara.Laser,
            Ataque.Aspiracion => Cara.Risa,
            Ataque.CorteCesped => Cara.Corte,
            Ataque.Barrido => Cara.Furia,
            _ => Cara.Enfadado,
        };

        void Empezar()
        {
            estado = Estado.Ataque;
            poderes = 3;
            proximoAroAlto = otroCorteUsado = false;
            if (Pilotado) PrepararCartas(); // las del siguiente, para ir eligiendo
            Empezar(principal);
            if (segundo.HasValue) Empezar(segundo.Value);
            if (tercero.HasValue) Empezar(tercero.Value);
        }

        void Empezar(Ataque a)
        {
            switch (a)
            {
                case Ataque.OndaLaser:
                    arosPorSalir = 3 + Mathf.RoundToInt(progreso * 4f);
                    siguienteAro = Time.time;
                    break;
                case Ataque.Aspiracion:
                    aspirando = true;
                    soplando = false;
                    finAspiracion = Time.time + Mathf.Lerp(5.5f, 8f, progreso);
                    // Desde la fase 2, a mitad de camino puede darse la vuelta y soplar hacia fuera
                    // (en la furia, casi siempre; y luego, a veces, vuelve a aspirar).
                    cambioSoplido = !Pilotado && UnityEngine.Random.value < (fase >= 3 ? 0.85f : fase == 2 ? 0.6f : 0f) ? Time.time + UnityEngine.Random.Range(2f, 3.2f) : float.MaxValue;
                    vueltaAspirar = float.MaxValue;
                    Sonidos.Reproducir(Sonido.Viento, Centro, 1f, 0.7f);
                    break;
                case Ataque.LluviaTuercas:
                    tuercas = true;
                    finTuercas = Time.time + Mathf.Lerp(6f, 8.5f, progreso);
                    siguienteTuerca = Time.time;
                    break;
                case Ataque.Barrido:
                    barriendo = true;
                    cuchillasEnUso = fase >= 3 ? 2 : 1;
                    velocidadCuchillaActual = Mathf.Lerp(velocidadCuchilla.x, velocidadCuchilla.y, progreso) * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
                    anguloCuchilla = UnityEngine.Random.Range(0f, 360f);
                    giradoCuchilla = 0f;
                    vueltasCuchilla = fase >= 3 ? 1.5f : 1.75f;
                    // Desde la fase 2, a veces se para un momento y gira al revés.
                    invertirEn = !Pilotado && fase >= 2 && UnityEngine.Random.value < (fase >= 3 ? 0.6f : 0.4f) ? UnityEngine.Random.Range(140f, 260f) : float.MaxValue;
                    pausaCuchilla = 0f;
                    for (int i = 0; i < cuchillas.Length; i++) if (cuchillas[i] != null) cuchillas[i].gameObject.SetActive(i < cuchillasEnUso);
                    tocadosCuchilla.Clear();
                    Sonidos.Reproducir(Sonido.Mecanismo, Centro, 1f, 0.6f);
                    break;
                case Ataque.CorteCesped:
                    EmpezarCorte();
                    break;
            }
        }

        void Terminar()
        {
            estado = Estado.Descanso;
            float descanso = fase >= 3 ? 0.5f : fase == 2 ? 1.1f : 1.8f;
            finEstado = Time.time + descanso;
            inicioDescanso = Time.time;
            PonerCara(fase >= 3 ? Cara.Furia : Cara.Normal);
            AlTerminar?.Invoke();
        }

        // ---------------------------------------------------------------------
        // El villano: cartas del siguiente ataque y poderes
        // ---------------------------------------------------------------------

        /// <summary>Tres cartas distintas para el siguiente ataque (elige la del medio, para empezar).</summary>
        void PrepararCartas()
        {
            opciones.Clear();
            var salen = new List<Ataque>();
            for (int i = 0; i < 3; i++)
            {
                var o = Sortear(salen);
                opciones.Add(o);
                salen.Add(o.a);
            }
            elegida = 1;
        }

        /// <summary>A: el poder de lo que esté pasando (tres por ataque, con un respiro entre ellos).</summary>
        void Poderes()
        {
            if (poderes <= 0 || Time.time < siguientePoder || !villano.PulsoAccion) return;
            bool usado = false;
            if (barriendo && Time.time >= pausaCuchilla) { InvertirCuchilla(); usado = true; }
            if (aspirando) { CambiarViento(); usado = true; }
            if (arosPorSalir > 0 && !proximoAroAlto && arosAltos != null && arosAltos.Length > 0) { proximoAroAlto = true; usado = true; }
            if (tuercas) { for (int i = 0; i < 3; i++) Escupir(); usado = true; }
            if (cortando && !otroCorteUsado && otraOleada <= 0f)
            {
                // Otro corte en las porciones de al lado, en cuanto se empiecen a cerrar las primeras.
                otroCorteUsado = true;
                otraOleada = Mathf.Max(Time.time + 0.2f, finPrimeraOleada);
                usado = true;
            }
            if (!usado) return;
            poderes--;
            siguientePoder = Time.time + 1.2f;
            Sonidos.Reproducir(Sonido.Pitido, Centro, 0.7f, 0.6f);
        }

        /// <summary>Lo que puede hacer ahora el villano (sale en su panel).</summary>
        string Ayuda()
        {
            var partes = new List<string> { "X: cambiar carta" };
            if (estado == Estado.Descanso) partes.Add("A: ¡lánzalo ya!");
            else if (estado == Estado.Anuncio && Anunciado(Ataque.CorteCesped)) partes.Add("Stick: elige dónde corta");
            else if (estado == Estado.Ataque)
            {
                if (tuercas) partes.Add("Stick: apuntar");
                string poder = barriendo ? "girar al revés" : aspirando ? (soplando ? "aspirar" : "soplar")
                             : arosPorSalir > 0 ? "aro alto" : tuercas ? "ráfaga" : cortando && !otroCorteUsado ? "otro corte" : null;
                if (poder != null && poderes > 0) partes.Add($"A: {poder} (quedan {poderes})");
            }
            return string.Join("  ·  ", partes);
        }

        /// <summary>La aspiración se da la vuelta: sopla hacia fuera o vuelve a aspirar.</summary>
        void CambiarViento()
        {
            soplando = !soplando;
            AlAviso?.Invoke(soplando ? "¡AHORA SOPLA! ¡HACIA DENTRO!" : "¡Y VUELVE A ASPIRAR! ¡HACIA FUERA!",
                            soplando ? new Color(0.45f, 0.85f, 1f) : new Color(1f, 0.75f, 0.3f));
            Sonidos.Reproducir(Sonido.Viento, Centro, 1f, soplando ? 1.1f : 0.7f);
        }

        /// <summary>La cuchilla frena en seco, avisa y vuelve hacia atrás.</summary>
        void InvertirCuchilla()
        {
            velocidadCuchillaActual = -velocidadCuchillaActual;
            pausaCuchilla = Time.time + 0.4f;
            AlAviso?.Invoke("¡LA CUCHILLA CAMBIA DE SENTIDO!", new Color(1f, 0.5f, 0.3f));
            Sonidos.Reproducir(Sonido.Mecanismo, Centro, 1f, 1.5f);
        }

        /// <summary>Un ataque (o una tuerca) ha tumbado a ese bicho.</summary>
        public void Derribado(ControladorBicho b) => AlDerribar?.Invoke(b);

        void ActualizarAtaques()
        {
            if (arosPorSalir > 0 && Time.time >= siguienteAro)
            {
                // Desde la fase 2, algunos aros van altos (hay que NO saltar).
                // El villano decide cuál va alto (A); si no, al azar desde la fase 2.
                bool alto = arosAltos != null && arosAltos.Length > 0 &&
                            (Pilotado ? proximoAroAlto : fase >= 2 && UnityEngine.Random.value < (fase >= 3 ? 0.5f : 0.4f));
                proximoAroAlto = false;
                SoltarAro(alto);
                arosPorSalir--;
                siguienteAro = Time.time + Mathf.Lerp(0.95f, 0.6f, progreso);
            }
            if (aspirando)
            {
                if (!soplando && Time.time >= cambioSoplido)
                {
                    cambioSoplido = float.MaxValue;
                    if (fase >= 3 && UnityEngine.Random.value < 0.5f) vueltaAspirar = Time.time + UnityEngine.Random.Range(1.6f, 2.4f);
                    CambiarViento();
                }
                if (soplando && Time.time >= vueltaAspirar)
                {
                    vueltaAspirar = float.MaxValue;
                    CambiarViento();
                }
                if (Time.time >= siguientePolvo)
                {
                    siguientePolvo = Time.time + 0.12f;
                    float a = UnityEngine.Random.value * Mathf.PI * 2f;
                    float r = UnityEngine.Random.Range(radioFoso, radioPista);
                    Efectos.Polvo(Centro + new Vector3(Mathf.Cos(a) * r, 0.2f, Mathf.Sin(a) * r), 0.5f);
                }
                if (Time.time >= finAspiracion) aspirando = soplando = false;
            }
            if (tuercas)
            {
                if (Time.time >= siguienteTuerca)
                {
                    Escupir();
                    if (fase >= 3) Escupir(); // en la furia, de dos en dos
                    siguienteTuerca = Time.time + Mathf.Lerp(0.45f, 0.22f, progreso) * UnityEngine.Random.Range(0.8f, 1.2f);
                }
                if (Time.time >= finTuercas) tuercas = false;
            }
            if (cortando) ActualizarCorte();
        }

        // ---------------------------------------------------------------------
        // Onda láser
        // ---------------------------------------------------------------------

        void SoltarAro(bool alto)
        {
            foreach (var t in alto ? arosAltos : aros)
            {
                if (t == null || t.gameObject.activeSelf) continue;
                t.gameObject.SetActive(true);
                t.localScale = new Vector3(radioRobot, 1f, radioRobot);
                arosVivos.Add(new AroVivo { t = t, radio = radioRobot, alto = alto });
                Sonidos.Reproducir(Sonido.Pitido, Centro, 0.8f, alto ? 1.1f : 1.6f);
                return;
            }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // Los aros crecen. Los bajos tumban a quien está en el suelo; los altos, a quien salta.
            for (int i = arosVivos.Count - 1; i >= 0; i--)
            {
                var a = arosVivos[i];
                a.radio += VelocidadAros * dt;
                if (a.t != null) a.t.localScale = new Vector3(a.radio, 1f, a.radio);
                foreach (var b in ControladorBicho.Todos)
                {
                    if (b == null || b.Retirado || a.tocados.Contains(b)) continue;
                    Vector3 rel = b.transform.position - Centro;
                    float altura = rel.y;
                    rel.y = 0f;
                    if (Mathf.Abs(rel.magnitude - a.radio) > 0.45f || altura < -1f) continue;
                    if (a.alto ? altura < 0.35f : altura > 0.55f) continue;
                    a.tocados.Add(b);
                    Golpear(b, rel.sqrMagnitude > 0.01f ? rel.normalized : Vector3.forward, a.alto ? new Color(0.3f, 0.9f, 1f) : new Color(1f, 0.3f, 0.8f));
                }
                if (a.radio > radioPista + 1.5f)
                {
                    if (a.t != null) a.t.gameObject.SetActive(false);
                    arosVivos.RemoveAt(i);
                }
            }

            // La aspiración tira de todos hacia el robot (o, si sopla, hacia fuera).
            if (aspirando)
            {
                float fuerza = Mathf.Lerp(aspiracion.x, aspiracion.y, progreso) * (soplando ? 0.9f : 1f);
                foreach (var b in ControladorBicho.Todos)
                {
                    if (b == null || b.Retirado) continue;
                    Vector3 rel = b.transform.position - Centro;
                    rel.y = 0f;
                    float d = rel.magnitude;
                    if (d < radioRobot + 0.6f || d > radioPista + 3f) continue;
                    float k = soplando ? Mathf.Lerp(0.8f, 1.25f, Mathf.InverseLerp(radioFoso, radioPista, d))
                                       : Mathf.Lerp(1.25f, 0.8f, Mathf.InverseLerp(radioFoso, radioPista, d));
                    b.Cuerpo.position += rel / d * (fuerza * k * dt * (soplando ? 1f : -1f));
                }
            }

            // El barrido: las cuchillas giran a ras de suelo; a quien pillan en el suelo, lo lanzan.
            if (barriendo)
            {
                if (giradoCuchilla >= invertirEn)
                {
                    invertirEn = float.MaxValue;
                    InvertirCuchilla();
                }
                float paso = Time.time < pausaCuchilla ? 0f : velocidadCuchillaActual * dt;
                anguloCuchilla = Mathf.Repeat(anguloCuchilla + paso, 360f);
                giradoCuchilla += Mathf.Abs(paso);
                for (int i = 0; i < cuchillasEnUso && i < cuchillas.Length; i++)
                    if (cuchillas[i] != null) cuchillas[i].localRotation = Quaternion.Euler(0f, -(anguloCuchilla + i * 180f), 0f);
                foreach (var b in ControladorBicho.Todos)
                {
                    if (b == null || b.Retirado) continue;
                    if (tocadosCuchilla.TryGetValue(b, out float antes) && Time.time - antes < 0.8f) continue;
                    Vector3 rel = b.transform.position - Centro;
                    float altura = rel.y;
                    rel.y = 0f;
                    float d = rel.magnitude;
                    if (d < radioRobot || d > radioPista + 0.5f || altura > 0.7f || altura < -1f) continue;
                    float anguloBicho = Mathf.Atan2(rel.z, rel.x) * Mathf.Rad2Deg;
                    for (int i = 0; i < cuchillasEnUso; i++)
                    {
                        float dif = Mathf.Abs(Mathf.DeltaAngle(anguloBicho, anguloCuchilla + i * 180f));
                        if (dif * Mathf.Deg2Rad * d > 0.6f) continue;
                        tocadosCuchilla[b] = Time.time;
                        Vector3 tangente = Vector3.Cross(Vector3.up, rel / d) * Mathf.Sign(velocidadCuchillaActual);
                        Golpear(b, (tangente + rel / d * 0.4f).normalized, new Color(1f, 0.5f, 0.3f));
                        break;
                    }
                }
                if (giradoCuchilla >= 360f * vueltasCuchilla)
                {
                    barriendo = false;
                    Apagar(cuchillas);
                }
            }
            sueltos.RemoveAll(p => p == null);
        }

        void Golpear(ControladorBicho b, Vector3 direccion, Color color)
        {
            b.Cuerpo.linearVelocity = direccion * (fase >= 3 ? 6.5f : 5.5f) + Vector3.up * 4f;
            b.Tropezar(0.9f);
            Sonidos.Reproducir(Sonido.Derribo, b.transform.position, b.EsBot ? 0.6f : 1f, 1.2f);
            Efectos.Impacto(b.transform.position + Vector3.up * 0.5f, color, 0.6f);
            AlDerribar?.Invoke(b);
        }

        // ---------------------------------------------------------------------
        // Corte de césped: porciones que parpadean, se caen y vuelven
        // ---------------------------------------------------------------------

        void EmpezarCorte()
        {
            if (trozos == null || trozos.Length == 0) return;
            cortando = true;
            // El villano elige dónde corta (apuntando con el stick); si no, al azar.
            Vector3 apunta = Pilotado ? villano.StickMundo : Vector3.zero;
            salidaCorte = apunta != Vector3.zero ? Sector(apunta) : UnityEngine.Random.Range(0, trozos.Length);
            float aviso = fase >= 3 ? 1.15f : 1.5f;
            Oleada(salidaCorte, fase >= 3 ? 6 : 5, aviso, apunta == Vector3.zero);
            finPrimeraOleada = Time.time + aviso + 2.4f;
            // En la furia, a veces otro corte en las porciones de al lado, justo cuando las primeras
            // empiezan a cerrarse (hay que cambiar de porción otra vez).
            otraOleada = !Pilotado && fase >= 3 && UnityEngine.Random.value < 0.6f ? finPrimeraOleada : 0f;
            Sonidos.Reproducir(Sonido.Mecanismo, Centro, 0.9f, 1.3f);
        }

        /// <summary>Porciones separadas (una sí y otra no, desde "salida"): siempre queda dónde ponerse.</summary>
        void Oleada(int salida, int cuantos, float aviso, bool desordenar)
        {
            int n = trozos.Length;
            var elegidas = new List<int>();
            for (int k = 0; k < n && elegidas.Count < cuantos; k += 2) elegidas.Add((salida + k + (desordenar && UnityEngine.Random.value < 0.3f ? 1 : 0)) % n);
            float ahora = Time.time;
            foreach (int i in elegidas)
            {
                var t = trozos[i];
                if (t == null || t.aviso > 0f) continue;
                t.aviso = ahora;
                t.cae = ahora + aviso;
                t.sube = t.cae + 2.4f;
                Sonidos.Reproducir(Sonido.Grieta, t.bisagra, 0.7f, 0.8f);
            }
        }

        void ActualizarCorte()
        {
            if (otraOleada > 0f && Time.time >= otraOleada)
            {
                otraOleada = 0f;
                Oleada(salidaCorte + 1, 5, 1.6f, false);
                AlAviso?.Invoke("¡OTRO CORTE! ¡CAMBIA DE PORCIÓN!", ColorAviso);
                Sonidos.Reproducir(Sonido.Mecanismo, Centro, 0.9f, 1.5f);
            }
            bool queda = otraOleada > 0f;
            foreach (var t in trozos)
            {
                if (t == null || t.aviso <= 0f) continue;
                queda = true;
                float ahora = Time.time;
                if (ahora < t.cae)
                {
                    // Aviso: se pone roja (latiendo, cada vez más), tiembla y se hunde un poco.
                    float k = (ahora - t.aviso) / Mathf.Max(0.1f, t.cae - t.aviso);
                    float latido = 0.5f + 0.5f * Mathf.Sin((ahora - t.aviso) * Mathf.Lerp(8f, 22f, k));
                    Pintar(t, Mathf.Lerp(0.25f, 0.9f, k) * Mathf.Lerp(0.6f, 1f, latido));
                    float temblor = Mathf.Sin(ahora * 47f) * 0.04f * k;
                    Colocar(t, 0f, -0.18f * k * k, temblor);
                }
                else if (ahora < t.sube)
                {
                    // Se abre como una trampilla (de golpe, cada vez más deprisa) y se queda abierta.
                    if (t.material != null && t.material.GetColor(IdColor) != ColorAviso) { Pintar(t, 1f); Efectos.Polvo(t.bisagra + (t.render.bounds.center - t.bisagra) * 0.5f, 1.2f); Sonidos.Reproducir(Sonido.Rotura, t.bisagra, 0.8f, 0.7f); }
                    float abre = Mathf.Clamp01((ahora - t.cae) / 0.45f);
                    Colocar(t, 75f * abre * abre, -0.18f, 0f);
                }
                else
                {
                    // Vuelve a cerrarse despacio y recupera su color.
                    float cierra = Mathf.Clamp01((ahora - t.sube) / 0.9f);
                    float suave = cierra * cierra * (3f - 2f * cierra);
                    Colocar(t, 75f * (1f - suave), -0.18f * (1f - suave), 0f);
                    Pintar(t, 1f - suave);
                    if (cierra >= 1f) { t.aviso = t.cae = t.sube = 0f; Pintar(t, 0f); }
                }
            }
            cortando = queda;
        }

        /// <summary>Coloca la porción: abierta "angulo" grados por su bisagra, hundida "baja" y movida de lado "temblor".</summary>
        void Colocar(Porcion t, float angulo, float baja, float temblor)
        {
            Quaternion giro = Quaternion.AngleAxis(angulo, t.eje);
            Vector3 lado = t.eje * temblor;
            t.rb.MovePosition(t.bisagra + giro * (t.inicio - t.bisagra) + Vector3.up * baja + lado);
            t.rb.MoveRotation(giro * t.giroInicio);
        }

        /// <summary>De su color (0) a rojo de aviso (1).</summary>
        static void Pintar(Porcion t, float rojo)
        {
            if (t.material == null || !t.material.HasProperty(IdColor)) return;
            t.material.SetColor(IdColor, Color.Lerp(t.color, ColorAviso, Mathf.Clamp01(rojo)));
        }

        /// <summary>¿Esa porción del césped se va a caer (o está caída)? Para los bots.</summary>
        public bool PorcionEnPeligro(Vector3 punto)
        {
            if (trozos == null || trozos.Length == 0) return false;
            int i = Sector(punto - Centro);
            return trozos[i] != null && trozos[i].aviso > 0f;
        }

        /// <summary>La porción del césped que hay en esa dirección (desde el centro).</summary>
        int Sector(Vector3 rel)
        {
            float angulo = Mathf.Repeat(Mathf.Atan2(rel.z, rel.x) * Mathf.Rad2Deg, 360f);
            return Mathf.Clamp(Mathf.FloorToInt(angulo / (360f / trozos.Length)), 0, trozos.Length - 1);
        }

        // ---------------------------------------------------------------------
        // Lluvia de tuercas
        // ---------------------------------------------------------------------

        void Escupir()
        {
            if (proyectiles == null || proyectiles.Length == 0 || boca == null) return;

            // Hacia delante de la boca (más o menos), a un punto del césped. Si el villano apunta,
            // hacia donde apunta (y sale por ese lado del robot, para no chocar con él).
            Vector3 delante = boca.forward;
            delante.y = 0f;
            delante.Normalize();
            float abre = 65f;
            Vector3 desde = boca.position;
            Vector3 apunta = Pilotado ? villano.StickMundo : Vector3.zero;
            if (apunta != Vector3.zero)
            {
                delante = apunta;
                abre = 20f;
                desde = Centro + apunta * (radioRobot + 0.8f) + Vector3.up * (boca.position.y - Centro.y);
            }
            desde += UnityEngine.Random.insideUnitSphere * 0.35f; // las de una ráfaga, sin salir una dentro de otra

            var plantilla = proyectiles[UnityEngine.Random.Range(0, proyectiles.Length)];
            var go = Instantiate(plantilla, desde, UnityEngine.Random.rotation);
            go.name = plantilla.name;
            go.SetActive(true);
            var rb = go.GetComponent<Rigidbody>();
            sueltos.Add(rb);
            if (go.TryGetComponent(out ProyectilChatarra chatarra)) chatarra.robot = this;

            float angulo = UnityEngine.Random.Range(-abre, abre);
            Vector3 dir = Quaternion.Euler(0f, angulo, 0f) * delante;
            float distancia = UnityEngine.Random.Range(radioFoso + 1.5f, radioPista - 2f);
            Vector3 destino = Centro + dir * distancia;
            float tiempo = 1.05f;
            float g = -Physics.gravity.y;
            Vector3 horizontal = new Vector3(destino.x - desde.x, 0f, destino.z - desde.z) / tiempo;
            float vertical = (destino.y - desde.y + 0.5f * g * tiempo * tiempo) / tiempo;
            rb.linearVelocity = horizontal + Vector3.up * vertical;
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 8f;
            Sonidos.Reproducir(Sonido.Lanzar, desde, 0.7f, UnityEngine.Random.Range(0.8f, 1.1f));
        }
    }
}
