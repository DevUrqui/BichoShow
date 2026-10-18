using UnityEngine;

namespace BichoShow
{
    /// <summary>
    /// La "gracia" de cada bicho (lo añade ControladorBicho solo):
    ///  - Se estira al saltar y se aplasta al aterrizar, con un rebote de gelatina.
    ///  - Polvo al aterrizar fuerte y al reaparecer.
    ///  - Tras un golpe que lo tira: estallido de estrellas, sacudida de cámara, estrellitas
    ///    dando vueltas sobre la cabeza y, si el golpe es muy gordo, un instante a cámara lenta.
    ///  - Sonidos de pasos, saltos, placajes, agarres, lanzamientos, golpes y caídas.
    /// </summary>
    [DisallowMultipleComponent]
    public class JugoBicho : MonoBehaviour
    {
        [Header("Estirar y aplastar")]
        public float rigidez = 170f;
        public float amortiguacion = 11f;

        [Header("Golpes")]
        [Tooltip("Golpe (cambio de velocidad, m/s) a partir del cual hay cámara lenta.")]
        public float golpeCamaraLenta = 11f;

        ControladorBicho bicho;
        Rigidbody rb;
        Transform raiz;
        Vector3 escalaRaiz = Vector3.one;
        float deformacion, velocidadDeformacion;

        bool estabaEnSuelo = true;
        float velocidadYAntes;
        float distanciaPaso;

        Transform estrellas;
        float finEstrellas = -10f;
        static Mesh mallaEstrella;
        static Material materialEstrella;

        void Awake()
        {
            bicho = GetComponent<ControladorBicho>();
            rb = GetComponent<Rigidbody>();
            var aspecto = GetComponent<AspectoBicho>();
            raiz = aspecto != null && aspecto.raizVisual != null ? aspecto.raizVisual : transform.Find("Aspecto");
            if (raiz != null) escalaRaiz = raiz.localScale;
        }

        void OnEnable()
        {
            if (bicho == null) return;
            bicho.AlHacer += AlHacer;
            bicho.AlRecibirGolpe += AlRecibirGolpe;
            bicho.AlCaerseDelMapa += AlCaerse;
            bicho.AlReaparecer += AlReaparecer;
        }

        void OnDisable()
        {
            if (bicho == null) return;
            bicho.AlHacer -= AlHacer;
            bicho.AlRecibirGolpe -= AlRecibirGolpe;
            bicho.AlCaerseDelMapa -= AlCaerse;
            bicho.AlReaparecer -= AlReaparecer;
        }

        float Volumen => bicho != null && bicho.EsBot ? 0.55f : 1f;

        // ------------------------------------------------------------------
        // Aterrizajes y pasos
        // ------------------------------------------------------------------

        void FixedUpdate()
        {
            if (bicho == null || rb == null || bicho.Retirado || rb.isKinematic) return;

            bool enSuelo = bicho.EnSuelo;
            if (enSuelo && !estabaEnSuelo && velocidadYAntes < -3.5f) Aterrizar(-velocidadYAntes);
            estabaEnSuelo = enSuelo;
            velocidadYAntes = rb.linearVelocity.y;

            // Pasos
            if (enSuelo && !bicho.Tumbado)
            {
                Vector3 v = rb.linearVelocity;
                distanciaPaso += new Vector2(v.x, v.z).magnitude * Time.fixedDeltaTime;
                if (distanciaPaso > 0.9f)
                {
                    distanciaPaso = 0f;
                    Sonidos.Reproducir(Sonido.Paso, transform.position, Volumen * 0.8f, 1f, 0.15f);
                }
            }
        }

        void Aterrizar(float velocidad)
        {
            float k = Mathf.Clamp01((velocidad - 3.5f) / 9f);
            Sonidos.Reproducir(Sonido.Aterrizaje, transform.position, Volumen * (0.4f + 0.6f * k), 1.1f - 0.3f * k);
            Efectos.Polvo(transform.position, k);
            velocidadDeformacion -= 2.5f + 6f * k;
            if (k > 0.7f && !bicho.EsBot) Efectos.Sacudir(0.15f);
        }

