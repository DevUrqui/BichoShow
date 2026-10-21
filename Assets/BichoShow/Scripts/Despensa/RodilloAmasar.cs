using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Despensa Deslizante", zona 1: rodillo de amasar gigante. Rueda por el mantel hacia la
    /// salida (contra la carrera), se para y vuelve. El cuerpo es gordo (hay que saltarlo justo)
    /// y los mangos de los lados son finos y bajos (se saltan fácil, pero ahí están los saleros).
    /// A quien pilla lo aplasta y lo echa hacia atrás.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RodilloAmasar : MonoBehaviour, IEsperaBot
    {
        [Tooltip("Z entre las que va y viene (el pivote está en el eje del rodillo).")]
        public float zDesde = 20f;
        public float zHasta = 40f;
        [Tooltip("Velocidad rodando hacia la salida (m/s).")]
        public float velocidadIda = 4.5f;
        [Tooltip("Velocidad volviendo (m/s).")]
        public float velocidadVuelta = 3f;
        public float pausa = 0.9f;
        [Tooltip("Desfase del ciclo (s), para que dos rodillos no vayan a la par.")]
        public float desfase;
        [Tooltip("Radio del cuerpo (para que ruede sin deslizar).")]
        public float radio = 0.9f;
        public float empujon = 8f;
        public float tumbado = 1.2f;
        [Tooltip("Lo que se ve (rueda). El objeto en sí solo se desplaza: así los puntos de ruta que cuelgan de él no giran.")]
        public Transform visual;

        Rigidbody rb;
        float y, x;

        /// <summary>Hacia dónde va ahora (-1 hacia la salida, +1 de vuelta, 0 parado).</summary>
        public float Sentido { get; private set; }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            y = transform.position.y;
            x = transform.position.x;
        }

        float Duracion => (zHasta - zDesde) / velocidadIda + (zHasta - zDesde) / velocidadVuelta + 2f * pausa;

        /// <summary>Z del rodillo en el momento t (sale de zHasta hacia zDesde y vuelve).</summary>
        public float ZEn(float t, out float sentido)
        {
            float largo = zHasta - zDesde;
            float ida = largo / velocidadIda, vuelta = largo / velocidadVuelta;
            float c = Mathf.Repeat(t + desfase, Duracion);
            if (c < ida) { sentido = -1f; return zHasta - c * velocidadIda; }
            c -= ida;
            if (c < pausa) { sentido = 0f; return zDesde; }
            c -= pausa;
            if (c < vuelta) { sentido = 1f; return zDesde + c * velocidadVuelta; }
            sentido = 0f;
            return zHasta;
        }

        void FixedUpdate()
        {
            float z = ZEn(Time.time, out float sentido);
            Sentido = sentido;
            float antes = rb.position.z;
            rb.MovePosition(new Vector3(x, y, z));
            avanceVisual += z - antes;
        }

        float avanceVisual;

        void Update()
        {
            // Rueda sin deslizar: el modelo gira sobre su eje (X) lo que avanza entre el radio.
            if (visual != null) visual.localRotation = Quaternion.Euler(avanceVisual / radio * Mathf.Rad2Deg, 0f, 0f);
        }

        void OnCollisionEnter(Collision choque) => Aplastar(choque);

        void Aplastar(Collision choque)
        {
            var cuerpo = choque.rigidbody;
            if (cuerpo == null || !cuerpo.TryGetComponent(out ControladorBicho bicho) || bicho.Retirado) return;
            // Si está encima (lo ha saltado mal y ha caído sobre él), no lo empuja: que siga.
            if (cuerpo.position.y > y + radio * 0.8f) return;
            float hacia = Sentido != 0f ? Sentido : Mathf.Sign(cuerpo.position.z - rb.position.z);
            cuerpo.linearVelocity = new Vector3(cuerpo.linearVelocity.x * 0.5f, 3f, hacia * empujon);
            bicho.Tropezar(tumbado);
            Sonidos.Reproducir(Sonido.Golpe, cuerpo.position, bicho.EsBot ? 0.6f : 1f, 0.8f);
            Efectos.Polvo(cuerpo.position, 0.8f);
            if (!bicho.EsBot) Efectos.Sacudir(0.2f);
        }

        /// <summary>Para los bots: pasar cuando el rodillo esté lejos o yéndose.</summary>
        public bool PuedoPasar(ControladorBicho bicho)
        {
            float yo = bicho.transform.position.z;
            float z = ZEn(Time.time, out float sentido);
            float luego = ZEn(Time.time + 2f, out _);
            return (sentido > 0f && z > yo + 2f) || Mathf.Min(Mathf.Abs(z - yo), Mathf.Abs(luego - yo)) > 9f;
        }
    }
}
