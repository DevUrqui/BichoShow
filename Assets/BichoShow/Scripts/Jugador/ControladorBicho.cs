using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Personaje físico y torpe. Todo el movimiento se hace con fuerzas sobre el Rigidbody,
    /// nunca moviendo el transform: así se tambalea, se cae, empuja y lo empujan.
    ///
    /// El cuerpo NO tiene la rotación bloqueada: un "muelle de equilibrio" (torque) lo mantiene
    /// de pie. Cuando el bicho tropieza, ese muelle se apaga un momento y cae como un saco;
    /// al terminar, se vuelve a encender más fuerte y el bicho se levanta solo.
    ///
    /// Lee los controles del componente PlayerInput del mismo objeto (acciones del mapa
    /// "Jugador": Mover, Saltar, Agarrar, Lanzarse).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ControladorBicho : MonoBehaviour
    {
        /// <summary>Todos los bichos activos en la escena (lo usa la cámara, entre otros).</summary>
        public static readonly List<ControladorBicho> Todos = new List<ControladorBicho>();

        /// <summary>Algún bicho ha tropezado por un golpe (no por lanzarse él mismo).</summary>
        public static event System.Action<ControladorBicho> AlgunoDerribado;

        /// <summary>Mientras es true, ningún bicho hace caso a su mando (menús, marcadores entre rondas).</summary>
        public static bool EntradaBloqueada;

        [Header("Movimiento")]
        [Tooltip("Velocidad máxima corriendo (m/s).")]
        public float velocidadMaxima = 6f;
        [Tooltip("Aceleración en el suelo (m/s²). Más bajo = más resbaladizo y torpe.")]
        public float aceleracion = 35f;
        [Tooltip("Fracción de la aceleración que se conserva en el aire.")]
        [Range(0f, 1f)] public float controlEnAire = 0.35f;
        [Tooltip("Altura sobre el centro de masas donde se empuja. Positivo = se inclina hacia delante al arrancar.")]
        public float alturaEmpuje = 0.25f;

        [Header("Giro y equilibrio")]
        [Tooltip("Lo rápido que se gira hacia donde apunta el stick.")]
        public float fuerzaGiro = 30f;
        public float amortiguacionGiro = 6f;
        [Tooltip("Fuerza del muelle que lo mantiene de pie. Más bajo = más bamboleo.")]
        public float fuerzaEquilibrio = 70f;
        public float amortiguacionEquilibrio = 9f;

        [Header("Salto")]
        [Tooltip("Velocidad vertical al saltar (m/s).")]
        public float velocidadSalto = 6.5f;
        [Tooltip("Margen para saltar justo después de salirse de un borde (s).")]
        public float tiempoCoyote = 0.12f;
        [Tooltip("Si pulsas saltar un poco antes de tocar el suelo, el salto se guarda este tiempo (s).")]
        public float bufferSalto = 0.12f;
        [Tooltip("Gravedad extra al caer, para que el salto no parezca de la Luna.")]
        public float multiplicadorGravedadCayendo = 1.6f;

        [Header("Suelo")]
        public float margenSuelo = 0.12f;
        public LayerMask capasSuelo = ~0;

        [Header("Torpeza")]
        [Tooltip("Cambio de velocidad recibido de un golpe (m/s) a partir del cual tropieza.")]
        public float golpeParaTropezar = 5.5f;
        [Tooltip("Cuánto tiempo está en el suelo tras un tropiezo (s). Más de 1.5 s frustra.")]
        public float duracionTropiezo = 0.9f;
        [Tooltip("Velocidad horizontal máxima, aunque te dé un golpe enorme (m/s).")]
        public float velocidadMaximaGolpe = 15f;
        [Tooltip("Velocidad máxima hacia arriba (m/s).")]
        public float velocidadMaximaSubida = 12f;
        [Tooltip("Si se inclina más que esto sin haber tropezado, recibe ayuda extra para levantarse.")]
        public float anguloTumbado = 60f;
        [Tooltip("Multiplicador del equilibrio durante el levantamiento.")]
        public float fuerzaLevantarse = 2.5f;
        public float duracionLevantarse = 0.5f;

        [Header("Agarrar")]
        public float alcanceAgarre = 0.8f;
        public float radioAgarre = 0.5f;
        [Tooltip("Fuerza a partir de la cual el agarre se suelta solo.")]
        public float fuerzaRoturaAgarre = 2500f;
        [Tooltip("Fracción de su fuerza que conserva un bicho mientras otro lo tiene agarrado.")]
        [Range(0f, 1f)] public float fuerzaSiTeAgarran = 0.3f;

        [Header("Lanzar lo agarrado (botón de lanzarse con algo agarrado)")]
        [Tooltip("Velocidad hacia delante que recibe lo lanzado (m/s).")]
        public float velocidadLanzarAdelante = 5f;
        [Tooltip("Velocidad hacia arriba que recibe lo lanzado (m/s). Da el arco.")]
        public float velocidadLanzarArriba = 4f;
        [Tooltip("Si lanzas a otro bicho, cuánto tiempo va dando tumbos (s).")]
        public float duracionVueloLanzado = 0.8f;
        [Tooltip("Pequeño empujón hacia atrás que recibe quien lanza (m/s).")]
        public float retrocesoAlLanzar = 1f;

        [Header("Lanzarse (placaje)")]
        public float impulsoLanzarse = 7f;
        public float impulsoLanzarseArriba = 2.5f;
        public float duracionLanzarse = 0.8f;
        public float enfriamientoLanzarse = 1.2f;

        [Header("Golpear (pelota, etc.)")]
        [Tooltip("Distancia desde el pecho a la que el botón de lanzarse golpea algo golpeable.")]
        public float alcanceGolpe = 1.8f;
        [Tooltip("Espera tras un golpe antes de poder volver a golpear o lanzarse (s).")]
        public float enfriamientoGolpe = 0.4f;

        [Header("Material físico del cuerpo")]
        [Tooltip("Rozamiento bajo para que no se quede pegado a las paredes.")]
        public float rozamientoCuerpo = 0.2f;
        public float rebote = 0.1f;

        [Header("Reaparición")]
        public float alturaCaidaMortal = -15f;

        [Header("Aspecto")]
        [Tooltip("Renderers que se tiñen con el color del jugador.")]
        public Renderer[] renderersColor;
        [Tooltip("Mano y brazo sencillos (esfera y cilindro) al agarrar. Desactivar si el aspecto del personaje anima los suyos.")]
        public bool manoSimple = true;

        /// <summary>El bicho está estirando la mano (agarrando o intentándolo).</summary>
        public bool ManoEstirada { get; private set; }
        /// <summary>Dónde está (o hacia dónde se estira) la mano, en el mundo.</summary>
        public Vector3 PuntoMano { get; private set; }

        // --- Estado público (lo usarán las pruebas, la cámara y los efectos) ---
        public bool EnSuelo { get; private set; }
        /// <summary>Fuera del juego (su jugador está saboteando).</summary>
        public bool Retirado { get; private set; }
        /// <summary>Hacia dónde quiere ir ahora mismo (en el suelo, longitud 0-1 según el stick).</summary>
        public Vector3 Intencion { get; private set; }
        public bool Tumbado => Time.time < finTumbado;
        public bool Agarrando => agarre != null;
        /// <summary>Otro bicho me tiene agarrado.</summary>
        public bool TeTienenAgarrado => agarradoPor > 0;
        public int IndiceJugador => playerInput != null ? playerInput.playerIndex : indiceSinMando;
        /// <summary>Lo controla la máquina (o cualquier fuente que no sea un mando).</summary>
        public bool EsBot => entradaExterna != null || GetComponent<IEntradaBicho>() != null;
        /// <summary>Lo que tengo agarrado, si es algo con Rigidbody.</summary>
        public Rigidbody CuerpoAgarrado => agarre != null ? agarre.connectedBody : null;
        public Color ColorJugador { get; private set; } = Color.white;
        public Rigidbody Cuerpo => rb;

        /// <summary>Se lanza cada vez que el bicho tropieza o se lanza al suelo.</summary>
        public event System.Action<ControladorBicho> AlTropezar;
        /// <summary>Se lanza al caerse del escenario, antes de reaparecer.</summary>
        public event System.Action<ControladorBicho> AlCaerseDelMapa;
        /// <summary>El bicho hace algo (saltar, placaje, golpear, agarrar, lanzar): para sonidos y efectos.</summary>
        public event System.Action<ControladorBicho, AccionBicho> AlHacer;
        /// <summary>Le han dado un golpe que lo tira (cambio de velocidad en m/s y punto del choque).</summary>
        public event System.Action<ControladorBicho, float, Vector3> AlRecibirGolpe;

        public enum AccionBicho { Saltar, Placaje, Golpear, Agarrar, Lanzar }

        Rigidbody rb;
        CapsuleCollider capsula;
        PlayerInput playerInput;
        InputAction accionMover, accionSaltar, accionAgarrar, accionLanzarse;
        IEntradaBicho entradaExterna;
        int indiceSinMando;

        Vector2 entradaMover;
        Vector3 direccionMirada;
        float ultimoEnSuelo = -10f;
        float ultimoSaltoPulsado = -10f;
        float ultimoLanzamiento = -10f;
        float finTumbado = -10f;
        float finLevantarse = -10f;
        bool lanzarsePendiente;
        float finResbalon = -10f;
        float factorResbalon = 1f;
        bool agarreBloqueado; // tras soltarse a la fuerza, hay que volver a pulsar
        bool pulsandoAgarrar;
        FixedJoint agarre;
        bool agarreACuerpo;             // agarré algo con Rigidbody (no el escenario)
        ControladorBicho bichoAgarrado; // si lo que agarro es otro jugador
        int agarradoPor;                // cuántos bichos me agarran a mí
        Transform camara;
        Transform mano, brazo;

        Vector3 puntoReaparicion;
        Quaternion rotacionReaparicion = Quaternion.identity;

        readonly RaycastHit[] impactosSuelo = new RaycastHit[8];
        readonly Collider[] candidatosAgarre = new Collider[12];

        bool inicializado;

        // Con "Enter Play Mode Options" sin recarga de dominio, los estáticos sobreviven entre partidas.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos()
        {
            Todos.Clear();
            AlgunoDerribado = null;
            EntradaBloqueada = false;
        }

        void Awake() => Inicializar();

        /// <summary>
        /// PlayerInputManager avisa de "jugador unido" desde el OnEnable de PlayerInput, que puede
        /// ejecutarse ANTES que nuestro Awake. Por eso la API pública llama aquí primero.
        /// </summary>
        void Inicializar()
        {
            if (inicializado) return;
            inicializado = true;

            rb = GetComponent<Rigidbody>();
            capsula = GetComponent<CapsuleCollider>();
            playerInput = GetComponent<PlayerInput>();

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.maxAngularVelocity = 20f;

            if (capsula != null && capsula.sharedMaterial == null)
            {
                capsula.sharedMaterial = new PhysicsMaterial("Bicho")
                {
                    dynamicFriction = rozamientoCuerpo,
                    staticFriction = rozamientoCuerpo,
                    bounciness = rebote,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Maximum
                };
            }

            direccionMirada = Aplanar(transform.forward);
            puntoReaparicion = transform.position;
            rotacionReaparicion = transform.rotation;

            CrearManoVisual();

            // Sonidos y efectos del bicho (aplastarse al caer, polvo, estrellitas...).
            if (!TryGetComponent(out JugoBicho _)) gameObject.AddComponent<JugoBicho>();
        }

        void OnEnable() => Todos.Add(this);

        void OnDisable()
        {
            Todos.Remove(this);
            Soltar();
        }

        void OnDestroy()
        {
            if (mano != null) Destroy(mano.gameObject);
            if (brazo != null) Destroy(brazo.gameObject);
        }

        void Start()
        {
            if (Camera.main != null) camara = Camera.main.transform;

            entradaExterna = GetComponent<IEntradaBicho>();
            if (entradaExterna != null) return; // lo controla un bot u otra fuente

            if (playerInput != null && playerInput.actions != null)
            {
                var acciones = playerInput.actions;
                accionMover = acciones.FindAction("Mover");
                accionSaltar = acciones.FindAction("Saltar");
                accionAgarrar = acciones.FindAction("Agarrar");
                accionLanzarse = acciones.FindAction("Lanzarse");
            }
            else
            {
                Debug.LogWarning($"{name}: no tiene PlayerInput con acciones; no se podrá controlar.", this);
            }
        }

        // ---------------------------------------------------------------------
        // Update: solo leer los mandos (los botones se leen por frame)
        // ---------------------------------------------------------------------
        void Update()
        {
            if (Retirado) return;

            if (EntradaBloqueada)
            {
                entradaMover = Vector2.zero;
                pulsandoAgarrar = false;
                return;
            }

            if (entradaExterna != null)
            {
                entradaMover = Vector2.ClampMagnitude(entradaExterna.Mover, 1f);
                if (entradaExterna.ConsumirSaltar()) ultimoSaltoPulsado = Time.time;
                if (entradaExterna.ConsumirLanzarse()) lanzarsePendiente = true;
                pulsandoAgarrar = entradaExterna.Agarrar;
            }
            else
            {
                entradaMover = accionMover != null ? accionMover.ReadValue<Vector2>() : Vector2.zero;

                if (accionSaltar != null && accionSaltar.WasPressedThisFrame())
                    ultimoSaltoPulsado = Time.time;

                if (accionLanzarse != null && accionLanzarse.WasPressedThisFrame())
                    lanzarsePendiente = true;

                pulsandoAgarrar = accionAgarrar != null && accionAgarrar.IsPressed();
            }

            if (!pulsandoAgarrar)
                agarreBloqueado = false;

            if (transform.position.y < alturaCaidaMortal)
            {
                AlCaerseDelMapa?.Invoke(this);
                Reaparecer();
            }
        }

        void LateUpdate()
        {
            if (!Retirado) ActualizarManoVisual();
        }

        // ---------------------------------------------------------------------
        // FixedUpdate: toda la física
        // ---------------------------------------------------------------------
        void FixedUpdate()
        {
            if (Retirado) return;
            ComprobarSuelo();

            Vector3 direccion = DireccionDesdeCamara(entradaMover);
            Intencion = Tumbado ? Vector3.zero : direccion;
            if (direccion.sqrMagnitude > 0.01f)
                direccionMirada = direccion.normalized;

            if (!Tumbado)
            {
                Mover(direccion);
                Saltar();
                Lanzarse();
            }
            else
            {
                lanzarsePendiente = false;
            }

            Equilibrar();
            Agarrar();
            GravedadExtra();
            LimitarVelocidad();
        }

        void ComprobarSuelo()
        {
            float radio = capsula != null ? capsula.radius * 0.9f : 0.4f;
            float mitad = capsula != null ? capsula.height * 0.5f : 0.6f;
            Vector3 origen = rb.worldCenterOfMass;
            float distancia = Mathf.Max(0f, mitad - radio) + margenSuelo;

            EnSuelo = false;
            int n = Physics.SphereCastNonAlloc(origen, radio, Vector3.down, impactosSuelo, distancia,
                                               capasSuelo, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var impacto = impactosSuelo[i];
                if (impacto.rigidbody == rb) continue;          // yo mismo
                if (impacto.distance <= 0f) continue;           // ya solapado al empezar
                if (impacto.normal.y < 0.5f) continue;          // pared, no suelo
                EnSuelo = true;
                break;
            }

            if (EnSuelo) ultimoEnSuelo = Time.time;
        }

        void Mover(Vector3 direccion)
        {
            Vector3 velocidadActual = Aplanar(rb.linearVelocity);
            Vector3 velocidadDeseada = direccion * velocidadMaxima;

            float acel = aceleracion * (EnSuelo ? 1f : controlEnAire);
            // Si me tienen agarrado, forcejeo pero con poca fuerza: así quien agarra me puede arrastrar.
            if (TeTienenAgarrado) acel *= fuerzaSiTeAgarran;
            // Sobre aceite (u otra cosa resbaladiza) casi no hay agarre: ni para arrancar ni para frenar.
            if (Time.time < finResbalon) acel *= factorResbalon;
            Vector3 cambio = Vector3.ClampMagnitude((velocidadDeseada - velocidadActual) / Time.fixedDeltaTime, acel);

            // Empujar un poco por encima del centro de masas hace que se incline al arrancar y frenar.
            Vector3 punto = rb.worldCenterOfMass + Vector3.up * alturaEmpuje;
            rb.AddForceAtPosition(cambio * rb.mass, punto, ForceMode.Force);
        }

        void Saltar()
        {
            bool puedeSaltar = Time.time - ultimoEnSuelo <= tiempoCoyote;
            bool quiereSaltar = Time.time - ultimoSaltoPulsado <= bufferSalto;
            if (!puedeSaltar || !quiereSaltar) return;

            Vector3 v = rb.linearVelocity;
            v.y = velocidadSalto;
            rb.linearVelocity = v;

            ultimoEnSuelo = -10f;
            ultimoSaltoPulsado = -10f;
            AlHacer?.Invoke(this, AccionBicho.Saltar);
        }

        void Lanzarse()
        {
            if (!lanzarsePendiente) return;
            lanzarsePendiente = false;
            if (Time.time - ultimoLanzamiento < enfriamientoLanzarse) return;
            ultimoLanzamiento = Time.time;

            // Con algo agarrado, el botón lanza lo agarrado en vez de tirarte en plancha.
            if (agarre != null)
            {
                LanzarAgarrado();
                return;
            }

            // Con algo golpeable cerca (la pelota), el botón golpea en vez de tirarte.
            if (IntentarGolpear())
            {
                ultimoLanzamiento = Time.time - enfriamientoLanzarse + enfriamientoGolpe;
                AlHacer?.Invoke(this, AccionBicho.Golpear);
                return;
            }

            Vector3 adelante = direccionMirada;
            rb.AddForce(adelante * impulsoLanzarse + Vector3.up * impulsoLanzarseArriba, ForceMode.VelocityChange);
            // Un empujoncito de giro para que caiga de morros.
            rb.AddTorque(Vector3.Cross(Vector3.up, adelante) * 8f, ForceMode.VelocityChange);
            Tropezar(duracionLanzarse);
            AlHacer?.Invoke(this, AccionBicho.Placaje);
        }

        void Equilibrar()
        {
            if (Tumbado) return;

            float multiplicador = Time.time < finLevantarse ? fuerzaLevantarse : 1f;

            // Si está muy inclinado sin haber tropezado (se cayó por un borde, etc.), ayudarle.
            float inclinacion = Vector3.Angle(transform.up, Vector3.up);
            if (inclinacion > anguloTumbado) multiplicador = Mathf.Max(multiplicador, fuerzaLevantarse);

            // Muelle que lleva "arriba" del cuerpo hacia arriba del mundo.
            Vector3 eje = Vector3.Cross(transform.up, Vector3.up);
            if (eje.sqrMagnitude < 1e-6f && inclinacion > 90f) eje = transform.right; // boca abajo exacto
            Vector3 torqueEquilibrio = eje.normalized * (inclinacion * Mathf.Deg2Rad) * fuerzaEquilibrio * multiplicador;
            Vector3 giroHorizontal = Vector3.ProjectOnPlane(rb.angularVelocity, Vector3.up);
            torqueEquilibrio -= giroHorizontal * amortiguacionEquilibrio;
            rb.AddTorque(torqueEquilibrio, ForceMode.Acceleration);

            // Girar sobre sí mismo hacia donde apunta el stick.
            Vector3 adelante = Aplanar(transform.forward);
            if (adelante.sqrMagnitude < 0.01f) return; // tumbado de cara, sin "delante" claro
            float anguloGiro = Vector3.SignedAngle(adelante.normalized, direccionMirada, Vector3.up);
            float torqueGiro = anguloGiro * Mathf.Deg2Rad * fuerzaGiro - rb.angularVelocity.y * amortiguacionGiro;
            rb.AddTorque(Vector3.up * torqueGiro, ForceMode.Acceleration);
        }

        void Agarrar()
        {
            if (!pulsandoAgarrar || Tumbado || AgarreRoto())
            {
                Soltar();
                return;
            }
            if (agarre != null || agarreBloqueado) return;

            Vector3 centro = PuntoAlcance();
            int n = Physics.OverlapSphereNonAlloc(centro, radioAgarre, candidatosAgarre, ~0, QueryTriggerInteraction.Ignore);

            // Preferimos lo que se mueve (bichos, cajas, pelota) antes que el escenario.
            Collider mejor = null;
            float mejorPuntuacion = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var c = candidatosAgarre[i];
                if (c.attachedRigidbody == rb) continue;
                if (c.attachedRigidbody != null && c.attachedRigidbody.GetComponent<NoAgarrable>() != null) continue;
                float puntuacion = (c.ClosestPointOnBounds(centro) - centro).sqrMagnitude;
                if (c.attachedRigidbody == null) puntuacion += 100f;
                if (puntuacion < mejorPuntuacion) { mejorPuntuacion = puntuacion; mejor = c; }
            }
            if (mejor == null) return;

            Vector3 puntoAgarre = PuntoMasCercano(mejor, centro);
            agarre = gameObject.AddComponent<FixedJoint>();
            agarre.connectedBody = mejor.attachedRigidbody; // null = agarrado al escenario
            agarre.anchor = transform.InverseTransformPoint(puntoAgarre);
            agarre.breakForce = fuerzaRoturaAgarre;
            agarre.breakTorque = fuerzaRoturaAgarre;
            // Sin choques entre quien agarra y lo agarrado: el joint los mete uno dentro del otro y
            // la física respondía con empujones enormes (el bicho tropezaba y soltaba la tele).
            agarre.enableCollision = false;
            agarreACuerpo = mejor.attachedRigidbody != null;

            bichoAgarrado = mejor.attachedRigidbody != null
                ? mejor.attachedRigidbody.GetComponent<ControladorBicho>()
                : null;
            if (bichoAgarrado != null) bichoAgarrado.agarradoPor++;
            if (agarreACuerpo) AlHacer?.Invoke(this, AccionBicho.Agarrar);
        }

        bool IntentarGolpear()
        {
            int n = Physics.OverlapSphereNonAlloc(rb.worldCenterOfMass, alcanceGolpe, candidatosAgarre, ~0, QueryTriggerInteraction.Ignore);
            IGolpeable mejor = null;
            float mejorDistancia = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var c = candidatosAgarre[i];
                if (c.attachedRigidbody == rb) continue;
                var golpeable = c.attachedRigidbody != null
                    ? c.attachedRigidbody.GetComponent<IGolpeable>()
                    : c.GetComponent<IGolpeable>();
                if (golpeable == null) continue;
                float d = (c.ClosestPointOnBounds(rb.worldCenterOfMass) - rb.worldCenterOfMass).sqrMagnitude;
                if (d < mejorDistancia) { mejorDistancia = d; mejor = golpeable; }
            }
            return mejor != null && mejor.Golpear(this, direccionMirada, !EnSuelo);
        }

        /// <summary>Durante un rato el bicho casi no tiene agarre (aceite, hielo…). factor 0 = patinar del todo.</summary>
        public void Resbalar(float factor, float duracion)
        {
            factorResbalon = Mathf.Clamp01(factor);
            finResbalon = Mathf.Max(finResbalon, Time.time + duracion);
        }

        void LanzarAgarrado()
        {
            Rigidbody objetivo = agarre.connectedBody;
            ControladorBicho bicho = bichoAgarrado;

            Soltar();
            agarreBloqueado = true; // que no lo vuelva a agarrar al instante si sigues con el gatillo
            if (objetivo == null) return; // agarrado al escenario: no hay nada que lanzar

            // Lo que pesa más que yo sale con menos fuerza.
            float factorMasa = Mathf.Clamp(rb.mass / objetivo.mass, 0.3f, 1f);
            Vector3 adelante = direccionMirada;
            Vector3 velocidad = adelante * velocidadLanzarAdelante + Vector3.up * velocidadLanzarArriba;

            objetivo.AddForce(velocidad * factorMasa, ForceMode.VelocityChange);
            objetivo.AddTorque(Vector3.Cross(Vector3.up, adelante) * 5f, ForceMode.VelocityChange); // que vaya dando vueltas
            rb.AddForce(-adelante * retrocesoAlLanzar, ForceMode.VelocityChange);

            if (bicho != null) bicho.Tropezar(duracionVueloLanzado);
            AlHacer?.Invoke(this, AccionBicho.Lanzar);
        }

        void Soltar()
        {
            if (agarre != null) Destroy(agarre);
            agarre = null;
            agarreACuerpo = false;
            LiberarBichoAgarrado();
        }

        /// <summary>Suelta lo agarrado si es este cuerpo (p. ej. un objeto que se entrega o se rompe).</summary>
        public void SoltarSi(Rigidbody cuerpo)
        {
            if (agarre != null && cuerpo != null && agarre.connectedBody == cuerpo) Soltar();
        }

        /// <summary>
        /// El agarre ya no tiene sentido: lo agarrado se destruyó (Unity lo convertiría en un
        /// agarre al mundo y el bicho se quedaría colgado en el aire) o se teletransportó lejos.
        /// </summary>
        bool AgarreRoto()
        {
            if (agarre == null) return false;
            if (agarreACuerpo && agarre.connectedBody == null) return true;

            Vector3 miPunto = transform.TransformPoint(agarre.anchor);
            Vector3 suPunto = agarre.connectedBody != null
                ? agarre.connectedBody.transform.TransformPoint(agarre.connectedAnchor)
                : agarre.connectedAnchor;
            return (miPunto - suPunto).sqrMagnitude > 1.5f * 1.5f;
        }

        void LiberarBichoAgarrado()
        {
            if (bichoAgarrado == null) return;
            bichoAgarrado.agarradoPor = Mathf.Max(0, bichoAgarrado.agarradoPor - 1);
            bichoAgarrado = null;
        }

        void OnJointBreak(float fuerza)
        {
            // Unity destruye el joint solo; obligamos a soltar el botón para volver a agarrar.
            agarre = null;
            agarreBloqueado = true;
            LiberarBichoAgarrado();
        }

        /// <summary>Centro de la esfera donde se busca qué agarrar: delante del bicho, a la altura del pecho.</summary>
        Vector3 PuntoAlcance() => rb.worldCenterOfMass + direccionMirada * alcanceAgarre;

        // ---------------------------------------------------------------------
        // Mano visible: se ve dónde intentas agarrar y qué tienes agarrado
        // ---------------------------------------------------------------------

        void CrearManoVisual()
        {
            if (!manoSimple) return; // el aspecto del personaje anima sus propios brazos
            mano = PiezaVisual(PrimitiveType.Sphere, "Mano");
            brazo = PiezaVisual(PrimitiveType.Cylinder, "Brazo");
            mano.localScale = Vector3.one * 0.24f;
        }

        Transform PiezaVisual(PrimitiveType tipo, string nombre)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = $"{name} {nombre}";
            Destroy(go.GetComponent<Collider>());
            if (renderersColor != null && renderersColor.Length > 0 && renderersColor[0] != null)
                go.GetComponent<Renderer>().sharedMaterial = renderersColor[0].sharedMaterial;
            go.SetActive(false);
            // Hijo del bicho para que viaje con él entre escenas (su posición se fija cada frame).
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        void ActualizarManoVisual()
        {
            // Dónde va la mano (lo usa también el aspecto del personaje, si anima sus propios brazos).
            ManoEstirada = agarre != null || (pulsandoAgarrar && !Tumbado && !agarreBloqueado);
            PuntoMano = agarre != null
                ? transform.TransformPoint(agarre.anchor)
                : transform.position + Vector3.up * 0.6f + direccionMirada * (alcanceAgarre + radioAgarre * 0.5f);

            if (mano == null || brazo == null) return;
            bool visible = ManoEstirada;
            mano.gameObject.SetActive(visible);
            brazo.gameObject.SetActive(visible);
            if (!visible) return;

            // Con el joint, la mano va al punto agarrado; si no, estira el brazo hacia delante.
            Vector3 hombro = transform.TransformPoint(new Vector3(0f, 0.7f, 0.25f));
            Vector3 puntoMano = PuntoMano;

            mano.position = puntoMano;
            Vector3 tramo = puntoMano - hombro;
            brazo.position = hombro + tramo * 0.5f;
            brazo.rotation = tramo.sqrMagnitude > 1e-6f ? Quaternion.FromToRotation(Vector3.up, tramo) : Quaternion.identity;
            brazo.localScale = new Vector3(0.11f, tramo.magnitude * 0.5f, 0.11f); // el cilindro mide 2 de alto
        }

        void GravedadExtra()
        {
            if (rb.linearVelocity.y < 0f)
                rb.AddForce(Physics.gravity * (multiplicadorGravedadCayendo - 1f), ForceMode.Acceleration);
        }

        void OnCollisionEnter(Collision choque)
        {
            // Solo tropezamos con cosas que se mueven (otros bichos, cajas, trampas),
            // no al aterrizar en el suelo tras un salto.
            if (choque.rigidbody == null) return;
            if (EsDeMiCarga(choque.rigidbody)) return;

            float cambioVelocidad = choque.impulse.magnitude / rb.mass;
            if (cambioVelocidad >= golpeParaTropezar)
            {
                bool yaEstaba = Tumbado;
                OrigenUltimoGolpe = NombreOrigen(choque.rigidbody);
                if (!yaEstaba)
                    AlRecibirGolpe?.Invoke(this, cambioVelocidad, choque.contactCount > 0 ? choque.GetContact(0).point : rb.worldCenterOfMass);
                Tropezar(duracionTropiezo);
                if (!yaEstaba) AlgunoDerribado?.Invoke(this);
            }
        }

        /// <summary>
        /// Choques que no deben tumbar: con lo que llevo agarrado, con quien me lleva agarrado y
        /// con quien carga el mismo objeto que yo (al llevar el sofá entre dos se dan codazos).
        /// </summary>
        /// <summary>Qué le dio el último golpe que lo tiró (para las estadísticas).</summary>
        public string OrigenUltimoGolpe { get; private set; }

        static string NombreOrigen(Rigidbody otro)
        {
            var bicho = otro.GetComponent<ControladorBicho>();
            if (bicho != null) return $"J{bicho.IndiceJugador + 1}";
            var trampa = otro.GetComponentInParent<TrampaSaboteable>();
            if (trampa != null) return trampa.Nombre;
            var objeto = otro.GetComponent<ObjetoFragil>();
            return objeto != null ? objeto.nombreObjeto : otro.name;
        }

        bool EsDeMiCarga(Rigidbody otro)
        {
            Rigidbody mia = CuerpoAgarrado;
            if (mia != null && otro == mia) return true;
            var bicho = otro.GetComponent<ControladorBicho>();
            if (bicho == null) return false;
            if (bicho.CuerpoAgarrado == rb) return true;
            return mia != null && bicho.CuerpoAgarrado == mia;
        }

        /// <summary>
        /// Tope de velocidad: un golpe muy gordo (una trampa en turbo, un choque raro de la física)
        /// lanza al bicho por los aires, pero no lo dispara como un cañón fuera del mapa.
        /// </summary>
        void LimitarVelocidad()
        {
            Vector3 v = rb.linearVelocity;
            Vector3 horizontal = new Vector3(v.x, 0f, v.z);
            bool cambia = false;
            if (horizontal.sqrMagnitude > velocidadMaximaGolpe * velocidadMaximaGolpe)
            {
                horizontal = horizontal.normalized * velocidadMaximaGolpe;
                cambia = true;
            }
            if (v.y > velocidadMaximaSubida) { v.y = velocidadMaximaSubida; cambia = true; }
            if (cambia) rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
        }

        // ---------------------------------------------------------------------
        // API pública
        // ---------------------------------------------------------------------

        /// <summary>Tira al bicho al suelo durante un tiempo. Al acabar se levanta solo.</summary>
        public void Tropezar(float duracion)
        {
            Inicializar();
            float fin = Time.time + duracion;
            if (fin <= finTumbado) return;
            bool yaEstabaTumbado = Tumbado;
            finTumbado = fin;
            finLevantarse = fin + duracionLevantarse;
            Soltar();
            if (!yaEstabaTumbado) AlTropezar?.Invoke(this);
        }

        /// <summary>Cambia dónde reaparece si se cae (puntos de control), sin moverlo ahora.</summary>
        public void FijarPuntoReaparicion(Vector3 posicion, Quaternion rotacion)
        {
            Inicializar();
            puntoReaparicion = posicion;
            rotacionReaparicion = rotacion;
        }

        /// <summary>Coloca al bicho en un punto y lo guarda como punto de reaparición.</summary>
        public void Colocar(Vector3 posicion, Quaternion rotacion)
        {
            Inicializar();
            puntoReaparicion = posicion;
            rotacionReaparicion = rotacion;
            direccionMirada = Aplanar(rotacion * Vector3.forward);
            Teletransportar(posicion, rotacion);
        }

        /// <summary>
        /// Saca al bicho del juego sin destruirlo (invisible, sin física ni colisiones), por
        /// ejemplo mientras su jugador hace de saboteador. Al volver, reaparece en su sitio.
        /// </summary>
        public void Retirar(bool retirar)
        {
            Inicializar();
            if (Retirado == retirar) return;
            Retirado = retirar;
            Soltar();
            entradaMover = Vector2.zero;
            pulsandoAgarrar = false;

            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = !retirar;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = !retirar;
            if (mano != null) mano.gameObject.SetActive(false);
            if (brazo != null) brazo.gameObject.SetActive(false);

            if (retirar)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                Todos.Remove(this);
            }
            else
            {
                rb.isKinematic = false;
                if (!Todos.Contains(this)) Todos.Add(this);
                Reaparecer();
            }
        }

        public void Reaparecer()
        {
            Inicializar();
            Soltar();
            finTumbado = -10f;
            Teletransportar(puntoReaparicion, rotacionReaparicion);
            direccionMirada = Aplanar(rotacionReaparicion * Vector3.forward);
            AlReaparecer?.Invoke(this);
        }

        /// <summary>Acaba de reaparecer en su punto (tras caerse, al empezar una ronda...).</summary>
        public event System.Action<ControladorBicho> AlReaparecer;

        /// <summary>Número de jugador para los bichos sin mando (bots). Los de mando usan el de PlayerInput.</summary>
        public void AsignarIndice(int indice) => indiceSinMando = indice;

        public void AsignarColor(Color color)
        {
            ColorJugador = color;
            if (renderersColor == null) return;
            foreach (var r in renderersColor)
            {
                if (r == null) continue;
                var mat = r.material; // instancia propia para este bicho
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else mat.color = color;
            }

            // La mano y el brazo comparten el material ya teñido del cuerpo.
            if (renderersColor.Length > 0 && renderersColor[0] != null)
            {
                var matCuerpo = renderersColor[0].sharedMaterial;
                if (mano != null) mano.GetComponent<Renderer>().sharedMaterial = matCuerpo;
                if (brazo != null) brazo.GetComponent<Renderer>().sharedMaterial = matCuerpo;
            }
        }

        // ---------------------------------------------------------------------
        // Utilidades
        // ---------------------------------------------------------------------

        void Teletransportar(Vector3 posicion, Quaternion rotacion)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(posicion, rotacion);
            rb.position = posicion;
            rb.rotation = rotacion;
        }

        Vector3 DireccionDesdeCamara(Vector2 entrada)
        {
            if (entrada.sqrMagnitude < 0.0001f) return Vector3.zero;

            Vector3 adelante = Vector3.forward, derecha = Vector3.right;
            // La cámara cambia con cada escena: si la anterior ya no existe, buscar la nueva.
            if (camara == null && Camera.main != null) camara = Camera.main.transform;
            if (camara != null)
            {
                adelante = Aplanar(camara.forward).normalized;
                derecha = Aplanar(camara.right).normalized;
            }
            Vector3 dir = adelante * entrada.y + derecha * entrada.x;
            return Vector3.ClampMagnitude(dir, 1f);
        }

        static Vector3 Aplanar(Vector3 v) => new Vector3(v.x, 0f, v.z);

        static Vector3 PuntoMasCercano(Collider c, Vector3 punto)
        {
            bool soportaClosestPoint = c is BoxCollider || c is SphereCollider || c is CapsuleCollider
                                       || (c is MeshCollider m && m.convex);
            return soportaClosestPoint ? c.ClosestPoint(punto) : c.ClosestPointOnBounds(punto);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 centro = (rb != null ? rb.worldCenterOfMass : transform.position + Vector3.up * 0.6f)
                             + (Application.isPlaying ? direccionMirada : transform.forward) * alcanceAgarre;
            Gizmos.DrawWireSphere(centro, radioAgarre);
        }
    }
}