        // ------------------------------------------------------------------
        // Eventos del bicho
        // ------------------------------------------------------------------

        void AlHacer(ControladorBicho _, ControladorBicho.AccionBicho accion)
        {
            Estadisticas.Evento(accion.ToString().ToLowerInvariant(), bicho);
            switch (accion)
            {
                case ControladorBicho.AccionBicho.Saltar:
                    Sonidos.Reproducir(Sonido.Salto, transform.position, Volumen);
                    velocidadDeformacion += 5f;
                    break;
                case ControladorBicho.AccionBicho.Placaje:
                    Sonidos.Reproducir(Sonido.Placaje, transform.position, Volumen);
                    velocidadDeformacion += 3f;
                    break;
                case ControladorBicho.AccionBicho.Agarrar:
                    Sonidos.Reproducir(Sonido.Agarrar, transform.position, Volumen);
                    break;
                case ControladorBicho.AccionBicho.Lanzar:
                    Sonidos.Reproducir(Sonido.Lanzar, transform.position, Volumen);
                    velocidadDeformacion -= 2f;
                    break;
            }
        }

        void AlRecibirGolpe(ControladorBicho _, float fuerza, Vector3 punto)
        {
            float k = Mathf.Clamp01((fuerza - bicho.golpeParaTropezar) / 8f);
            Estadisticas.Evento("derribado", bicho, $"golpe de {fuerza:0} m/s");
            Sonidos.Reproducir(Sonido.Golpe, punto, 0.6f + 0.4f * k);
            Sonidos.Reproducir(Sonido.Derribo, punto, 0.7f + 0.3f * k, Random.Range(0.9f, 1.15f));
            Sonidos.Reproducir(Sonido.Ooh, punto, 0.5f + 0.5f * k);
            Efectos.Impacto(punto, bicho.ColorJugador, k);
            // Los golpes entre bots también se ven, pero sin tanto temblor ni cámara lenta.
            Efectos.Sacudir((0.12f + 0.3f * k) * (bicho.EsBot ? 0.5f : 1f));
            if (fuerza >= golpeCamaraLenta && !bicho.EsBot) Efectos.CamaraLenta();

            velocidadDeformacion -= 4f;
            finEstrellas = Time.time + bicho.duracionTropiezo + 0.6f;
            Sonidos.Reproducir(Sonido.Estrellitas, punto, 0.8f);
        }

        void AlCaerse(ControladorBicho _)
        {
            Sonidos.Reproducir(Sonido.Caida, transform.position, Volumen);
            Estadisticas.Evento("caida", bicho, "fuera del mapa");
        }

        void AlReaparecer(ControladorBicho _)
        {
            finEstrellas = -10f;
            estabaEnSuelo = true;
            velocidadYAntes = 0f;
            if (bicho.Retirado) return;
            Sonidos.Reproducir(Sonido.Aparecer, transform.position, Volumen * 0.8f);
            Efectos.Polvo(transform.position, 0.3f);
            velocidadDeformacion += 4f;
        }

        // ------------------------------------------------------------------
        // Gelatina y estrellitas
        // ------------------------------------------------------------------

