using UnityEngine;
using UnityEngine.Rendering;

namespace BichoShow
{
    /// <summary>
    /// Efectos de "gracia" que se llaman desde cualquier sitio:
    ///  - Efectos.Polvo(punto): nubecitas de polvo que giran (aterrizajes, porrazos).
    ///  - Efectos.Impacto(punto, color): destello, onda expansiva, chispas y estrellitas (golpes, roturas).
    ///  - Efectos.CamaraLenta(): un instante a cámara lenta para los momentazos.
    ///  - Efectos.Sacudir(fuerza): tiembla la cámara (lo aplica CamaraGrupo).
    /// Las partículas se crean por código; sus materiales salen de Resources (los crea el
    /// editor solo, ver Editor/RecursosEfectos.cs) con las texturas hechas en Blender
    /// (Arte/Blender/crear_texturas_particulas.py), para que también funcionen en el juego compilado.
    /// </summary>
    public static class Efectos
    {
        public const string RecursoPolvo = "EfectosPolvo";
        public const string RecursoEstrella = "EfectosEstrella";
        public const string RecursoNube = "EfectosNube", RecursoDestello = "EfectosDestello", RecursoOnda = "EfectosOnda", RecursoChispa = "EfectosChispa";

        static Anfitrion anfitrion;
        static float trauma;
        static int fotogramaTrauma = -1;
        static float finCamaraLenta = -100f, siguienteCamaraLenta = -100f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void LimpiarEstaticos()
        {
            anfitrion = null;
            trauma = 0f;
            fotogramaTrauma = -1;
            finCamaraLenta = siguienteCamaraLenta = -100f;
            Time.timeScale = 1f;
        }

        // ------------------------------------------------------------------
        // Partículas
        // ------------------------------------------------------------------

        public static void Polvo(Vector3 punto, float cantidad = 1f)
        {
            if (!RedShow.EfectoLocal(RedShow.EfectoPolvo, punto, Color.clear, cantidad, 0f)) return;
            var ps = Anfitrion.Obtener().polvo;
            int n = Mathf.RoundToInt(Mathf.Lerp(4f, 14f, Mathf.Clamp01(cantidad)));
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < n; i++)
            {
                Vector2 c = Random.insideUnitCircle.normalized;
                p.position = punto + new Vector3(c.x, 0.05f, c.y) * 0.25f;
                p.velocity = new Vector3(c.x, 0f, c.y) * Random.Range(1.2f, 2.6f) * (0.6f + cantidad * 0.6f) + Vector3.up * Random.Range(0.3f, 1.2f);
                p.startSize = Random.Range(0.35f, 0.7f) * (0.7f + cantidad * 0.5f);
                p.startLifetime = Random.Range(0.45f, 0.8f);
                p.startColor = new Color(0.93f, 0.88f, 0.8f, 0.9f);
                p.rotation = Random.Range(0f, 360f);
                p.angularVelocity = Random.Range(-120f, 120f);
                ps.Emit(p, 1);
            }
        }

        public static void Impacto(Vector3 punto, Color color, float cantidad = 1f)
        {
            if (!RedShow.EfectoLocal(RedShow.EfectoImpacto, punto, color, cantidad, 0f)) return;
            var a = Anfitrion.Obtener();
            float k = Mathf.Clamp01(cantidad);
            var p = new ParticleSystem.EmitParams();

            // Destello y onda: un instante, en el sitio del golpe.
            if (a.destello != null)
            {
                p.position = punto;
                p.velocity = Vector3.zero;
                p.startSize = Mathf.Lerp(1.2f, 2.4f, k);
                p.startLifetime = 0.16f;
                p.rotation = Random.Range(0f, 45f);
                p.angularVelocity = 0f;
                p.startColor = Color.Lerp(Color.white, color, 0.3f);
                a.destello.Emit(p, 1);
            }
            if (a.onda != null)
            {
                p.startSize = Mathf.Lerp(2.2f, 4f, k);
                p.startLifetime = 0.3f;
                p.rotation = 0f;
                p.startColor = new Color(color.r, color.g, color.b, 0.9f);
                a.onda.Emit(p, 1);
            }
            if (a.chispas != null)
            {
                int c = Mathf.RoundToInt(Mathf.Lerp(5f, 12f, k));
                for (int i = 0; i < c; i++)
                {
                    Vector3 dir = Random.onUnitSphere;
                    dir.y = Mathf.Abs(dir.y) * 0.9f + 0.1f;
                    p.position = punto;
                    p.velocity = dir.normalized * Random.Range(6f, 11f) * (0.7f + k * 0.4f);
                    p.startSize = Random.Range(0.12f, 0.22f);
                    p.startLifetime = Random.Range(0.18f, 0.32f);
                    p.startColor = i % 2 == 0 ? new Color(1f, 0.92f, 0.6f) : color;
                    a.chispas.Emit(p, 1);
                }
            }

            var ps = a.estrellas;
            int n = Mathf.RoundToInt(Mathf.Lerp(4f, 11f, k));
            for (int i = 0; i < n; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f;
                p.position = punto;
                p.velocity = dir.normalized * Random.Range(3f, 7f) * (0.7f + cantidad * 0.4f);
                p.startSize = Random.Range(0.22f, 0.45f);
                p.startLifetime = Random.Range(0.3f, 0.55f);
                p.rotation = Random.Range(0f, 360f);
                p.angularVelocity = Random.Range(-500f, 500f);
                p.startColor = i % 3 == 0 ? Color.white : (i % 3 == 1 ? new Color(1f, 0.9f, 0.35f) : color);
                ps.Emit(p, 1);
            }
        }

