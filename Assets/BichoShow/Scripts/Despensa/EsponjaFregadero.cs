using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// "Despensa Deslizante", zona 2: esponja gigante flotando en el fregadero. Se hunde un poco
    /// por cada bicho que tenga encima. Y se empapa: con muchos encima, o si alguien se queda
    /// quieto en ella demasiado rato, tiembla, suelta espuma y se hunde bajo el agua jabonosa un
    /// par de segundos (quien siga encima, al agua). Por el centro se va rápido, pero sin parar.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EsponjaFregadero : MonoBehaviour, IEsperaBot
    {
        [Tooltip("Tamaño (ancho, fondo) de la cara de arriba, para ver quién está encima.")]
        public Vector2 tamanoArriba = new Vector2(6f, 11f);
        public float alturaArriba = 0.4f;
        [Tooltip("Lo que se hunde por cada bicho encima (m).")]
        public float hundimientoPorBicho = 0.18f;
        [Tooltip("Con estos bichos encima a la vez, se empapa.")]
        public int limite = 3;
        [Tooltip("Ocupada sin parar este rato, también se empapa (s).")]
        public float tiempoEmpapar = 3.5f;
        [Tooltip("Lo que baja al empaparse (por debajo del agua).")]
        public float hundimientoEmpapada = 1.6f;
        public float duracionEmpapada = 2f;
        public float aviso = 0.7f;
        public Transform visual;
        public ParticleSystem espuma;

        Rigidbody rb;
        Vector3 inicio, escalaVisual = Vector3.one, posicionVisual;
        float bajado, ocupadaDesde = -1f, empapadaDesde = -100f, avisoDesde = -1f;
        int encimaAhora;
        readonly Collider[] encima = new Collider[24];
        readonly System.Collections.Generic.HashSet<ControladorBicho> contados = new System.Collections.Generic.HashSet<ControladorBicho>();

        public bool Empapada => Time.time - empapadaDesde < duracionEmpapada;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            inicio = transform.position;
            if (visual != null) { escalaVisual = visual.localScale; posicionVisual = visual.localPosition; }
        }

        void FixedUpdate()
        {
            encimaAhora = Contar();
            float t = Time.time;
            if (encimaAhora > 0 && ocupadaDesde < 0f) ocupadaDesde = t;
            if (encimaAhora == 0) ocupadaDesde = -1f;

            bool debeEmpapar = !Empapada && (encimaAhora >= limite || (ocupadaDesde >= 0f && t - ocupadaDesde > tiempoEmpapar));
            if (debeEmpapar && avisoDesde < 0f)
            {
                avisoDesde = t;
                Sonidos.Reproducir(Sonido.Ola, transform.position, 0.5f, 1.4f);
            }
            if (avisoDesde >= 0f && t - avisoDesde >= aviso)
            {
                avisoDesde = -1f;
                empapadaDesde = t;
                ocupadaDesde = -1f;
                if (espuma != null) espuma.Play();
                Sonidos.Reproducir(Sonido.Ola, transform.position, 0.9f, 0.8f);
            }

            float objetivo = Empapada ? hundimientoEmpapada : encimaAhora * hundimientoPorBicho;
            float velocidad = Empapada ? 2.5f : objetivo > bajado ? 1.5f : 0.6f;
            bajado = Mathf.MoveTowards(bajado, objetivo, velocidad * Time.fixedDeltaTime);
            rb.MovePosition(inicio - Vector3.up * bajado);
        }

        void Update()
        {
            if (visual == null) return;
            // Se espachurra con el peso y tiembla antes de empaparse.
            float aplastada = Mathf.Clamp01(bajado / Mathf.Max(0.01f, hundimientoEmpapada));
            visual.localScale = new Vector3(escalaVisual.x * (1f + 0.06f * aplastada), escalaVisual.y * (1f - 0.25f * aplastada), escalaVisual.z * (1f + 0.06f * aplastada));
            float temblor = avisoDesde >= 0f ? 0.08f : 0f;
            visual.localPosition = posicionVisual + new Vector3(Mathf.Sin(Time.time * 60f), 0f, Mathf.Cos(Time.time * 55f)) * temblor;
        }

        int Contar()
        {
            Vector3 centro = transform.position + Vector3.up * (alturaArriba + 0.6f);
            var medio = new Vector3(tamanoArriba.x * 0.5f, 0.7f, tamanoArriba.y * 0.5f);
            int n = Physics.OverlapBoxNonAlloc(centro, medio, encima, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            contados.Clear();
            for (int i = 0; i < n; i++)
            {
                var cuerpo = encima[i].attachedRigidbody;
                if (cuerpo != null && cuerpo != rb && cuerpo.TryGetComponent(out ControladorBicho b) && !b.Retirado) contados.Add(b);
            }
            return contados.Count;
        }

        /// <summary>Para los bots: subirse si no está empapada ni a punto.</summary>
        public bool PuedoPasar(ControladorBicho bicho) => !Empapada && avisoDesde < 0f && bajado < 0.3f && encimaAhora < limite - 1;
    }
}
