using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Anima a Triangulín (modelo con esqueleto: Modelos/TriangulinAnimado.fbx) a partir de la física:
    ///  - Le dice al Animator qué pasa (velocidad, en el aire, tumbado, celebrando) y este mezcla
    ///    las animaciones hechas en Blender: Quieto/Andar/Correr, Saltar/Caer, Tumbado y Celebrar.
    ///  - Orienta el dibujo aparte de la cápsula de física (gira suave hacia donde va, se inclina
    ///    solo un poco; tumbado sí sigue a la física: el porrazo se ve).
    ///  - Encima de la animación, por código: parpadeo, ojos como platos al salir volando y los
    ///    brazos estirados hacia lo que agarra.
    /// Lo configura el menú BichoShow > Personaje: aplicar Triangulín animado.
    /// </summary>
    [RequireComponent(typeof(ControladorBicho))]
    public class AnimadorBicho : MonoBehaviour
    {
        [Header("Piezas (las pone la herramienta)")]
        [Tooltip("Objeto que contiene todo el aspecto (\"Aspecto\"). Se orienta aparte de la cápsula de física.")]
        public Transform raizVisual;
        public Animator animador;
        public Transform ojoIzq, ojoDer;
        public Transform brazoIzq, brazoDer, antebrazoIzq, antebrazoDer, manoIzq, manoDer;
        [Tooltip("Antenas (opcional): se bambolean como un muelle con los acelerones, frenazos y botes.")]
        public Transform antenaIzq, antenaDer, puntaAntenaIzq, puntaAntenaDer;

        [Header("Antenas")]
        public float rigidezAntenas = 110f;
        public float amortiguacionAntenas = 6f;
        [Tooltip("Grados que se doblan por cada m/s² de acelerón.")]
        public float fuerzaAntenas = 1.4f;

        [Header("Giro")]
        [Tooltip("Inclinación hacia dentro de la curva al girar corriendo (grados por cada grado/segundo de giro).")]
        public float inclinacionEnCurva = 0.035f;
        public float inclinacionEnCurvaMaxima = 16f;

        [Header("Orientación del dibujo")]
        [Range(0f, 1f)] public float inclinacionFisica = 0.3f;
        public float inclinacionMaxima = 15f;
        public float suavizadoGiro = 12f;
        [Tooltip("Centro de la cápsula, en local: el dibujo se coloca centrado ahí.")]
        public Vector3 centroLocal = new Vector3(0f, 0.7f, 0f);

        [Header("Ritmo de las animaciones")]
        [Tooltip("Velocidad (m/s) a la que la animación de correr va a su ritmo normal.")]
        public float velocidadCorrer = 6f;

        static readonly int PVelocidad = Animator.StringToHash("Velocidad");
        static readonly int PEnSuelo = Animator.StringToHash("EnSuelo");
        static readonly int PVelY = Animator.StringToHash("VelY");
        static readonly int PTumbado = Animator.StringToHash("Tumbado");
        static readonly int PCelebrando = Animator.StringToHash("Celebrando");
        static readonly int PRitmo = Animator.StringToHash("Ritmo");

        ControladorBicho bicho;
        Rigidbody rb;
        Quaternion rotacionVisual;
        Vector3 ultimoAdelante = Vector3.forward;
        float ultimoEnSuelo;
        float finCelebrar = -10f;

        // Ojos
        Vector3 escalaOjoIzq, escalaOjoDer;
        int ejeArribaIzq = 1, ejeArribaDer = 1;
        float siguienteParpadeo, finParpadeo;

        // Brazos al agarrar
        Vector3 brazoAManoIzq, brazoAManoDer;
        Quaternion antebrazoReposoIzq, antebrazoReposoDer;
        float pesoAgarre;

        // Antenas (muelle) y giro
        Vector2 desvioAntenas, velocidadDesvio;
        Vector3 posicionAnterior, velocidadAnterior;
        bool hayPosicionAnterior;
        float inclinacionCurva;

        void Awake()
        {
            bicho = GetComponent<ControladorBicho>();
            rb = GetComponent<Rigidbody>();
            if (raizVisual == null) raizVisual = transform.Find("Aspecto");
            if (animador == null) animador = GetComponentInChildren<Animator>();
            if (animador != null)
            {
                animador.applyRootMotion = false;
                animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            rotacionVisual = transform.rotation;

            // Pose de reposo (la del modelo antes de que el Animator la toque).
            if (ojoIzq != null) { escalaOjoIzq = ojoIzq.localScale; ejeArribaIzq = EjeHaciaArriba(ojoIzq); }
            if (ojoDer != null) { escalaOjoDer = ojoDer.localScale; ejeArribaDer = EjeHaciaArriba(ojoDer); }
            if (brazoIzq != null && manoIzq != null) brazoAManoIzq = brazoIzq.InverseTransformDirection(manoIzq.position - brazoIzq.position);
            if (brazoDer != null && manoDer != null) brazoAManoDer = brazoDer.InverseTransformDirection(manoDer.position - brazoDer.position);
            if (antebrazoIzq != null) antebrazoReposoIzq = antebrazoIzq.localRotation;
            if (antebrazoDer != null) antebrazoReposoDer = antebrazoDer.localRotation;

            siguienteParpadeo = Time.time + Random.Range(1f, 4f);
        }

        void Start()
        {
            // Que no vayan todos al compás.
            if (animador != null && animador.runtimeAnimatorController != null)
                animador.Play(0, 0, Random.value);
        }

        /// <summary>Hace la animación de celebrar durante un rato (gana la ronda, se une al show…).</summary>
        public void Celebrar(float segundos) => finCelebrar = Mathf.Max(finCelebrar, Time.time + segundos);

        int EjeHaciaArriba(Transform t)
        {
            int mejor = 1;
            float mejorPunto = -1f;
            Vector3 arriba = transform.up;
            for (int i = 0; i < 3; i++)
            {
                Vector3 eje = Vector3.zero;
                eje[i] = 1f;
                float d = Mathf.Abs(Vector3.Dot(t.TransformDirection(eje), arriba));
                if (d > mejorPunto) { mejorPunto = d; mejor = i; }
            }
            return mejor;
        }

        void Update()
        {
            if (animador == null || bicho == null || bicho.Retirado) return;
            float dt = Time.deltaTime;

            Vector3 v = rb != null ? rb.linearVelocity : Vector3.zero;
            float rapidez = new Vector2(v.x, v.z).magnitude;
            if (bicho.EnSuelo) ultimoEnSuelo = Time.time;
            bool enAire = Time.time - ultimoEnSuelo > 0.12f; // un saltito del suelo no cuenta como "en el aire"

            animador.SetFloat(PVelocidad, rapidez, 0.08f, dt);
            animador.SetFloat(PVelY, v.y, 0.1f, dt);
            animador.SetBool(PEnSuelo, !enAire);
            animador.SetBool(PTumbado, bicho.Tumbado);
            animador.SetBool(PCelebrando, Time.time < finCelebrar && !bicho.Tumbado && !enAire);
            animador.SetFloat(PRitmo, Mathf.Clamp(rapidez > velocidadCorrer ? rapidez / velocidadCorrer : 1f, 0.8f, 1.5f));
        }

        void LateUpdate()
        {
            if (bicho == null || bicho.Retirado) return;
            float dt = Time.deltaTime;
            Orientar(dt);
            BrazosAlAgarrar(dt);
            Antenas(dt);
            Ojos();
        }

        /// <summary>
        /// Las antenas se comportan como un muelle: se quedan atrás al arrancar, se van hacia
        /// delante al frenar, se doblan al aterrizar y se balancean solas un poco.
        /// </summary>
        void Antenas(float dt)
        {
            if (antenaIzq == null && antenaDer == null) return;
            if (dt <= 0f || raizVisual == null) return;

            Vector3 posicion = raizVisual.position;
            if (!hayPosicionAnterior) { posicionAnterior = posicion; hayPosicionAnterior = true; }
            Vector3 velocidad = (posicion - posicionAnterior) / dt;
            Vector3 acelera = Vector3.ClampMagnitude((velocidad - velocidadAnterior) / dt, 60f);
            posicionAnterior = posicion;
            velocidadAnterior = velocidad;
            Vector3 local = raizVisual.InverseTransformDirection(acelera);

            // x: hacia los lados; y: adelante/atrás. La aceleración las empuja al revés (inercia).
            Vector2 empuje = new Vector2(local.x, -local.z + local.y * 0.35f) * fuerzaAntenas;
            float pasos = Mathf.Ceil(dt / 0.01f); // integrar en pasos pequeños: estable aunque baje el FPS
            float h = dt / pasos;
            for (int i = 0; i < pasos; i++)
            {
                velocidadDesvio += (-rigidezAntenas * desvioAntenas - amortiguacionAntenas * velocidadDesvio + empuje) * h;
                desvioAntenas += velocidadDesvio * h;
            }
            desvioAntenas = Vector2.ClampMagnitude(desvioAntenas, 50f);

            float vaiven = Mathf.Sin(Time.time * 2.3f) * 3f;
            Torcer(antenaIzq, desvioAntenas + new Vector2(vaiven, 0f), 1f);
            Torcer(antenaDer, desvioAntenas + new Vector2(-vaiven, 0f), 1f);
            Torcer(puntaAntenaIzq, desvioAntenas, 0.8f);
            Torcer(puntaAntenaDer, desvioAntenas, 0.8f);
        }

        void Torcer(Transform hueso, Vector2 desvio, float factor)
        {
            if (hueso == null) return;
            hueso.rotation = Quaternion.AngleAxis(desvio.y * factor, raizVisual.right)
                           * Quaternion.AngleAxis(-desvio.x * factor, raizVisual.forward)
                           * hueso.rotation;
        }

        /// <summary>
        /// El dibujo no copia tal cual la cápsula de física (que se tambalea y gira de golpe):
        /// mira suavemente hacia donde va el bicho y se inclina solo un poco con la física.
        /// Tumbado, sí sigue a la física por completo (el porrazo se ve).
        /// </summary>
        void Orientar(float dt)
        {
            if (raizVisual == null) return;

            Quaternion objetivo;
            if (bicho.Tumbado)
            {
                objetivo = transform.rotation;
            }
            else
            {
                Vector3 adelante = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                Vector3 adelanteAntes = ultimoAdelante;
                if (adelante.sqrMagnitude > 0.01f) ultimoAdelante = adelante.normalized;
                Quaternion rumbo = Quaternion.LookRotation(ultimoAdelante, Vector3.up);

                Quaternion inclinacion = Quaternion.FromToRotation(Vector3.up, transform.up);
                inclinacion.ToAngleAxis(out float angulo, out Vector3 eje);
                if (angulo > 180f) angulo -= 360f;
                float anguloVisual = Mathf.Clamp(angulo * inclinacionFisica, -inclinacionMaxima, inclinacionMaxima);

                // Al girar corriendo se inclina hacia dentro de la curva, como una moto.
                float giroPorSegundo = dt > 0f ? Vector3.SignedAngle(adelanteAntes, ultimoAdelante, Vector3.up) / dt : 0f;
                Vector3 v = rb != null ? rb.linearVelocity : Vector3.zero;
                float rapidez = Mathf.Clamp01(new Vector2(v.x, v.z).magnitude / 4f);
                float objetivoCurva = Mathf.Clamp(-giroPorSegundo * inclinacionEnCurva * rapidez, -inclinacionEnCurvaMaxima, inclinacionEnCurvaMaxima);
                inclinacionCurva = Mathf.Lerp(inclinacionCurva, objetivoCurva, 1f - Mathf.Exp(-8f * dt));

                objetivo = Quaternion.AngleAxis(anguloVisual, eje) * rumbo * Quaternion.AngleAxis(inclinacionCurva, Vector3.forward);
            }

            rotacionVisual = Quaternion.Slerp(rotacionVisual, objetivo, 1f - Mathf.Exp(-suavizadoGiro * dt));
            raizVisual.rotation = rotacionVisual;
            Vector3 centroFisico = transform.TransformPoint(centroLocal);
            raizVisual.position = centroFisico - rotacionVisual * centroLocal;
        }

        /// <summary>Al agarrar (o intentarlo), los dos brazos se estiran hacia el punto de la mano.</summary>
        void BrazosAlAgarrar(float dt)
        {
            pesoAgarre = Mathf.MoveTowards(pesoAgarre, bicho.ManoEstirada && !bicho.Tumbado ? 1f : 0f, dt * 7f);
            if (pesoAgarre <= 0.001f) return;
            Vector3 lado = (raizVisual != null ? raizVisual.right : transform.right) * 0.16f;
            Estirar(brazoIzq, antebrazoIzq, brazoAManoIzq, antebrazoReposoIzq, bicho.PuntoMano - lado);
            Estirar(brazoDer, antebrazoDer, brazoAManoDer, antebrazoReposoDer, bicho.PuntoMano + lado);
        }

        void Estirar(Transform brazo, Transform antebrazo, Vector3 haciaManoLocal, Quaternion antebrazoReposo, Vector3 objetivo)
        {
            if (brazo == null || haciaManoLocal.sqrMagnitude < 1e-6f) return;
            if (antebrazo != null) antebrazo.localRotation = Quaternion.Slerp(antebrazo.localRotation, antebrazoReposo, pesoAgarre);
            Vector3 actual = brazo.TransformDirection(haciaManoLocal);
            Vector3 deseada = objetivo - brazo.position;
            if (deseada.sqrMagnitude < 1e-4f) return;
            Quaternion giro = Quaternion.FromToRotation(actual, deseada);
            brazo.rotation = Quaternion.Slerp(Quaternion.identity, giro, pesoAgarre) * brazo.rotation;
        }

        /// <summary>Parpadea de vez en cuando; volando, ojos como platos; tirado en el suelo, cerrados.</summary>
        void Ojos()
        {
            if (Time.time >= siguienteParpadeo)
            {
                finParpadeo = Time.time + 0.12f;
                siguienteParpadeo = Time.time + Random.Range(2.5f, 5.5f);
            }
            Vector3 v = rb != null ? rb.linearVelocity : Vector3.zero;
            bool volando = !bicho.EnSuelo && (v.y < -6f || v.sqrMagnitude > 80f);
            bool cerrado = !volando && (Time.time < finParpadeo || (bicho.Tumbado && bicho.EnSuelo));
            Escalar(ojoIzq, escalaOjoIzq, ejeArribaIzq, cerrado, volando);
            Escalar(ojoDer, escalaOjoDer, ejeArribaDer, cerrado, volando);
        }

        static void Escalar(Transform ojo, Vector3 reposo, int ejeArriba, bool cerrado, bool volando)
        {
            if (ojo == null) return;
            Vector3 e = volando ? reposo * 1.3f : reposo;
            if (cerrado) e[ejeArriba] *= 0.1f;
            ojo.localScale = e;
        }
    }
}