        // ------------------------------------------------------------------
        // Cámara lenta y sacudida
        // ------------------------------------------------------------------

        /// <summary>Un instante a cámara lenta (como mucho uno cada pocos segundos, para que no canse).</summary>
        public static void CamaraLenta(float escala = 0.3f, float duracion = 0.45f, float espera = 4f)
        {
            float ahora = Time.unscaledTime;
            if (ahora < siguienteCamaraLenta) return;
            if (!RedShow.EfectoLocal(RedShow.EfectoCamaraLenta, Vector3.zero, Color.clear, escala, duracion)) return;
            siguienteCamaraLenta = ahora + duracion + espera;
            finCamaraLenta = ahora + duracion;
            Time.timeScale = escala;
            Anfitrion.Obtener(); // el anfitrión devuelve el tiempo a su ritmo
        }

        /// <summary>Hace temblar la cámara. fuerza 0.2 = toque, 0.5 = golpe fuerte, 1 = terremoto.</summary>
        public static void Sacudir(float fuerza) => trauma = Mathf.Clamp01(Mathf.Max(trauma, 0f) + fuerza);

        /// <summary>Desplazamiento y giro de la sacudida en este fotograma (lo usa CamaraGrupo).</summary>
        public static void Sacudida(out Vector3 desplazamiento, out float giro)
        {
            if (fotogramaTrauma != Time.frameCount)
            {
                fotogramaTrauma = Time.frameCount;
                trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * 1.6f);
            }
            float k = trauma * trauma;
            float t = Time.unscaledTime * 22f;
            desplazamiento = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * 1.1f * k;
            giro = (Mathf.PerlinNoise(t, 5.1f) - 0.5f) * 6f * k;
        }

        // ------------------------------------------------------------------
        // Materiales (también los usa el editor para guardarlos en Resources)
        // ------------------------------------------------------------------

        public static Material CrearMaterialParticulas(Texture2D textura) => CrearMaterialParticulas(textura, false);

        /// <summary>Material de partículas sin luces, transparente; si es aditivo, la luz se suma (brilla).</summary>
        public static Material CrearMaterialParticulas(Texture2D textura, bool aditivo)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            var m = new Material(shader);
            m.SetTexture("_BaseMap", textura);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);   // transparente
            m.SetFloat("_Blend", aditivo ? 2f : 0f);     // aditivo o alfa
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(aditivo ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.mainTexture = textura;
            return m;
        }

        /// <summary>Círculo blanco de bordes suaves.</summary>
        public static Texture2D CrearTexturaCirculo(int tamano = 64)
        {
            var tex = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false) { name = "Circulo", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < tamano; y++)
            for (int x = 0; x < tamano; x++)
            {
                float dx = (x + 0.5f) / tamano * 2f - 1f, dy = (y + 0.5f) / tamano * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((1f - r) * 2.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
            tex.Apply();
            return tex;
        }

        /// <summary>Estrella blanca de cinco puntas.</summary>
        public static Texture2D CrearTexturaEstrella(int tamano = 64)
        {
            var tex = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false) { name = "Estrella", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < tamano; y++)
            for (int x = 0; x < tamano; x++)
            {
                float dx = (x + 0.5f) / tamano * 2f - 1f, dy = (y + 0.5f) / tamano * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx) + Mathf.PI / 2f;
                // Radio de la estrella en este ángulo: entre 0.42 (huecos) y 0.95 (puntas).
                float sector = Mathf.Abs(Mathf.Repeat(ang / (2f * Mathf.PI / 5f), 1f) - 0.5f) * 2f; // 1 en la punta, 0 en el hueco
                float radio = Mathf.Lerp(0.42f, 0.95f, Mathf.Pow(sector, 1.6f));
                float a = Mathf.Clamp01((radio - r) * 18f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------
        // Anfitrión: guarda los sistemas de partículas y controla la cámara lenta
        // ------------------------------------------------------------------

        class Anfitrion : MonoBehaviour
        {
            public ParticleSystem polvo, estrellas, destello, onda, chispas;

            public static Anfitrion Obtener()
            {
                if (anfitrion != null) return anfitrion;
                var go = new GameObject("Bicho Show: efectos");
                DontDestroyOnLoad(go);
                anfitrion = go.AddComponent<Anfitrion>();

                // Las nubecitas (hoja 2x2 de Blender); si no están, el círculo de siempre.
                var matNube = Resources.Load<Material>(RecursoNube);
                var matPolvo = matNube ?? Resources.Load<Material>(RecursoPolvo) ?? CrearMaterialParticulas(CrearTexturaCirculo());
                var matEstrella = Resources.Load<Material>(RecursoEstrella) ?? CrearMaterialParticulas(CrearTexturaEstrella());
                anfitrion.polvo = CrearSistema(go.transform, "Polvo", matPolvo, gravedad: -0.15f, crecer: true);
                if (matNube != null)
                {
                    var hojas = anfitrion.polvo.textureSheetAnimation;
                    hojas.enabled = true;
                    hojas.numTilesX = 2;
                    hojas.numTilesY = 2;
                    hojas.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
                    hojas.startFrame = new ParticleSystem.MinMaxCurve(0f, 3.999f);
                }
                anfitrion.estrellas = CrearSistema(go.transform, "Estrellas", matEstrella, gravedad: 0.6f, crecer: false);

                // Destello, onda y chispas (solo si están sus materiales).
                var matDestello = Resources.Load<Material>(RecursoDestello);
                var matOnda = Resources.Load<Material>(RecursoOnda);
                var matChispa = Resources.Load<Material>(RecursoChispa);
                if (matDestello != null) anfitrion.destello = CrearSistema(go.transform, "Destello", matDestello, 0f, false, encoger: true);
                if (matOnda != null) anfitrion.onda = CrearSistema(go.transform, "Onda", matOnda, 0f, true, abrir: true);
                if (matChispa != null)
                {
                    anfitrion.chispas = CrearSistema(go.transform, "Chispas", matChispa, 1.2f, false);
                    var r = anfitrion.chispas.GetComponent<ParticleSystemRenderer>();
                    r.renderMode = ParticleSystemRenderMode.Stretch;
                    r.velocityScale = 0.06f;
                    r.lengthScale = 1.5f;
                }
                return anfitrion;
            }

            static ParticleSystem CrearSistema(Transform padre, string nombre, Material material, float gravedad, bool crecer,
                                               bool encoger = false, bool abrir = false)
            {
                var go = new GameObject(nombre);
                go.transform.SetParent(padre, false);
                var ps = go.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = ps.main;
                main.playOnAwake = false;
                main.loop = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 600;
                main.gravityModifier = gravedad;
                main.startSpeed = 0f;

                var emision = ps.emission;
                emision.enabled = false;
                var forma = ps.shape;
                forma.enabled = false;

                var alfa = new Gradient();
                alfa.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                             new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
                var color = ps.colorOverLifetime;
                color.enabled = true;
                color.color = alfa;

                var tamano = ps.sizeOverLifetime;
                tamano.enabled = true;
                // Polvo: crece; estrellas: se encogen; destello: se apaga de golpe; onda: se abre deprisa.
                tamano.size = new ParticleSystem.MinMaxCurve(1f,
                    abrir ? AnimationCurve.EaseInOut(0f, 0.15f, 1f, 1f)
                    : encoger ? AnimationCurve.Linear(0f, 1f, 1f, 0.3f)
                    : crecer ? AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f) : AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

                // Rozamiento con el aire: salen disparadas y se frenan enseguida.
                var frenado = ps.limitVelocityOverLifetime;
                frenado.enabled = true;
                frenado.limit = 100f;
                frenado.drag = crecer ? 4f : 2.5f;
                frenado.multiplyDragByParticleSize = false;
                frenado.multiplyDragByParticleVelocity = false;

                var r = go.GetComponent<ParticleSystemRenderer>();
                r.sharedMaterial = material;
                r.renderMode = ParticleSystemRenderMode.Billboard;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                ps.Play();
                return ps;
            }

            void Update()
            {
                if (finCamaraLenta > -50f && Time.unscaledTime >= finCamaraLenta)
                {
                    finCamaraLenta = -100f;
                    Time.timeScale = 1f;
                }
            }

            void OnDestroy()
            {
                Time.timeScale = 1f;
                if (anfitrion == this) anfitrion = null;
            }
        }
    }
}
