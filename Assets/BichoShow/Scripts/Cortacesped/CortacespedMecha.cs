using System;
using System.Collections.Generic;
using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "El Cortacésped Mecha": el robot cortacésped del centro de la arena. Gira sobre sí mismo y
    /// va alternando tres ataques, que anuncia antes con la cara de su pantalla LED:
    ///  - Onda láser: aros rosas a ras de suelo que salen de él y crecen hasta el borde. Hay que
    ///    saltarlos; a quien pilla en el suelo lo tumba. Con la ronda avanzada, salen varios.
    ///  - Aspiración invertida: enciende la turbina y tira de todos hacia él (hacia el foso de
    ///    chatarra que lo rodea): hay que correr hacia fuera.
    ///  - Lluvia de tuercas: escupe tuercas y pilas de goma que rebotan por la arena y tumban.
    /// Lo maneja RondaCortacesped (lo enciende, le dice cuánto va de ronda y escucha sus avisos).
    /// El pivote está en el centro, a la altura del césped.
    /// </summary>
    public class CortacespedMecha : MonoBehaviour
    {
        public enum Ataque { OndaLaser, Aspiracion, LluviaTuercas }
        public enum Cara { Normal, Enfadado, Risa, Mareado, Laser }

        [Header("Piezas")]
        [Tooltip("Lo que gira (el robot entero).")]
        public Transform cuerpo;
        [Tooltip("Una pantalla por cara, en el orden de Cara: se enciende la que toca.")]
        public GameObject[] caras;
        [Tooltip("Aros del láser (radio 1 a escala 1), apagados.")]
        public Transform[] aros;
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
        public float giro = 16f;
        [Tooltip("Velocidad de los aros láser (m/s): al principio y al final de la ronda.")]
        public Vector2 velocidadAro = new Vector2(7f, 10f);
        [Tooltip("Lo que arrastra la aspiración (m/s): al principio y al final.")]
        public Vector2 aspiracion = new Vector2(3f, 4.3f);
        public float anuncio = 1.4f;

        /// <summary>El robot avisa de un ataque (con la cara puesta).</summary>
        public event Action<Ataque> AlAnunciar;
        /// <summary>Un ataque ha terminado.</summary>
        public event Action<Ataque> AlTerminar;

        /// <summary>Lo enciende la ronda mientras se juega.</summary>
        [NonSerialized] public bool activo;
        /// <summary>Cuánto va de ronda (0-1): todo va más rápido.</summary>
        [NonSerialized] public float progreso;

        public Vector3 Centro => transform.position;
        public bool Aspirando => fase == Fase.Ataque && ataque == Ataque.Aspiracion;
        public bool Anunciando => fase == Fase.Anuncio;
        public Ataque AtaqueActual => ataque;
        public float VelocidadAros => Mathf.Lerp(velocidadAro.x, velocidadAro.y, progreso);
        /// <summary>Radio de cada aro láser activo (para los bots).</summary>
        public IEnumerable<float> RadiosAros
        {
            get { foreach (var a in arosVivos) yield return a.radio; }
        }
        /// <summary>Las tuercas y pilas que andan sueltas (para los bots).</summary>
        public IEnumerable<Rigidbody> Proyectiles
        {
            get { foreach (var p in sueltos) if (p != null) yield return p; }
        }

        enum Fase { Descanso, Anuncio, Ataque }

        class AroVivo
        {
            public Transform t;
            public float radio;
            public float salida;
            public readonly HashSet<ControladorBicho> tocados = new HashSet<ControladorBicho>();
        }

        readonly List<AroVivo> arosVivos = new List<AroVivo>();
        readonly List<Rigidbody> sueltos = new List<Rigidbody>();
        Fase fase = Fase.Descanso;
        Ataque ataque;
        int ultimoAtaque = -1;
        float finFase, siguienteTuerca, siguientePolvo, giroTurbina;
        int arosPorSalir;
        float siguienteAro;

        void Start()
        {
            PonerCara(Cara.Normal);
            foreach (var a in aros) if (a != null) a.gameObject.SetActive(false);
            finFase = Time.time + 2f;
        }

        public void PonerCara(Cara cara)
        {
            if (caras == null) return;
            for (int i = 0; i < caras.Length; i++)
                if (caras[i] != null && caras[i].activeSelf != (i == (int)cara)) caras[i].SetActive(i == (int)cara);
        }

        /// <summary>Para todo (fin de ronda): fuera aros y tuercas, y cara de mareo.</summary>
        public void Parar()
        {
            activo = false;
            fase = Fase.Descanso;
            foreach (var a in arosVivos) if (a.t != null) a.t.gameObject.SetActive(false);
            arosVivos.Clear();
            foreach (var p in sueltos) if (p != null) Destroy(p.gameObject);
            sueltos.Clear();
            PonerCara(Cara.Mareado);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool atacando = fase == Fase.Ataque;
            if (cuerpo != null) cuerpo.Rotate(0f, giro * (atacando ? 1.8f : 1f) * dt, 0f, Space.World);
            if (turbina != null)
            {
                giroTurbina = Mathf.MoveTowards(giroTurbina, Aspirando ? 1400f : 180f, 900f * dt);
                turbina.Rotate(0f, giroTurbina * dt, 0f, Space.Self);
            }
            if (!activo) return;

            switch (fase)
            {
                case Fase.Descanso:
                    if (Time.time >= finFase) Anunciar();
                    break;
                case Fase.Anuncio:
                    if (Time.time >= finFase) Empezar();
                    break;
                case Fase.Ataque:
                    ActualizarAtaque();
                    break;
            }
        }

        void Anunciar()
        {
            int elegido;
            do elegido = UnityEngine.Random.Range(0, 3); while (elegido == ultimoAtaque);
            ultimoAtaque = elegido;
            ataque = (Ataque)elegido;
            fase = Fase.Anuncio;
            finFase = Time.time + anuncio;
            PonerCara(ataque == Ataque.OndaLaser ? Cara.Laser : ataque == Ataque.Aspiracion ? Cara.Risa : Cara.Enfadado);
            Sonidos.Reproducir(Sonido.Alarma, Centro, 0.7f, ataque == Ataque.OndaLaser ? 1.3f : ataque == Ataque.Aspiracion ? 0.8f : 1f);
            AlAnunciar?.Invoke(ataque);
        }

        void Empezar()
        {
            fase = Fase.Ataque;
            switch (ataque)
            {
                case Ataque.OndaLaser:
                    arosPorSalir = 1 + Mathf.RoundToInt(progreso * 2.2f);
                    siguienteAro = Time.time;
                    finFase = float.MaxValue; // acaba cuando el último aro llega al borde
                    break;
                case Ataque.Aspiracion:
                    finFase = Time.time + Mathf.Lerp(4.5f, 7f, progreso);
                    Sonidos.Reproducir(Sonido.Viento, Centro, 1f, 0.7f);
                    break;
                case Ataque.LluviaTuercas:
                    finFase = Time.time + Mathf.Lerp(5f, 8f, progreso);
                    siguienteTuerca = Time.time;
                    break;
            }
        }

        void Terminar()
        {
            fase = Fase.Descanso;
            finFase = Time.time + Mathf.Lerp(2.6f, 1.2f, progreso);
            PonerCara(Cara.Normal);
            AlTerminar?.Invoke(ataque);
        }

        void ActualizarAtaque()
        {
            switch (ataque)
            {
                case Ataque.OndaLaser:
                    if (arosPorSalir > 0 && Time.time >= siguienteAro)
                    {
                        SoltarAro();
                        arosPorSalir--;
                        siguienteAro = Time.time + Mathf.Lerp(1.1f, 0.8f, progreso);
                    }
                    if (arosPorSalir == 0 && arosVivos.Count == 0) Terminar();
                    break;
                case Ataque.Aspiracion:
                    if (Time.time >= siguientePolvo)
                    {
                        // Polvo que corre hacia el robot: se ve hacia dónde tira.
                        siguientePolvo = Time.time + 0.12f;
                        float a = UnityEngine.Random.value * Mathf.PI * 2f;
                        float r = UnityEngine.Random.Range(radioFoso, radioPista);
                        Efectos.Polvo(Centro + new Vector3(Mathf.Cos(a) * r, 0.2f, Mathf.Sin(a) * r), 0.5f);
                    }
                    if (Time.time >= finFase) Terminar();
                    break;
                case Ataque.LluviaTuercas:
                    if (Time.time >= siguienteTuerca)
                    {
                        Escupir();
                        siguienteTuerca = Time.time + Mathf.Lerp(0.6f, 0.3f, progreso) * UnityEngine.Random.Range(0.8f, 1.2f);
                    }
                    if (Time.time >= finFase) Terminar();
                    break;
            }
        }

        // ---------------------------------------------------------------------
        // Onda láser
        // ---------------------------------------------------------------------

        void SoltarAro()
        {
            foreach (var t in aros)
            {
                if (t == null || t.gameObject.activeSelf) continue;
                t.gameObject.SetActive(true);
                arosVivos.Add(new AroVivo { t = t, radio = radioRobot, salida = Time.time });
                Sonidos.Reproducir(Sonido.Pitido, Centro, 0.8f, 1.6f);
                return;
            }
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            // Los aros crecen y tumban a quien pillan en el suelo.
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
                    if (Mathf.Abs(rel.magnitude - a.radio) > 0.45f || altura > 0.55f || altura < -1f) continue;
                    a.tocados.Add(b);
                    Vector3 fuera = rel.sqrMagnitude > 0.01f ? rel.normalized : Vector3.forward;
                    b.Cuerpo.linearVelocity = fuera * 4f + Vector3.up * 4f;
                    b.Tropezar(0.9f);
                    Sonidos.Reproducir(Sonido.Derribo, b.transform.position, b.EsBot ? 0.6f : 1f, 1.2f);
                    Efectos.Impacto(b.transform.position + Vector3.up * 0.5f, new Color(1f, 0.3f, 0.8f), 0.6f);
                }
                if (a.radio > radioPista + 1.5f)
                {
                    if (a.t != null) a.t.gameObject.SetActive(false);
                    arosVivos.RemoveAt(i);
                }
            }

            // La aspiración tira de todos hacia el robot (hacia el foso).
            if (Aspirando)
            {
                float fuerza = Mathf.Lerp(aspiracion.x, aspiracion.y, progreso);
                foreach (var b in ControladorBicho.Todos)
                {
                    if (b == null || b.Retirado) continue;
                    Vector3 rel = b.transform.position - Centro;
                    rel.y = 0f;
                    float d = rel.magnitude;
                    if (d < radioRobot + 0.6f || d > radioPista + 3f) continue;
                    // Más fuerte cerca del robot.
                    float k = Mathf.Lerp(1.25f, 0.8f, Mathf.InverseLerp(radioFoso, radioPista, d));
                    b.Cuerpo.position -= rel / d * (fuerza * k * dt);
                }
            }
            sueltos.RemoveAll(p => p == null);
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
            float angulo = UnityEngine.Random.Range(-60f, 60f);
            Vector3 dir = Quaternion.Euler(0f, angulo, 0f) * delante.normalized;
            float distancia = UnityEngine.Random.Range(radioFoso + 1.5f, radioPista - 2f);
            Vector3 destino = Centro + dir * distancia;
            float tiempo = 1.1f;
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
