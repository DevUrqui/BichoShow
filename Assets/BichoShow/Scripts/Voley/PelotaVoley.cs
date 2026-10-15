using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// Pelota del Voley atados. Flota (gravedad reducida) para que dé tiempo a llegar.
    ///  - Al chocar con un bicho, sube con un "toque" asistido hacia el campo rival.
    ///  - Con el botón de lanzarse cerca, se golpea en parábola al campo rival (apuntando
    ///    con el stick); si el bicho está en el aire, es un remate más rápido y hacia abajo.
    ///  - Una sombra en el suelo indica dónde va a caer.
    /// Tipos especiales: Rayo (quien la toca cae), Aceite (deja charcos), Gigante.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class PelotaVoley : MonoBehaviour, IGolpeable
    {
        public enum Tipo { Normal, Rayo, Aceite, Gigante }

        [Header("Vuelo")]
        public float escalaGravedad = 0.45f;
        public float velocidadMaxima = 22f;

        [Header("Toque con el cuerpo")]
        [Tooltip("Velocidad hacia arriba mínima al tocarla con el cuerpo.")]
        public float subidaToque = 7f;
        [Tooltip("Velocidad hacia el campo rival al tocarla con el cuerpo.")]
        public float avanceToque = 1.8f;

        [Header("Golpe con el botón")]
        [Tooltip("Tiempo de vuelo de un golpe normal (s). Más = más bombeado.")]
        public float tiempoGolpe = 1.25f;
        [Tooltip("Tiempo de vuelo de un remate (s).")]
        public float tiempoRemate = 0.55f;

        public Tipo TipoPelota { get; private set; } = Tipo.Normal;
        /// <summary>Equipo que la tocó por última vez (-1 si nadie).</summary>
        public int UltimoEquipo { get; private set; } = -1;
        /// <summary>El punto de esta pelota ya se ha decidido.</summary>
        public bool PuntoResuelto { get; set; }
        public Rigidbody Cuerpo => rb;
        public float GravedadEfectiva => -Physics.gravity.y * escalaGravedad;

        RondaVoley ronda;
        Rigidbody rb;
        SphereCollider esfera;
        Transform sombra;
        Material material, materialSombra, materialCharco;
        Color colorBase;
        float ultimoToque = -10f;
        float ultimoCharco = -10f;
        readonly RaycastHit[] impactos = new RaycastHit[8];

        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");

        public static string NombreTipo(Tipo t) => t switch
        {
            Tipo.Rayo => "¡PELOTA RAYO!",
            Tipo.Aceite => "¡PELOTA DE ACEITE!",
            Tipo.Gigante => "¡PELOTA GIGANTE!",
            _ => "",
        };

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            esfera = GetComponent<SphereCollider>();
            rb.useGravity = false;
            rb.mass = 0.4f;
            rb.linearDamping = 0.15f;
            rb.angularDamping = 0.4f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            esfera.sharedMaterial = new PhysicsMaterial("Pelota")
            {
                bounciness = 0.6f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                dynamicFriction = 0.3f,
                staticFriction = 0.3f,
            };
            if (GetComponent<NoAgarrable>() == null) gameObject.AddComponent<NoAgarrable>();
        }

        public void Configurar(RondaVoley rondaVoley, Tipo tipo, Material mat, Material matSombra, Material matCharco)
        {
            ronda = rondaVoley;
            TipoPelota = tipo;
            material = mat;
            materialSombra = matSombra;
            materialCharco = matCharco;

            colorBase = tipo switch
            {
                Tipo.Rayo => new Color(1f, 0.9f, 0.2f),
                Tipo.Aceite => new Color(0.18f, 0.14f, 0.1f),
                Tipo.Gigante => new Color(1f, 0.55f, 0.2f),
                _ => new Color(0.98f, 0.97f, 0.9f),
            };
            if (material != null)
            {
                GetComponent<Renderer>().sharedMaterial = material;
                Escribir(material, colorBase);
            }

            if (tipo == Tipo.Gigante)
            {
                transform.localScale *= 2f;
                escalaGravedad *= 0.7f;
            }

            CrearSombra();
        }

        void OnDestroy()
        {
            if (sombra != null) Destroy(sombra.gameObject);
            if (material != null) Destroy(material);
        }

        // ---------------------------------------------------------------------
        // Física
        // ---------------------------------------------------------------------

        void FixedUpdate()
        {
            rb.AddForce(Physics.gravity * escalaGravedad, ForceMode.Acceleration);
            if (rb.linearVelocity.sqrMagnitude > velocidadMaxima * velocidadMaxima)
                rb.linearVelocity = rb.linearVelocity.normalized * velocidadMaxima;

            if (transform.position.y < -6f && ronda != null) ronda.PelotaPerdida(this);
        }

        void OnCollisionEnter(Collision choque)
        {
            var bicho = choque.rigidbody != null ? choque.rigidbody.GetComponent<ControladorBicho>() : null;
            if (bicho != null)
            {
                Toque(bicho);
                return;
            }

            var zona = choque.collider.GetComponent<ZonaPista>();
            if (zona != null)
            {
                if (TipoPelota == Tipo.Aceite) DejarCharco(choque.GetContact(0).point);
                if (ronda != null) ronda.PelotaEnSuelo(this, zona);
            }
        }

        /// <summary>Toque con el cuerpo: la pelota sube y avanza un poco hacia el campo rival.</summary>
        void Toque(ControladorBicho bicho)
        {
            if (Time.time - ultimoToque < 0.12f) return;
            if (ronda == null) return;

            int equipo = RondaBase.EquipoDe(bicho);
            Vector3 haciaRival = ronda.HaciaRival(equipo);
            Vector3 horizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z) * 0.3f + haciaRival * avanceToque;
            rb.linearVelocity = horizontal + Vector3.up * Mathf.Max(rb.linearVelocity.y, subidaToque);

            RegistrarToque(bicho, equipo);
        }

        public bool Golpear(ControladorBicho quien, Vector3 direccion, bool enElAire)
        {
            if (ronda == null || PuntoResuelto) return false;

            int equipo = RondaBase.EquipoDe(quien);
            Vector3 objetivo = ronda.PuntoDeGolpe(equipo, direccion, enElAire);
            float t = enElAire ? tiempoRemate : tiempoGolpe;

            // Parábola: llegar a "objetivo" en t segundos con la gravedad de la pelota.
            Vector3 desde = rb.position;
            float g = GravedadEfectiva;
            Vector3 v = (objetivo - desde) / t;
            v.y += 0.5f * g * t;
            rb.linearVelocity = v;
            rb.angularVelocity = Random.insideUnitSphere * 8f;

            RegistrarToque(quien, equipo);
            return true;
        }

        void RegistrarToque(ControladorBicho bicho, int equipo)
        {
            ultimoToque = Time.time;
            UltimoEquipo = equipo;
            if (TipoPelota == Tipo.Rayo) bicho.Tropezar(0.7f);
        }

        // ---------------------------------------------------------------------
        // Aceite
        // ---------------------------------------------------------------------

        void DejarCharco(Vector3 punto)
        {
            if (Time.time - ultimoCharco < 0.3f) return;
            ultimoCharco = Time.time;

            var charco = new GameObject("Charco");
            charco.transform.position = new Vector3(punto.x, 0f, punto.z);
            var caja = charco.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = new Vector3(2.4f, 0.8f, 2.4f);
            caja.center = new Vector3(0f, 0.4f, 0f);
            charco.AddComponent<Charco>();

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Mancha";
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(charco.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            visual.transform.localScale = new Vector3(Random.Range(2f, 2.6f), 0.01f, Random.Range(2f, 2.6f));
            if (materialCharco != null) visual.GetComponent<Renderer>().sharedMaterial = materialCharco;

            Destroy(charco, 9f);
        }

        // ---------------------------------------------------------------------
        // Aspecto: sombra y parpadeo del rayo
        // ---------------------------------------------------------------------

        void CrearSombra()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "SombraPelota";
            Destroy(go.GetComponent<Collider>());
            if (materialSombra != null) go.GetComponent<Renderer>().sharedMaterial = materialSombra;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sombra = go.transform;
        }

        void LateUpdate()
        {
            if (TipoPelota == Tipo.Rayo && material != null)
                Escribir(material, Mathf.Repeat(Time.time * 10f, 1f) < 0.5f ? colorBase : Color.white);

            if (sombra == null) return;

            // Rayo hacia abajo saltándose la propia pelota y los bichos.
            float radio = esfera.radius * transform.lossyScale.x;
            int n = Physics.RaycastNonAlloc(transform.position, Vector3.down, impactos, 40f, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            Vector3 punto = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                var c = impactos[i].collider;
                if (c.attachedRigidbody == rb) continue;
                if (c.attachedRigidbody != null && c.attachedRigidbody.GetComponent<ControladorBicho>() != null) continue;
                if (impactos[i].distance < mejor) { mejor = impactos[i].distance; punto = impactos[i].point; }
            }

            bool hay = mejor < float.MaxValue;
            sombra.gameObject.SetActive(hay);
            if (!hay) return;

            float altura = mejor;
            float tam = radio * 2f * Mathf.Lerp(1.3f, 0.6f, Mathf.Clamp01(altura / 8f));
            sombra.position = punto + Vector3.up * 0.02f;
            sombra.rotation = Quaternion.identity;
            sombra.localScale = new Vector3(tam, 0.005f, tam);
        }

        static void Escribir(Material m, Color c)
        {
            if (m.HasProperty(IdBaseColor)) m.SetColor(IdBaseColor, c);
            else m.color = c;
        }
    }
}