        void LateUpdate()
        {
            if (bicho == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            if (raiz != null)
            {
                // En el aire se estira un poco al subir y se encoge al caer.
                float objetivo = 0f;
                if (rb != null && !bicho.EnSuelo && !bicho.Tumbado)
                    objetivo = Mathf.Clamp(rb.linearVelocity.y * 0.022f, -0.1f, 0.14f);
                velocidadDeformacion += (rigidez * (objetivo - deformacion) - amortiguacion * velocidadDeformacion) * dt;
                deformacion = Mathf.Clamp(deformacion + velocidadDeformacion * dt, -0.4f, 0.4f);
                raiz.localScale = Vector3.Scale(escalaRaiz, new Vector3(1f - deformacion * 0.5f, 1f + deformacion, 1f - deformacion * 0.5f));
            }

            ActualizarEstrellas();
        }

        void ActualizarEstrellas()
        {
            bool visibles = Time.time < finEstrellas && bicho.Tumbado && !bicho.Retirado;
            if (!visibles)
            {
                if (estrellas != null && estrellas.gameObject.activeSelf) estrellas.gameObject.SetActive(false);
                return;
            }
            if (estrellas == null) CrearEstrellas();
            if (!estrellas.gameObject.activeSelf) estrellas.gameObject.SetActive(true);

            Vector3 centro = (rb != null ? rb.worldCenterOfMass : transform.position) + Vector3.up * 0.95f;
            estrellas.SetPositionAndRotation(centro, Quaternion.Euler(0f, Time.time * 280f, 0f));
            for (int i = 0; i < estrellas.childCount; i++)
            {
                var e = estrellas.GetChild(i);
                float a = i * Mathf.PI * 2f / estrellas.childCount;
                e.localPosition = new Vector3(Mathf.Cos(a) * 0.4f, Mathf.Sin(Time.time * 6f + i * 2f) * 0.06f, Mathf.Sin(a) * 0.4f);
                e.localRotation = Quaternion.Euler(0f, Time.time * -500f + i * 40f, 0f);
            }
        }

        void CrearEstrellas()
        {
            if (mallaEstrella == null) mallaEstrella = CrearMallaEstrella(0.13f, 0.055f, 0.035f);
            if (materialEstrella == null)
            {
                materialEstrella = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "EstrellaKO" };
                var amarillo = new Color(1f, 0.85f, 0.2f);
                materialEstrella.SetColor("_BaseColor", amarillo);
                materialEstrella.EnableKeyword("_EMISSION");
                materialEstrella.SetColor("_EmissionColor", amarillo * 1.5f);
                materialEstrella.SetFloat("_Cull", 0f); // se ve por las dos caras
            }

            estrellas = new GameObject("EstrellasKO").transform;
            estrellas.SetParent(transform, false); // viaja con el bicho entre escenas
            for (int i = 0; i < 3; i++)
            {
                var e = new GameObject($"Estrella{i}");
                e.transform.SetParent(estrellas, false);
                e.AddComponent<MeshFilter>().sharedMesh = mallaEstrella;
                var r = e.AddComponent<MeshRenderer>();
                r.sharedMaterial = materialEstrella;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>Estrella de cinco puntas con algo de grosor, de pie (en el plano XY).</summary>
        static Mesh CrearMallaEstrella(float radio, float radioInterior, float grosor)
        {
            const int puntas = 5;
            int n = puntas * 2;
            var v = new Vector3[(n + 1) * 2];
            for (int lado = 0; lado < 2; lado++)
            {
                float z = lado == 0 ? -grosor / 2f : grosor / 2f;
                int b = lado * (n + 1);
                v[b] = new Vector3(0f, 0f, z);
                for (int i = 0; i < n; i++)
                {
                    float a = Mathf.PI / 2f + i * Mathf.PI / puntas;
                    float r = i % 2 == 0 ? radio : radioInterior;
                    v[b + 1 + i] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, z);
                }
            }
            var t = new System.Collections.Generic.List<int>();
            for (int i = 0; i < n; i++)
            {
                int a = 1 + i, c = 1 + (i + 1) % n;
                t.AddRange(new[] { 0, a, c });                                   // cara de delante (mira a -Z)
                t.AddRange(new[] { n + 1, n + 1 + c, n + 1 + a });               // cara de detrás (mira a +Z)
                t.AddRange(new[] { a, c, n + 1 + c, a, n + 1 + c, n + 1 + a });  // canto
            }
            var malla = new Mesh { name = "EstrellaKO", vertices = v, triangles = t.ToArray() };
            malla.RecalculateNormals();
            malla.RecalculateBounds();
            return malla;
        }
    }
}
