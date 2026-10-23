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
    ///     que saltarla) y el corte de césped (porciones del suelo que parpadean y se caen). Y
    ///     variantes: aros láser altos (te dan si saltas) y la aspiración que de golpe sopla.
    ///  3. Modo furia: descansos mínimos y, a menudo, dos ataques a la vez.
    /// El pivote está en el centro, a la altura del césped.
    /// </summary>
    public class CortacespedMecha : MonoBehaviour
    {
        public enum Ataque { OndaLaser, Aspiracion, LluviaTuercas, Barrido, CorteCesped }
        public enum Cara { Normal, Enfadado, Risa, Mareado, Laser, Furia, Corte }

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

        [Header("Arena")]
        public float radioRobot = 4.2f;
        public float radioFoso = 7f;
        public float radioPista = 22f;

        [Header("Ritmo")]
        public float giro = 18f;
        [Tooltip("Velocidad de los aros láser (m/s): al principio y al final de la ronda.")]
        public Vector2 velocidadAro = new Vector2(8f, 12f);
        [Tooltip("Lo que arrastra la aspiración (m/s): al principio y al final.")]
        public Vector2 aspiracion = new Vector2(3.6f, 4.8f);
        [Tooltip("Velocidad de las cuchillas (grados/s): al principio y al final.")]
        public Vector2 velocidadCuchilla = new Vector2(70f, 115f);
        public float anuncio = 1.3f;

        /// <summary>El robot avisa de un ataque (o de dos, en el modo furia: el segundo, si no es null).</summary>
        public event Action<Ataque, Ataque?> AlAnunciar;
        /// <summary>Han terminado los ataques anunciados.</summary>
        public event Action AlTerminar;
        /// <summary>Algo que contar a mitad de un ataque ("¡AHORA SOPLA!").</summary>
        public event Action<string, Color> AlAviso;

        /// <summary>Lo enciende la ronda mientras se juega.</summary>
        [NonSerialized] public bool activo;
        /// <summary>Cuánto va de ronda (0-1): todo va más rápido.</summary>
        [NonSerialized] public float progreso;
        /// <summary>Fase de la pelea (1-3): qué ataques puede hacer.</summary>
        [NonSerialized] public int fase = 1;

        public Vector3 Centro => transform.position;
        public bool Anunciando => estado == Estado.Anuncio;
        public bool Anunciado(Ataque a) => estado == Estado.Anuncio && (principal == a || segundo == a);
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
            public Renderer[] renders;
            public Vector3 inicio;
            public float aviso, cae, sube; // momentos (Time.time); 0 = tranquila
        }

        readonly List<AroVivo> arosVivos = new List<AroVivo>();
        readonly List<Rigidbody> sueltos = new List<Rigidbody>();
        readonly Dictionary<ControladorBicho, float> tocadosCuchilla = new Dictionary<ControladorBicho, float>();
        Porcion[] trozos;
        Estado estado = Estado.Descanso;
        Ataque principal;
        Ataque? segundo;
        float finEstado, siguientePolvo, giroTurbina;

        // Lo que está pasando ahora (puede haber dos ataques a la vez).
        int arosPorSalir;
        float siguienteAro;
        bool aspirando, soplando;
        float finAspiracion, cambioSoplido;
        bool tuercas;
        float finTuercas, siguienteTuerca;
        bool barriendo;
        int cuchillasEnUso;
        float anguloCuchilla, giradoCuchilla, vueltasCuchilla, velocidadCuchillaActual;
        bool cortando;

        void Awake()
        {
            if (porciones != null)
            {
                trozos = new Porcion[porciones.Length];
                for (int i = 0; i < porciones.Length; i++)
                {
                    if (porciones[i] == null) continue;
                    porciones[i].isKinematic = true;
                    trozos[i] = new Porcion { rb = porciones[i], renders = porciones[i].GetComponentsInChildren<Renderer>(), inicio = porciones[i].position };
                }
            }
        }

        void Start()
        {
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
            Apagar(cuchillas);
            foreach (var p in sueltos) if (p != null) Destroy(p.gameObject);
            sueltos.Clear();
            if (trozos != null)
                foreach (var t in trozos)
                {
                    if (t == null) continue;
                    t.aviso = t.cae = t.sube = 0f;
                    t.rb.MovePosition(t.inicio);
                    foreach (var r in t.renders) r.enabled = true;
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
            if (!activo) return;

            switch (estado)
            {
                case Estado.Descanso:
                    if (Time.time >= finEstado) Anunciar();
                    break;
                case Estado.Anuncio:
                    if (Time.time >= finEstado) Empezar();
                    break;
                case Estado.Ataque:
                    ActualizarAtaques();
                    if (!(arosPorSalir > 0 || arosVivos.Count > 0 || aspirando || tuercas || barriendo || cortando)) Terminar();
                    break;
            }
        }

        // ---------------------------------------------------------------------
        // Elegir, anunciar, empezar y terminar
        // ---------------------------------------------------------------------

        void Anunciar()
        {
            var posibles = new List<Ataque> { Ataque.OndaLaser, Ataque.Aspiracion, Ataque.LluviaTuercas };
            if (fase >= 2) { posibles.Add(Ataque.Barrido); posibles.Add(Ataque.CorteCesped); }
            // Los nuevos, más a menudo al llegar a su fase (y nunca el mismo que el anterior).
            if (fase == 2) { posibles.Add(Ataque.Barrido); posibles.Add(Ataque.CorteCesped); }
            principal = Elegir(posibles);
            segundo = null;
            if (fase >= 3 && UnityEngine.Random.value < 0.65f)
            {
                // Combo: dos a la vez, de los que se pueden juntar.
                var compatibles = new List<Ataque>();
                foreach (Ataque a in Enum.GetValues(typeof(Ataque)))
                    if (a != principal && SeJuntan(principal, a)) compatibles.Add(a);
                if (compatibles.Count > 0) segundo = compatibles[UnityEngine.Random.Range(0, compatibles.Count)];
            }
            estado = Estado.Anuncio;
            finEstado = Time.time + anuncio * (segundo.HasValue ? 1.25f : 1f);
            PonerCara(CaraDe(principal));
            Sonidos.Reproducir(Sonido.Alarma, Centro, 0.7f, principal == Ataque.OndaLaser ? 1.3f : principal == Ataque.Aspiracion ? 0.8f : 1f);
            AlAnunciar?.Invoke(principal, segundo);
        }

        Ataque ultimo = (Ataque)(-1);

        Ataque Elegir(List<Ataque> posibles)
        {
            Ataque elegido;
            int intentos = 0;
            do elegido = posibles[UnityEngine.Random.Range(0, posibles.Count)];
            while (elegido == ultimo && ++intentos < 10);
            ultimo = elegido;
            return elegido;
        }

        /// <summary>Qué ataques se pueden hacer a la vez (sin que sea imposible).</summary>
        static bool SeJuntan(Ataque a, Ataque b)
        {
            bool Par(Ataque x, Ataque y) => (a == x && b == y) || (a == y && b == x);
            return Par(Ataque.OndaLaser, Ataque.LluviaTuercas) || Par(Ataque.Barrido, Ataque.LluviaTuercas)
                || Par(Ataque.CorteCesped, Ataque.OndaLaser) || Par(Ataque.CorteCesped, Ataque.Aspiracion)
                || Par(Ataque.Barrido, Ataque.CorteCesped);
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
            Empezar(principal);
            if (segundo.HasValue) Empezar(segundo.Value);
        }

        void Empezar(Ataque a)
        {
            switch (a)
            {
                case Ataque.OndaLaser:
                    arosPorSalir = 2 + Mathf.RoundToInt(progreso * 3f);
                    siguienteAro = Time.time;
                    break;
                case Ataque.Aspiracion:
                    aspirando = true;
                    soplando = false;
                    finAspiracion = Time.time + Mathf.Lerp(5f, 7.5f, progreso);
                    // Desde la fase 2, a mitad de camino puede darse la vuelta y soplar hacia fuera.
                    cambioSoplido = fase >= 2 && UnityEngine.Random.value < 0.6f ? Time.time + UnityEngine.Random.Range(2.2f, 3.5f) : float.MaxValue;
                    Sonidos.Reproducir(Sonido.Viento, Centro, 1f, 0.7f);
                    break;
                case Ataque.LluviaTuercas:
                    tuercas = true;
                    finTuercas = Time.time + Mathf.Lerp(5.5f, 8f, progreso);
                    siguienteTuerca = Time.time;
                    break;
                case Ataque.Barrido:
                    barriendo = true;
                    cuchillasEnUso = fase >= 3 ? 2 : 1;
                    velocidadCuchillaActual = Mathf.Lerp(velocidadCuchilla.x, velocidadCuchilla.y, progreso) * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
                    anguloCuchilla = UnityEngine.Random.Range(0f, 360f);
                    giradoCuchilla = 0f;
                    vueltasCuchilla = fase >= 3 ? 1.25f : 1.5f;
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
            float descanso = fase >= 3 ? 0.7f : fase == 2 ? 1.4f : 2.2f;
            finEstado = Time.time + descanso;
            PonerCara(fase >= 3 ? Cara.Furia : Cara.Normal);
            AlTerminar?.Invoke();
        }

        void ActualizarAtaques()
        {
            if (arosPorSalir > 0 && Time.time >= siguienteAro)
            {
                // Desde la fase 2, algunos aros van altos (hay que NO saltar).
                bool alto = fase >= 2 && arosAltos != null && arosAltos.Length > 0 && UnityEngine.Random.value < 0.4f;
                SoltarAro(alto);
                arosPorSalir--;
                siguienteAro = Time.time + Mathf.Lerp(1.05f, 0.7f, progreso);
            }
            if (aspirando)
            {
                if (!soplando && Time.time >= cambioSoplido)
                {
                    soplando = true;
                    AlAviso?.Invoke("¡AHORA SOPLA! ¡HACIA DENTRO!", new Color(0.45f, 0.85f, 1f));
                    Sonidos.Reproducir(Sonido.Viento, Centro, 1f, 1.1f);
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
                    siguienteTuerca = Time.time + Mathf.Lerp(0.5f, 0.24f, progreso) * UnityEngine.Random.Range(0.8f, 1.2f);
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
                float paso = velocidadCuchillaActual * dt;
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
            b.Cuerpo.linearVelocity = direccion * 4.5f + Vector3.up * 4f;
            b.Tropezar(0.9f);
            Sonidos.Reproducir(Sonido.Derribo, b.transform.position, b.EsBot ? 0.6f : 1f, 1.2f);
            Efectos.Impacto(b.transform.position + Vector3.up * 0.5f, color, 0.6f);
        }

        // ---------------------------------------------------------------------
        // Corte de césped: porciones que parpadean, se caen y vuelven
        // ---------------------------------------------------------------------

        void EmpezarCorte()
        {
            if (trozos == null || trozos.Length == 0) return;
            cortando = true;
            int n = trozos.Length;
            int cuantos = fase >= 3 ? 5 : 4;
            // Porciones separadas (sin dos seguidas): siempre queda dónde ponerse.
            var elegidas = new List<int>();
            int salida = UnityEngine.Random.Range(0, n);
            for (int k = 0; k < n && elegidas.Count < cuantos; k += 2) elegidas.Add((salida + k + (UnityEngine.Random.value < 0.3f ? 1 : 0)) % n);
            float ahora = Time.time;
            float aviso = fase >= 3 ? 1.3f : 1.7f;
            foreach (int i in elegidas)
            {
                var t = trozos[i];
                if (t == null || t.aviso > 0f) continue;
                t.aviso = ahora;
                t.cae = ahora + aviso;
                t.sube = t.cae + 2.6f;
            }
            Sonidos.Reproducir(Sonido.Mecanismo, Centro, 0.9f, 1.3f);
        }

        void ActualizarCorte()
        {
            bool queda = false;
            foreach (var t in trozos)
            {
                if (t == null || t.aviso <= 0f) continue;
                queda = true;
                float ahora = Time.time;
                if (ahora < t.cae)
                {
                    // Parpadea, cada vez más deprisa.
                    float k = (ahora - t.aviso) / Mathf.Max(0.1f, t.cae - t.aviso);
                    bool visible = Mathf.Repeat(ahora * Mathf.Lerp(5f, 12f, k), 1f) < 0.6f;
                    foreach (var r in t.renders) if (r.enabled != visible) r.enabled = visible;
                }
                else if (ahora < t.sube)
                {
                    foreach (var r in t.renders) if (!r.enabled) r.enabled = true;
                    float baja = Mathf.Clamp01((ahora - t.cae) / 0.35f);
                    t.rb.MovePosition(t.inicio + Vector3.down * 9f * baja);
                }
                else
                {
                    float sube = Mathf.Clamp01((ahora - t.sube) / 0.8f);
                    t.rb.MovePosition(t.inicio + Vector3.down * 9f * (1f - sube));
                    if (sube >= 1f) { t.aviso = t.cae = t.sube = 0f; }
                }
            }
            cortando = queda;
        }

        /// <summary>¿Esa porción del césped se va a caer (o está caída)? Para los bots.</summary>
        public bool PorcionEnPeligro(Vector3 punto)
        {
            if (trozos == null || trozos.Length == 0) return false;
            Vector3 rel = punto - Centro;
            float angulo = Mathf.Repeat(Mathf.Atan2(rel.z, rel.x) * Mathf.Rad2Deg, 360f);
            int i = Mathf.Clamp(Mathf.FloorToInt(angulo / (360f / trozos.Length)), 0, trozos.Length - 1);
            return trozos[i] != null && trozos[i].aviso > 0f;
        }

        // ---------------------------------------------------------------------
        // Lluvia de tuercas
        // ---------------------------------------------------------------------

        void Escupir()
        {
            if (proyectiles == null || proyectiles.Length == 0 || boca == null) return;
            var plantilla = proyectiles[UnityEngine.Random.Range(0, proyectiles.Length)];
            var go = Instantiate(plantilla, boca.position, UnityEngine.Random.rotation);
            go.name = plantilla.name;
            go.SetActive(true);
            var rb = go.GetComponent<Rigidbody>();
            sueltos.Add(rb);

            // Hacia delante de la boca (más o menos), a un punto del césped.
            Vector3 delante = boca.forward;
            delante.y = 0f;
            float angulo = UnityEngine.Random.Range(-65f, 65f);
            Vector3 dir = Quaternion.Euler(0f, angulo, 0f) * delante.normalized;
            float distancia = UnityEngine.Random.Range(radioFoso + 1.5f, radioPista - 2f);
            Vector3 destino = Centro + dir * distancia;
            float tiempo = 1.05f;
            float g = -Physics.gravity.y;
            Vector3 desde = boca.position;
            Vector3 horizontal = new Vector3(destino.x - desde.x, 0f, destino.z - desde.z) / tiempo;
            float vertical = (destino.y - desde.y + 0.5f * g * tiempo * tiempo) / tiempo;
            rb.linearVelocity = horizontal + Vector3.up * vertical;
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 8f;
            Sonidos.Reproducir(Sonido.Lanzar, desde, 0.7f, UnityEngine.Random.Range(0.8f, 1.1f));
        }
    }
}
