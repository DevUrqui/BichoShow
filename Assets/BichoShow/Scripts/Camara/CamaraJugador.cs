using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BichoShow
{
    /// <summary>
    /// Cámara de un jugador en las carreras, al estilo de Fall Guys: detrás de su bicho, baja y
    /// cerca, mirando hacia delante. Se gira con el stick derecho (o el ratón con teclado). Si no
    /// se toca, se va colocando sola mirando hacia donde sigue la carrera (la ruta de los bots),
    /// así quien juega con teclado no tiene que preocuparse. Si algo se mete entre la cámara y el
    /// bicho (una pared, el fregadero…), se acerca. La crea y la coloca en pantalla
    /// CamarasPorJugador; el stick del bicho va relativo a esta cámara.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CamaraJugador : MonoBehaviour
    {
        public static readonly List<CamaraJugador> Todas = new List<CamaraJugador>();

        [Tooltip("El bicho al que sigue (en un invitado, su marioneta).")]
        public ControladorBicho bicho;
        [Tooltip("Mando de este jugador (para el stick derecho).")]
        public PlayerInput entrada;
        [Tooltip("Número de mando en este ordenador (el invitado lo usa para su stick).")]
        public int local;

        [Header("Encuadre")]
        public float distancia = 8.5f;
        public float alturaMirada = 1.4f;
        [Tooltip("Inclinación de partida (grados hacia abajo).")]
        public float inclinacion = 16f;
        public float inclinacionMinima = -5f;
        public float inclinacionMaxima = 60f;
        public float campoVision = 60f;

        [Header("Control")]
        [Tooltip("Grados por segundo con el stick derecho a tope.")]
        public float velocidadStick = 160f;
        [Tooltip("Grados por unidad de movimiento del ratón.")]
        public float sensibilidadRaton = 0.15f;
        [Tooltip("Tras mover la cámara a mano, segundos hasta que vuelve a colocarse sola.")]
        public float esperaAuto = 1.5f;
        [Tooltip("Lo que tarda en colocarse sola mirando a la carrera (s).")]
        public float suavizadoAuto = 0.8f;

        public Camera Camara { get; private set; }
        /// <summary>Lo que sigue ahora (su bicho o, si está fuera, otro).</summary>
        public ControladorBicho Siguiendo { get; set; }

        float yaw, pitch, velocidadYaw, velocidadPitch, ultimoManual = -100f, distanciaActual, velocidadDistancia;
        Vector3 pivote, velocidadPivote;
        bool colocada;
        InputAction accionMirar;
        RondaRuta ronda;
        readonly RaycastHit[] golpes = new RaycastHit[16];

        /// <summary>La cámara de este bicho (si es de un jugador de este ordenador con cámara propia).</summary>
        public static CamaraJugador De(ControladorBicho b)
        {
            if (b == null) return null;
            foreach (var c in Todas) if (c != null && c.bicho == b && c.isActiveAndEnabled) return c;
            return null;
        }

        /// <summary>La cámara del mando "local" de este ordenador (para el invitado).</summary>
        public static CamaraJugador DeLocal(int local)
        {
            foreach (var c in Todas) if (c != null && c.local == local && c.isActiveAndEnabled) return c;
            return null;
        }

        void Awake()
        {
            Camara = GetComponent<Camera>();
            pitch = inclinacion;
        }

        void OnEnable() => Todas.Add(this);
        void OnDisable() => Todas.Remove(this);

        /// <summary>Vuelve a colocarse detrás del bicho sin suavizar (al aparecer o reaparecer).</summary>
        public void Colocar()
        {
            colocada = false;
        }

        void LateUpdate()
        {
            var objetivo = Siguiendo != null ? Siguiendo : bicho;
            if (objetivo == null) return;
            if (Camara.fieldOfView != campoVision) Camara.fieldOfView = campoVision;

            Vector3 punto = objetivo.transform.position + Vector3.up * alturaMirada;
            if (!colocada || (punto - pivote).sqrMagnitude > 15f * 15f)
            {
                // Recién creada o el bicho ha reaparecido lejos: colocarse detrás, mirando a la carrera.
                pivote = punto;
                velocidadPivote = Vector3.zero;
                Vector3 guia = DireccionCarrera(objetivo.transform.position);
                if (guia.sqrMagnitude < 0.01f) guia = objetivo.transform.forward;
                yaw = Mathf.Atan2(guia.x, guia.z) * Mathf.Rad2Deg;
                pitch = inclinacion;
                distanciaActual = distancia;
                colocada = true;
            }
            else pivote = Vector3.SmoothDamp(pivote, punto, ref velocidadPivote, 0.06f);

            // A mano: stick derecho (grados por segundo) o ratón (movimiento de este fotograma).
            Vector2 mirar = LeerMirar(out bool raton);
            if (mirar.sqrMagnitude > 0.0004f)
            {
                float k = raton ? sensibilidadRaton : velocidadStick * Time.unscaledDeltaTime;
                yaw += mirar.x * k;
                pitch = Mathf.Clamp(pitch - mirar.y * k, inclinacionMinima, inclinacionMaxima);
                ultimoManual = Time.unscaledTime;
                velocidadYaw = velocidadPitch = 0f;
            }
            else if (Time.unscaledTime - ultimoManual > esperaAuto)
            {
                // Sola: mirando hacia donde sigue la carrera, con la inclinación de partida.
                Vector3 guia = DireccionCarrera(objetivo.transform.position);
                if (guia.sqrMagnitude > 0.01f)
                    yaw = Mathf.SmoothDampAngle(yaw, Mathf.Atan2(guia.x, guia.z) * Mathf.Rad2Deg, ref velocidadYaw, suavizadoAuto);
                pitch = Mathf.SmoothDamp(pitch, inclinacion, ref velocidadPitch, suavizadoAuto * 1.5f);
            }

            Quaternion giro = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 atras = giro * Vector3.back;
            float libre = DistanciaLibre(pivote, atras, distancia);
            // Si algo tapa, se acerca enseguida; al quedar libre, se aleja despacio.
            distanciaActual = libre < distanciaActual
                ? libre
                : Mathf.SmoothDamp(distanciaActual, libre, ref velocidadDistancia, 0.35f);

            Efectos.Sacudida(out Vector3 temblor, out float giroTemblor);
            transform.SetPositionAndRotation(pivote + atras * distanciaActual + giro * temblor, giro * Quaternion.Euler(0f, 0f, giroTemblor));
        }

        Vector2 LeerMirar(out bool raton)
        {
            raton = false;
            if (MandoRemoto.Pausado) return Vector2.zero;
            if (accionMirar == null && entrada != null && entrada.actions != null) accionMirar = entrada.actions.FindAction("Mirar");
            if (accionMirar == null) return Vector2.zero;
            raton = accionMirar.activeControl != null && accionMirar.activeControl.device is Mouse;
            return accionMirar.ReadValue<Vector2>();
        }

        /// <summary>Hacia dónde sigue la carrera desde aquí (según la ruta de los bots).</summary>
        Vector3 DireccionCarrera(Vector3 desde)
        {
            if (ronda == null) ronda = FindAnyObjectByType<RondaRuta>();
            if (ronda == null) return Vector3.zero;
            var p = ronda.PuntoRutaCercano(desde);
            if (p == null) return Vector3.zero;
            Vector3 hacia = Vector3.zero;
            if (p.siguientes != null && p.siguientes.Length > 0 && p.siguientes[0] != null)
                hacia = p.siguientes[0].transform.position - p.transform.position;
            Vector3 alPunto = p.transform.position - desde;
            alPunto.y = 0f;
            if (alPunto.magnitude > 3f) hacia = alPunto + hacia.normalized * 2f;
            hacia.y = 0f;
            return hacia.normalized;
        }

        /// <summary>Hasta dónde puede alejarse la cámara sin que algo sólido se meta delante.</summary>
        float DistanciaLibre(Vector3 desde, Vector3 direccion, float maxima)
        {
            int n = Physics.SphereCastNonAlloc(desde, 0.3f, direccion, golpes, maxima, ~0, QueryTriggerInteraction.Ignore);
            float libre = maxima;
            for (int i = 0; i < n; i++)
            {
                var g = golpes[i];
                if (g.distance <= 0f) continue;
                var cuerpo = g.collider.attachedRigidbody;
                if (cuerpo != null && (cuerpo.GetComponent<ControladorBicho>() != null || !cuerpo.isKinematic)) continue;
                libre = Mathf.Min(libre, g.distance);
            }
            return Mathf.Max(1.5f, libre - 0.2f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Limpiar() => Todas.Clear();
    }
}
