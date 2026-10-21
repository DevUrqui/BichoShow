using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;
using M = BichoShow.Herramientas.CreadorMudanza;
using A = BichoShow.Herramientas.CreadorArena;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menú "BichoShow > Crear escena: La Cumbre del Girasol". Gran final: una pista en espiral
    /// (300 m, 3 vueltas, pendiente del 15 %) alrededor del tallo de un girasol gigante, dentro de
    /// un invernadero con público. Sin puntos de control; la savia sube desde el suelo.
    ///  · Sección 1 (m 0-80, 13 m de ancho): mariquitas que rebotan (quietas y patrullando) y hojas trampolín.
    ///  · Sección 2 (m 80-180, 9 m): bolas de polen rodando por el centro, valla con huecos y puentes de hoja.
    ///  · Sección 3 (m 180-270, 7 m): suelo de néctar resbaladizo, mariquitas rápidas y ventiladores.
    ///  · Sección 4 (m 270-300, 6 m): anillo de pétalos que suben y bajan, y la corona sobre el disco.
    /// La pista es una malla hecha aquí (carriles de colores: verde a los lados, amarillo y naranja
    /// en el centro, por donde bajan las bolas); se guarda en Modelos/Girasol.
    /// </summary>
    public static class CreadorCumbre
    {
        const string RutaEscena = E.CarpetaEscenas + "/Cumbre.unity";
        const string CarpetaMallas = E.Raiz + "/Modelos/Girasol";

        // --- La espiral ---
        const float Largo = 300f;
        const float RadioInterior = 10f;
        const float Pendiente = 0.15f;
        const float AlturaSalida = 0.5f;
        const float Grosor = 0.6f;
        const float RadioTallo = 4.5f;
        const float AlturaDisco = 50f;
        static readonly float[] Carriles = { 0f, 0.28f, 0.72f, 1f };
        /// <summary>Tramos sin valla por fuera (m de pista).</summary>
        static readonly Vector2[] SinValla = { new Vector2(100f, 114f), new Vector2(124f, 132f), new Vector2(160f, 168f), new Vector2(202f, 214f), new Vector2(238f, 250f) };

        static float[] angulos;
        static Transform mapa, pista, obstaculos, decorado, rutaBots;
        static readonly List<PuntoRuta> ruta = new List<PuntoRuta>();
        static Material verdeA, verdeB, amarillo, naranja, rocioA, rocioB, nectarA, nectarB, borde, bajo, vallaLado, vallaTope;
        static Material tallo, hojaTrampolin, hojaPuente, petalo, cesped, baldosa, savia, mariquitaTela, banderaA, banderaB, banderaC, plataforma;
        static Material particula;

        [MenuItem("BichoShow/Crear escena: La Cumbre del Girasol")]
        public static void Crear()
        {
            var bicho = A.Preparar(RutaEscena, "La Cumbre del Girasol", out _);
            if (bicho == null) return;
            CrearEscena(bicho);
            Debug.Log("Bicho Show: escena de La Cumbre del Girasol creada. Es una final: el show la sortea con la otra.");
        }

        // =====================================================================
        // La espiral: medidas por metro de pista (s)
        // =====================================================================

        /// <summary>Embudo: 13 m en la base, 9 en el medio, 7 arriba y 6 al final.</summary>
        static float Ancho(float s)
        {
            if (s < 80f) return 13f;
            if (s < 100f) return Mathf.Lerp(13f, 9f, (s - 80f) / 20f);
            if (s < 180f) return 9f;
            if (s < 190f) return Mathf.Lerp(9f, 7f, (s - 180f) / 10f);
            if (s < 270f) return 7f;
            return Mathf.Lerp(7f, 6f, Mathf.Clamp01((s - 270f) / 30f));
        }

        static float Altura(float s) => AlturaSalida + Pendiente * s;

        /// <summary>Ángulo (radianes) de cada metro: se avanza "a la derecha" vista desde fuera (el ángulo baja).</summary>
        static void PrepararAngulos()
        {
            int n = Mathf.CeilToInt(Largo) + 2;
            angulos = new float[n + 1];
            float a = 0f;
            for (int i = 0; i <= n; i++)
            {
                angulos[i] = a;
                a -= 1f / (RadioInterior + Ancho(i) * 0.5f);
            }
        }

        static float Angulo(float s)
        {
            s = Mathf.Clamp(s, 0f, angulos.Length - 2);
            int i = Mathf.FloorToInt(s);
            return Mathf.Lerp(angulos[i], angulos[i + 1], s - i);
        }

        /// <summary>Punto de la cara de arriba de la pista: s metros, a una fracción f del ancho (0 dentro, 1 fuera).</summary>
        static Vector3 Punto(float s, float f)
        {
            float a = Angulo(s), r = RadioInterior + f * Ancho(s);
            return new Vector3(r * Mathf.Sin(a), Altura(s), r * Mathf.Cos(a));
        }

        static Vector3 Fuera(float s)
        {
            float a = Angulo(s);
            return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        }

        /// <summary>Hacia dónde se avanza (en horizontal).</summary>
        static Vector3 Adelante(float s)
        {
            Vector3 d = Punto(s + 0.5f, 0.5f) - Punto(s - 0.5f, 0.5f);
            d.y = 0f;
            return d.normalized;
        }

        static Quaternion Giro(float s) => Quaternion.LookRotation(Adelante(s), Vector3.up);

        static bool HayValla(float s)
        {
            foreach (var t in SinValla) if (s >= t.x && s < t.y) return false;
            return true;
        }

        // =====================================================================
        // Escena
        // =====================================================================

        static void CrearMateriales()
        {
            verdeA = E.CrearMaterial("CumbreVerdeA", new Color(0.42f, 0.78f, 0.3f), 0.35f);
            verdeB = E.CrearMaterial("CumbreVerdeB", new Color(0.32f, 0.68f, 0.26f), 0.35f);
            amarillo = E.CrearMaterial("CumbreAmarillo", new Color(1f, 0.82f, 0.22f), 0.35f);
            naranja = E.CrearMaterial("CumbreNaranja", new Color(1f, 0.58f, 0.18f), 0.35f);
            rocioA = E.CrearMaterial("CumbreRocioA", new Color(0.55f, 0.88f, 0.75f), 0.9f);
            rocioB = E.CrearMaterial("CumbreRocioB", new Color(0.45f, 0.8f, 0.68f), 0.9f);
            nectarA = E.CrearMaterial("CumbreNectarA", new Color(1f, 0.75f, 0.35f), 0.92f);
            nectarB = E.CrearMaterial("CumbreNectarB", new Color(1f, 0.62f, 0.28f), 0.92f);
            borde = E.CrearMaterial("CumbreBorde", new Color(1f, 0.5f, 0.38f), 0.3f);
            bajo = E.CrearMaterial("CumbreBajo", new Color(0.2f, 0.42f, 0.18f), 0.2f);
            vallaLado = E.CrearMaterial("CumbreVallaLado", new Color(1f, 0.55f, 0.3f), 0.35f);
            vallaTope = E.CrearMaterial("CumbreVallaTope", new Color(1f, 0.9f, 0.55f), 0.35f);
            tallo = E.CrearMaterial("CumbreTallo", new Color(0.36f, 0.66f, 0.24f), 0.3f);
            hojaTrampolin = E.CrearMaterial("CumbreHojaTrampolin", new Color(0.5f, 0.9f, 0.3f), 0.4f);
            hojaPuente = E.CrearMaterial("CumbreHojaPuente", new Color(0.4f, 0.75f, 0.28f), 0.35f);
            petalo = E.CrearMaterial("CumbrePetalo", new Color(1f, 0.8f, 0.12f), 0.35f);
            cesped = E.CrearMaterial("CumbreCesped", new Color(0.38f, 0.62f, 0.28f), 0.15f);
            baldosa = TelasCostura.Tela("CumbreBaldosa", new Color(0.93f, 0.72f, 0.58f), TelasCostura.TablaCorte, 3f, 0.25f);
            savia = E.CrearMaterial("CumbreSavia", new Color(0.95f, 0.58f, 0.12f), 0.95f);
            mariquitaTela = E.CrearMaterial("CumbreMariquita", new Color(0.92f, 0.16f, 0.14f), 0.6f);
            banderaA = E.CrearMaterial("CumbreBanderaA", new Color(0.36f, 0.62f, 1f), 0.3f);
            banderaB = E.CrearMaterial("CumbreBanderaB", new Color(1f, 0.42f, 0.66f), 0.3f);
            banderaC = E.CrearMaterial("CumbreBanderaC", new Color(1f, 0.6f, 0.15f), 0.3f);
            plataforma = E.CrearMaterial("CumbrePlataforma", new Color(0.42f, 0.24f, 0.6f), 0.3f);
            particula = AssetDatabase.LoadAssetAtPath<Material>($"{E.CarpetaMateriales}/Particula.mat");
            if (particula == null)
            {
                particula = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
                AssetDatabase.CreateAsset(particula, $"{E.CarpetaMateriales}/Particula.mat");
            }
        }

        static void CrearEscena(GameObject prefabBicho)
        {
            CrearMateriales();
            PrepararAngulos();
            ruta.Clear();
            E.CrearCarpeta(E.Raiz + "/Modelos", "Girasol");

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camara = A.Camara(16f, 40f);
            camara.orbitar = true;
            camara.ejeOrbita = Vector3.zero;
            camara.alturaOrbita = 0.7f;
            camara.centroSinJugadores = Punto(6f, 0.5f);
            camara.ColocarAlInstante();

            mapa = new GameObject("Mapa").transform;
            pista = M.Grupo(mapa, "Pista");
            obstaculos = M.Grupo(mapa, "Obstaculos");
            decorado = M.Grupo(mapa, "Decorado");
            rutaBots = M.Grupo(mapa, "RutaBots");

            CrearPista();
            CrearValla();
            CrearTallo();
            Seccion1();
            var bolas = Seccion2();
            Seccion3();
            var (corona, petalos) = Seccion4();
            var saviaGo = CrearSavia();
            var confeti = Invernadero();
            var dron = Abejorro();

            // Sin puntos de control: quien cae al suelo del invernadero vuelve a la salida.
            var suelo = M.ZonaTrigger<ZonaMortal>("SueloCaida", mapa, new Vector3(0f, -1.7f, 0f), new Vector3(130f, 4f, 130f));
            suelo.motivoObjeto = "se ha caído al suelo del invernadero";

            // Parrilla de salida escalonada.
            var apariciones = new[] { Punto(4f, 0.3f), Punto(4f, 0.7f), Punto(8f, 0.3f), Punto(8f, 0.7f) };
            var gestor = A.Jugadores(prefabBicho, apariciones, Punto(16f, 0.5f));
            foreach (var p in gestor.puntosAparicion) p.rotation = Giro(6f);

            var inicio = RutaBots(petalos);

            var rondaGo = new GameObject("RondaCumbre");
            var ronda = rondaGo.AddComponent<RondaCumbre>();
            ronda.gestor = gestor;
            ronda.saboteadorMaquina = false;
            ronda.savia = saviaGo;
            ronda.corona = corona;
            ronda.bolas = bolas;
            ronda.dron = dron;
            ronda.confeti = confeti;
            ronda.alturaCima = AlturaDisco;
            ronda.ruta = ruta.ToArray();
            ronda.inicioRuta = inicio;

            Ambientes.Aplicar(Ambientes.Tema.Invernadero);
            foreach (var luz in Object.FindObjectsByType<Light>())
                if (luz.type == LightType.Directional) { ronda.sol = luz; break; }
            EditorSceneManager.SaveScene(escena, RutaEscena);
            E.AnadirAlBuild(RutaEscena);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rondaGo;
        }

        // =====================================================================
        // Pista y valla (mallas)
        // =====================================================================

        /// <summary>Malla en construcción con varios materiales (submallas) y caras de cuatro vértices.</summary>
        class Malla
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int>[] sub;

            public Malla(int materiales)
            {
                sub = new List<int>[materiales];
                for (int i = 0; i < materiales; i++) sub[i] = new List<int>();
            }

            /// <summary>Cara a-b-c-d, girada para que mire hacia "normal".</summary>
            public void Cara(int m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
                bool derecha = Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0f;
                if (derecha) sub[m].AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                else sub[m].AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }

            public Mesh Guardar(string nombre)
            {
                string ruta = $"{CarpetaMallas}/{nombre}.asset";
                var malla = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
                bool nueva = malla == null;
                if (nueva) malla = new Mesh { name = nombre };
                malla.Clear();
                malla.indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                malla.SetVertices(v);
                malla.SetUVs(0, uv);
                malla.subMeshCount = sub.Length;
                for (int i = 0; i < sub.Length; i++) malla.SetTriangles(sub[i], i);
                malla.RecalculateNormals();
                malla.RecalculateBounds();
                if (nueva) AssetDatabase.CreateAsset(malla, ruta);
                else EditorUtility.SetDirty(malla);
                return malla;
            }
        }

        static void CrearPista()
        {
            // Materiales: 0-1 verde (lados), 2-3 amarillo/naranja (centro), 4-5 rocío, 6-7 néctar (sección 3), 8 borde, 9 debajo.
            var m = new Malla(10);
            Vector3 abajo = Vector3.down * Grosor;
            int pasos = Mathf.CeilToInt(Largo);
            for (int i = 0; i < pasos; i++)
            {
                float s0 = i, s1 = i + 1;
                int baldosaN = (i / 4) % 2;
                bool resbala = s0 >= 180f && s0 < 270f;
                for (int k = 0; k < 3; k++)
                {
                    float f0 = Carriles[k], f1 = Carriles[k + 1];
                    bool centro = k == 1;
                    int mat = resbala ? (centro ? 6 : 4) : (centro ? 2 : 0);
                    mat += baldosaN;
                    Vector3 a = Punto(s0, f0), b = Punto(s0, f1), c = Punto(s1, f1), d = Punto(s1, f0);
                    m.Cara(mat, a, b, c, d, Vector3.up,
                           new Vector2(s0, f0 * Ancho(s0)), new Vector2(s0, f1 * Ancho(s0)), new Vector2(s1, f1 * Ancho(s1)), new Vector2(s1, f0 * Ancho(s1)));
                }
                // Lados (borde acolchado) y la cara de abajo.
                Vector3 i0 = Punto(s0, 0f), i1 = Punto(s1, 0f), o0 = Punto(s0, 1f), o1 = Punto(s1, 1f);
                m.Cara(8, i0, i1, i1 + abajo, i0 + abajo, -Fuera(s0), new Vector2(s0, 0f), new Vector2(s1, 0f), new Vector2(s1, -Grosor), new Vector2(s0, -Grosor));
                m.Cara(8, o0, o1, o1 + abajo, o0 + abajo, Fuera(s0), new Vector2(s0, 0f), new Vector2(s1, 0f), new Vector2(s1, -Grosor), new Vector2(s0, -Grosor));
                m.Cara(9, i0 + abajo, o0 + abajo, o1 + abajo, i1 + abajo, Vector3.down, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
            // Tapas de los extremos.
            foreach (float s in new[] { 0f, Largo })
            {
                Vector3 a = Punto(s, 0f), b = Punto(s, 1f);
                m.Cara(8, a, b, b + abajo, a + abajo, s > 0f ? Adelante(s) : -Adelante(s), Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
            var malla = m.Guardar("EspiralGirasol");
            var go = new GameObject("Espiral");
            go.transform.SetParent(pista, false);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { verdeA, verdeB, amarillo, naranja, rocioA, rocioB, nectarA, nectarB, borde, bajo };
            go.AddComponent<MeshCollider>().sharedMesh = malla;
        }

        /// <summary>Valla acolchada por fuera (0,9 m), con huecos en algunos tramos.</summary>
        static void CrearValla()
        {
            var m = new Malla(2);
            const float alto = 0.9f, grueso = 0.5f;
            int pasos = Mathf.CeilToInt(Largo);
            bool antes = false;
            for (int i = 0; i < pasos; i++)
            {
                float s0 = i, s1 = i + 1;
                bool hay = HayValla(s0);
                float fd0 = 1f - grueso / Ancho(s0), fd1 = 1f - grueso / Ancho(s1);
                Vector3 d0 = Punto(s0, fd0), d1 = Punto(s1, fd1), o0 = Punto(s0, 1f), o1 = Punto(s1, 1f);
                Vector3 arriba = Vector3.up * alto;
                if (hay)
                {
                    m.Cara(0, d0, d1, d1 + arriba, d0 + arriba, -Fuera(s0), new Vector2(s0, 0f), new Vector2(s1, 0f), new Vector2(s1, alto), new Vector2(s0, alto));
                    m.Cara(0, o0, o1, o1 + arriba, o0 + arriba, Fuera(s0), new Vector2(s0, 0f), new Vector2(s1, 0f), new Vector2(s1, alto), new Vector2(s0, alto));
                    m.Cara(1, d0 + arriba, o0 + arriba, o1 + arriba, d1 + arriba, Vector3.up, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                }
                // Tapas donde empieza o acaba un tramo de valla.
                if (hay != antes)
                {
                    Vector3 n = hay ? -Adelante(s0) : Adelante(s0);
                    m.Cara(0, d0, o0, o0 + arriba, d0 + arriba, n, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                }
                antes = hay;
            }
            var malla = m.Guardar("VallaGirasol");
            var go = new GameObject("Valla");
            go.transform.SetParent(pista, false);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { vallaLado, vallaTope };
            go.AddComponent<MeshCollider>().sharedMesh = malla;
        }

        /// <summary>El tallo, con hojas grandes que sujetan la espiral por debajo.</summary>
        static void CrearTallo()
        {
            var t = M.Grupo(decorado, "Tallo");
            M.Decorado(PrimitiveType.Cylinder, "Tallo", t, new Vector3(0f, AlturaDisco * 0.5f - 1.5f, 0f), new Vector3(RadioTallo * 2f, AlturaDisco * 0.5f - 0.5f, RadioTallo * 2f), tallo);
            for (float s = 10f; s < Largo; s += 16f)
            {
                float a = Angulo(s) * Mathf.Rad2Deg;
                var fuera = Fuera(s);
                Kit("HojaTallo", t, fuera * (RadioTallo - 0.3f) + Vector3.up * (Altura(s) - 2.4f), a, null, 1.45f);
            }
        }

        // =====================================================================
        // Sección 1: mariquitas y hojas trampolín (m 0-80)
        // =====================================================================

        static void Seccion1()
        {
            var g = M.Grupo(obstaculos, "Seccion 1");
            Mariquita(g, 20f, 0.5f, 0.5f, 0.5f, 4f, 0f);
            Mariquita(g, 38f, 0.25f, 0.25f, 0.25f, 4f, 0f);
            Mariquita(g, 56f, 0.75f, 0.75f, 0.75f, 4f, 0f);
            Mariquita(g, 28f, 0.5f, 0.15f, 0.85f, 4f, 0f);
            Mariquita(g, 47f, 0.5f, 0.15f, 0.85f, 3.5f, 0.3f);
            Mariquita(g, 66f, 0.5f, 0.15f, 0.85f, 3f, 0.6f);
            Trampolin(g, 14f, 0.08f, 33f);
            Trampolin(g, 44f, 0.92f, 63f);
        }

        /// <summary>Mariquita acolchada en el metro s. Si fA ≠ fB, patrulla de lado a lado entre esas dos fracciones del ancho.</summary>
        static MariquitaBumper Mariquita(Transform padre, float s, float f, float fA, float fB, float periodo, float desfase)
        {
            var go = new GameObject($"Mariquita m{s:0}");
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(Punto(s, f), Quaternion.LookRotation(-Adelante(s), Vector3.up)); // mira a quien sube
            go.AddComponent<Rigidbody>().isKinematic = true;
            var capsula = go.AddComponent<CapsuleCollider>();
            capsula.direction = 2;
            capsula.radius = 1.3f;
            capsula.height = 3.2f;
            capsula.center = new Vector3(0f, 0.8f, 0.2f);
            var visual = Kit("Mariquita", go.transform, Vector3.zero, 0f, null, 1f, local: true);
            var bumper = go.AddComponent<MariquitaBumper>();
            bumper.visual = visual.transform;
            bumper.periodo = periodo;
            bumper.desfase = desfase;
            bumper.puntoA = Punto(s, fA);
            bumper.puntoB = Punto(s, fB);
            return bumper;
        }

        /// <summary>Hoja trampolín en el borde de la pista (metro s, fracción f) que lanza hasta el metro sDestino.</summary>
        static HojaTrampolin Trampolin(Transform padre, float s, float f, float sDestino)
        {
            var go = new GameObject($"Hoja trampolin m{s:0}");
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(Punto(s, f) + Vector3.up * 0.02f, Giro(s));
            var caja = go.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.size = new Vector3(2.8f, 1f, 2.8f);
            caja.center = new Vector3(0f, 0.5f, 0f);
            var visual = Kit("HojaRedonda", go.transform, Vector3.zero, 0f, hojaTrampolin, 1f, local: true);
            var destino = new GameObject("Destino").transform;
            destino.SetParent(go.transform, true);
            destino.position = Punto(sDestino, f) + Vector3.up * 0.6f;
            var hoja = go.AddComponent<HojaTrampolin>();
            hoja.destino = destino;
            hoja.visual = visual.transform;
            hoja.render = visual.GetComponentInChildren<Renderer>();
            // Una flecha en el destino, para que se entienda adónde lleva.
            Kit("HojaRedonda", decorado, Punto(sDestino, f) + Vector3.up * 0.01f, 0f, verdeB, 0.6f);
            return hoja;
        }

        // =====================================================================
        // Sección 2: bombardeo de polen y puentes de hoja (m 80-180)
        // =====================================================================

        static BolasPolen Seccion2()
        {
            var g = M.Grupo(obstaculos, "Seccion 2");
            // Las bolas: salen del cañón en el metro 176 y bajan rodando hasta el 74.
            var camino = new List<Vector3>();
            var fuera = new List<Vector3>();
            var ancho = new List<float>();
            for (float s = 176f; s >= 74f; s -= 2f)
            {
                camino.Add(Punto(s, 0.5f));
                fuera.Add(Fuera(s));
                ancho.Add(Ancho(s));
            }
            var bolasGo = new GameObject("Bolas de polen");
            bolasGo.transform.SetParent(g, false);
            var bolas = bolasGo.AddComponent<BolasPolen>();
            var lista = new List<Rigidbody>();
            for (int i = 0; i < 7; i++)
            {
                var b = new GameObject($"Bola de polen {i + 1}");
                b.transform.SetParent(bolasGo.transform, false);
                b.transform.position = camino[0] + Vector3.up * 1.2f;
                var rb = b.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                b.AddComponent<SphereCollider>().radius = 1.15f;
                Kit("BolaPolen", b.transform, Vector3.zero, 0f, null, 1f, local: true);
                lista.Add(rb);
            }
            bolas.bolas = lista.ToArray();
            bolas.camino = camino.ToArray();
            bolas.fuera = fuera.ToArray();
            bolas.ancho = ancho.ToArray();
            // El cañón de polen, arriba.
            KitObjetos.Poner("CanonConfeti", decorado, Punto(179f, 0.5f), Angulo(179f) * Mathf.Rad2Deg + 90f, null, 2f, false, KitObjetos.KitPlato);

            // Puentes de hoja: atajos por dentro de la espiral (de una curva a la siguiente).
            PuenteHoja(g, 116f, 142f);
            PuenteHoja(g, 150f, 176f);
            return bolas;
        }

        static PuenteHoja PuenteHoja(Transform padre, float sA, float sB)
        {
            Vector3 a = Punto(sA, 0.1f), b = Punto(sB, 0.1f);
            Vector3 dir = b - a;
            float largo = dir.magnitude + 1.2f;
            var go = new GameObject($"Puente de hoja m{sA:0}-{sB:0}");
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.LookRotation(dir.normalized, Vector3.up));
            go.AddComponent<Rigidbody>().isKinematic = true;
            var caja = go.AddComponent<BoxCollider>();
            caja.size = new Vector3(3f, 0.3f, largo);
            caja.center = new Vector3(0f, -0.15f, 0f);
            var visual = Kit("HojaLarga", go.transform, new Vector3(0f, -0.15f, 0f), 0f, hojaPuente, 1f, local: true);
            visual.transform.localScale = Vector3.Scale(visual.transform.localScale, new Vector3(1f, 1f, largo / 14f));
            var puente = go.AddComponent<PuenteHoja>();
            puente.tamano = new Vector2(3f, largo);
            return puente;
        }

        // =====================================================================
        // Sección 3: el tallo ventoso (m 180-270)
        // =====================================================================

        static void Seccion3()
        {
            var g = M.Grupo(obstaculos, "Seccion 3");
            var zona = new GameObject("Nectar resbaladizo").AddComponent<ZonaResbaladiza>();
            zona.transform.SetParent(g, false);
            zona.alturaDesde = Altura(180f) - 0.5f;
            zona.alturaHasta = Altura(270f) + 1.5f;
            zona.radioDesde = RadioInterior - 0.5f;
            zona.radioHasta = RadioInterior + 9.5f;

            float[] sitios = { 195f, 215f, 235f, 255f };
            for (int i = 0; i < sitios.Length; i++) Mariquita(g, sitios[i], 0.5f, 0.12f, 0.88f, 2f, i * 0.3f);

            foreach (float s in new[] { 208f, 244f })
            {
                float giro = Angulo(s) * Mathf.Rad2Deg;
                var ventilador = Kit("Ventilador", g, Punto(s, 0.03f), giro, banderaA, 1.2f);
                var aspas = Kit("AspasVentilador", ventilador.transform, new Vector3(0f, 2.8f, 0.2f), 0f, banderaA, 1f, local: true);
                var viento = new GameObject($"Viento m{s:0}").AddComponent<VientoLateral>();
                viento.transform.SetParent(g, false);
                viento.transform.SetPositionAndRotation(Punto(s, 0f), Quaternion.LookRotation(Fuera(s), Vector3.up));
                viento.tamanoZona = new Vector3(10f, 4f, Ancho(s) + 2f);
                viento.aspas = aspas.transform;
                viento.desfase = s > 220f ? 0.5f : 0f;
                // Cañón de confeti al lado (decorado del plató).
                KitObjetos.Poner("CanonConfeti", decorado, Punto(s + 4f, 0.05f), giro, null, 1.2f, false, KitObjetos.KitPlato);
            }
        }

        // =====================================================================
        // Sección 4: la corona de pétalos (m 270-300 y la cabeza)
        // =====================================================================

        static (CoronaCumbre corona, PetaloPiano[] petalos) Seccion4()
        {
            var g = M.Grupo(obstaculos, "Seccion 4");
            float aFin = Angulo(Largo);
            const float radioPetalos = 10f;
            var petalos = new PetaloPiano[6];
            for (int k = 0; k < petalos.Length; k++)
            {
                float a = aFin - (16f + 22f * k) * Mathf.Deg2Rad;
                var fuera = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                var go = new GameObject($"Petalo {k + 1}");
                go.transform.SetParent(g, false);
                go.transform.SetPositionAndRotation(fuera * radioPetalos + Vector3.up * (46.1f + 0.6f * k), Quaternion.LookRotation(fuera, Vector3.up));
                go.AddComponent<Rigidbody>().isKinematic = true;
                var caja = go.AddComponent<BoxCollider>();
                caja.size = new Vector3(2.2f, 0.3f, 4.4f);
                Kit("Petalo", go.transform, Vector3.zero, 0f, petalo, 1f, local: true);
                var p = go.AddComponent<PetaloPiano>();
                p.amplitud = 0.8f;
                p.periodo = 3.2f;
                p.desfase = k * 0.17f;
                petalos[k] = p;
            }

            // La cabeza del girasol: disco pisable con su corona de pétalos (estos, de adorno, levantados).
            var cabeza = M.Grupo(decorado, "Cabeza del girasol");
            Kit("CabezaGirasol", cabeza, new Vector3(0f, AlturaDisco, 0f));
            SoloColision(A.Disco(cabeza, "Disco", Vector3.zero, AlturaDisco, 6f, 1f, tallo));
            float desde = 16f + 22f * petalos.Length + 6f;
            for (float grados = desde; grados < 360f - 10f; grados += 20f)
            {
                float a = aFin - grados * Mathf.Deg2Rad;
                var fuera = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                var adorno = Kit("Petalo", cabeza, fuera * 9.5f + Vector3.up * (AlturaDisco + 1.2f), 0f, petalo, 1.1f);
                adorno.transform.rotation = Quaternion.LookRotation(fuera, Vector3.up) * Quaternion.Euler(-40f, 0f, 0f) * adorno.transform.rotation;
            }

            // La corona, sobre el géiser de polen.
            var coronaGo = new GameObject("Corona Dorada");
            coronaGo.transform.SetParent(g, false);
            coronaGo.transform.position = new Vector3(0f, AlturaDisco + 2.8f, 0f); // al alcance de un salto (unos 2 m)
            KitObjetos.Poner("Corona", coronaGo.transform, new Vector3(0f, -0.9f, 0f), 0f, null, 3f, true, KitObjetos.KitPlato);
            var corona = coronaGo.AddComponent<CoronaCumbre>();
            corona.amplitud = 0.8f;
            corona.alcance = 1.6f;

            var geiser = M.Particulas("Geiser de polen", g, new Vector3(0f, AlturaDisco + 0.1f, 0f), new Color(1f, 0.85f, 0.3f), new Color(1f, 0.95f, 0.6f), 30, 2f, 4f, particula);
            var main = geiser.main;
            main.loop = true;
            main.playOnAwake = true;
            main.gravityModifier = -0.05f;
            var emision = geiser.emission;
            emision.rateOverTime = 30f;
            var forma = geiser.shape;
            forma.angle = 8f;
            forma.radius = 0.8f;
            return (corona, petalos);
        }

        // =====================================================================
        // Savia, invernadero, público y el abejorro
        // =====================================================================

        static Savia CrearSavia()
        {
            var go = new GameObject("Savia");
            go.transform.SetParent(mapa, false);
            go.transform.position = new Vector3(0f, -1f, 0f);
            M.Decorado(PrimitiveType.Cylinder, "Superficie", go.transform, new Vector3(0f, -0.5f, 0f), new Vector3(112f, 0.5f, 112f), savia, local: true);
            var s = go.AddComponent<Savia>();
            s.velocidad = 0.38f;
            s.retraso = 8f;
            s.alturaMaxima = AlturaDisco - 3f;
            return s;
        }

        static ParticleSystem[] Invernadero()
        {
            var inv = M.Grupo(decorado, "Invernadero");
            // Suelo: baldosas y un círculo de césped alrededor del tallo.
            M.Decorado(PrimitiveType.Cylinder, "Suelo", inv, new Vector3(0f, -0.25f, 0f), new Vector3(116f, 0.25f, 116f), baldosa);
            M.Decorado(PrimitiveType.Cylinder, "Cesped", inv, new Vector3(0f, -0.2f, 0f), new Vector3(20f, 0.25f, 20f), cesped);
            // Costillas de hierro de la cúpula y tres anillos.
            const float radio = 56f;
            for (int k = 0; k < 12; k++) Kit("ArcoInvernadero", inv, Vector3.zero, k * 15f, null, radio);
            foreach (float h in new[] { 18f, 34f, 46f })
            {
                float r = Mathf.Sqrt(radio * radio - h * h);
                foreach (float giro in new[] { 0f, 180f })
                {
                    var anillo = Kit("ArcoInvernadero", inv, Vector3.up * h, giro, null, r);
                    anillo.transform.rotation = Quaternion.Euler(0f, giro, 0f) * Quaternion.Euler(90f, 0f, 0f) * Quaternion.Inverse(Quaternion.Euler(0f, giro, 0f)) * anillo.transform.rotation;
                }
            }

            // Público: gradas a distintas alturas alrededor, mirando al girasol.
            var publico = new List<ParticleSystem>();
            Material[] colores = PublicoMateriales();
            for (int k = 0; k < 6; k++)
            {
                float a = (k * 60f + 20f) * Mathf.Deg2Rad;
                float h = 2f + k * 7.5f;
                var sitio = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 47f + Vector3.up * h;
                Grada(inv, sitio, colores, k);
                if (k % 2 == 0)
                {
                    var c = M.Particulas($"Confeti {k / 2 + 1}", inv, sitio + Vector3.up * 3f, new Color(1f, 0.42f, 0.66f), new Color(1f, 0.85f, 0.3f), 160, 8f, 14f, particula);
                    c.transform.rotation = Quaternion.LookRotation(Vector3.up - sitio.normalized * 0.3f);
                    publico.Add(c);
                    KitObjetos.Poner("CanonConfeti", inv, sitio + sitio.normalized * -4f + Vector3.down * 0.2f, KitObjetos.GiroMirando(sitio, Vector3.zero), null, 1.5f, false, KitObjetos.KitPlato);
                }
            }
            // Focos del plató y pantallas.
            for (int k = 0; k < 6; k++)
            {
                float a = (k * 60f + 50f) * Mathf.Deg2Rad;
                var pie = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 52f;
                KitObjetos.Poner("TorreFocos", inv, pie, KitObjetos.GiroMirando(pie, Vector3.up * 30f), null, 3f, false, KitObjetos.KitPlato);
            }
            foreach (float grados in new[] { 140f, 260f })
            {
                float a = grados * Mathf.Deg2Rad;
                var sitio = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 50f + Vector3.up * 22f;
                KitObjetos.Poner("Pantalla", inv, sitio, KitObjetos.GiroMirando(sitio, Vector3.zero), null, 1f, false, KitObjetos.KitPlato);
                M.Cartel(inv, "BICHO SHOW", sitio + Vector3.up * 4.6f - sitio.normalized * 0.6f, 2.2f, Color.white);
            }
            // Banderolas colgando de la cúpula.
            Material[] telas = { banderaA, banderaB, banderaC };
            for (int k = 0; k < 9; k++)
            {
                float a = (k * 40f + 10f) * Mathf.Deg2Rad;
                var sitio = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 40f + Vector3.up * (24f + (k % 3) * 5f);
                Kit("Banderola", inv, sitio, KitObjetos.GiroMirando(sitio, Vector3.zero), telas[k % telas.Length], 1.6f);
            }
            return publico.ToArray();
        }

        static Material[] PublicoMateriales()
        {
            Color[] colores =
            {
                new Color(1f, 0.45f, 0.45f), new Color(0.4f, 0.65f, 1f), new Color(1f, 0.8f, 0.3f),
                new Color(0.5f, 0.9f, 0.5f), new Color(0.85f, 0.55f, 1f), new Color(1f, 0.6f, 0.85f),
            };
            var m = new Material[colores.Length];
            for (int i = 0; i < colores.Length; i++) m[i] = E.CrearMaterial($"Publico{i}", colores[i], 0.3f);
            return m;
        }

        /// <summary>Grada del plató sobre una plataforma con pata, con público de bichos animado.</summary>
        static void Grada(Transform padre, Vector3 sitio, Material[] colores, int semilla)
        {
            M.Decorado(PrimitiveType.Cube, "Plataforma", padre, sitio + Vector3.down * 0.3f, new Vector3(10f, 0.6f, 7f), plataforma);
            if (sitio.y > 1f)
                M.Decorado(PrimitiveType.Cylinder, "Pata", padre, new Vector3(sitio.x, sitio.y * 0.5f - 0.3f, sitio.z), new Vector3(1.4f, sitio.y * 0.5f, 1.4f), plataforma);
            var grupo = M.Grupo(padre, "Gradas");
            grupo.gameObject.AddComponent<PublicoAnimado>();
            var grada = KitObjetos.Poner("Grada", grupo, sitio, KitObjetos.GiroMirando(sitio, new Vector3(0f, sitio.y, 0f)), null, 1f, false, KitObjetos.KitPlato);
            var azar = new System.Random(semilla * 7919 + 13);
            for (int fila = 0; fila < 4; fila++)
                for (int s = 0; s < 5; s++)
                {
                    if (azar.NextDouble() < 0.12) continue;
                    float px = -3.2f + 1.6f * s + (float)(azar.NextDouble() - 0.5) * 0.5f;
                    KitObjetos.Poner(azar.NextDouble() < 0.35 ? "PublicoBrazos" : "Publico", grada.transform,
                        new Vector3(px, 0.5f * (fila + 1) + 0.1f, 1.35f - 1.1f * fila), (float)(azar.NextDouble() - 0.5) * 30f,
                        colores[azar.Next(colores.Length)], 1f + (float)(azar.NextDouble() - 0.5) * 0.2f, true, KitObjetos.KitPlato);
                }
        }

        static AbejorroDron Abejorro()
        {
            var go = new GameObject("Abejorro con camara");
            go.transform.SetParent(obstaculos, false);
            go.transform.position = new Vector3(0f, 30f, 20f);
            Kit("Abejorro", go.transform, Vector3.zero, 180f, null, 1.2f, local: true);
            var izq = new GameObject("Ala izquierda").transform;
            izq.SetParent(go.transform, false);
            izq.localPosition = new Vector3(-0.4f, 0.85f, 0.2f);
            Kit("AbejorroAla", izq, Vector3.zero, 180f, null, 1.2f, local: true);
            var der = new GameObject("Ala derecha").transform;
            der.SetParent(go.transform, false);
            der.localPosition = new Vector3(0.4f, 0.85f, 0.2f);
            Kit("AbejorroAla", der, Vector3.zero, 0f, null, 1.2f, local: true);
            var dron = go.AddComponent<AbejorroDron>();
            dron.reposo = new Vector3(0f, 28f, 0f);
            dron.ejeTallo = Vector3.zero;
            dron.alaIzquierda = izq;
            dron.alaDerecha = der;
            return dron;
        }

        // =====================================================================
        // Ruta de los bots
        // =====================================================================

        static PuntoRuta RutaBots(PetaloPiano[] petalos)
        {
            // La espiral, cada 7 m: por el centro, salvo en el polen (por el carril de dentro).
            var normal = new List<(float s, PuntoRuta p)>();
            for (float s = 6f; s <= Largo - 2f; s += 7f)
            {
                bool polen = s >= 80f && s < 180f;
                float f = polen ? 0.16f : 0.5f;
                float anchura = s < 80f ? 3f : polen ? 0.7f : s < 270f ? 1.2f : 0.9f;
                normal.Add((s, Ruta($"m{s:0}", Punto(s, f), anchura)));
            }
            for (int i = 0; i + 1 < normal.Count; i++) Unir(normal[i].p, normal[i + 1].p);

            // Puentes de hoja: atajo para los hábiles.
            Atajo(normal, 113f, 116f, 142f, 146f);
            Atajo(normal, 147f, 150f, 176f, 180f);

            // Del final de la pista, de pétalo en pétalo, al disco y a por la corona.
            var fin = Ruta("Salto al primer petalo", Punto(Largo - 0.6f, 0.35f), 0.3f, saltar: true, esperar: petalos[0]);
            Unir(normal[normal.Count - 1].p, fin);
            PuntoRuta anterior = fin;
            for (int k = 0; k < petalos.Length; k++)
            {
                bool ultimo = k == petalos.Length - 1;
                var p = Ruta($"Petalo {k + 1}", petalos[k].transform.position + Vector3.up * 0.15f, 0.2f, saltar: true,
                             esperar: ultimo ? null : petalos[k + 1]);
                Unir(anterior, p);
                anterior = p;
            }
            Vector3 haciaPetalo = petalos[petalos.Length - 1].transform.position;
            haciaPetalo.y = 0f;
            var disco = Ruta("Disco", haciaPetalo.normalized * 3f + Vector3.up * AlturaDisco, 0.4f);
            Unir(anterior, disco);
            var corona = Ruta("Corona", new Vector3(0f, AlturaDisco, 0f), 0.2f, saltar: true);
            corona.agarrar = true;
            Unir(disco, corona);
            Unir(corona, corona); // salta con la mano estirada hasta cogerla
            return normal[0].p;
        }

        /// <summary>Atajo por un puente de hoja: [0] el puente (difícil), [1] la espiral.</summary>
        static void Atajo(List<(float s, PuntoRuta p)> normal, float sCruce, float sEntrada, float sSalida, float sVuelta)
        {
            PuntoRuta cruce = null, vuelta = null;
            foreach (var (s, p) in normal)
            {
                if (cruce == null && s >= sCruce) cruce = p;
                if (vuelta == null && s >= sVuelta) vuelta = p;
            }
            if (cruce == null || vuelta == null || cruce.siguientes == null) return;
            var entrada = Ruta($"Entrada puente m{sEntrada:0}", Punto(sEntrada, 0.12f), 0.3f);
            var medio = Ruta($"Puente m{sEntrada:0}", (Punto(sEntrada, 0.1f) + Punto(sSalida, 0.1f)) * 0.5f, 0.3f);
            var salida = Ruta($"Salida puente m{sSalida:0}", Punto(sSalida + 1.5f, 0.15f), 0.4f);
            Unir(entrada, medio);
            Unir(medio, salida);
            Unir(salida, vuelta);
            Unir(cruce, entrada, cruce.siguientes[0]);
        }

        static PuntoRuta Ruta(string nombre, Vector3 posicion, float anchura, bool saltar = false, MonoBehaviour esperar = null)
        {
            var go = new GameObject($"Ruta {ruta.Count + 1}: {nombre}");
            go.transform.SetParent(rutaBots, false);
            go.transform.position = posicion;
            var p = go.AddComponent<PuntoRuta>();
            p.anchura = anchura;
            p.saltar = saltar;
            p.esperarA = esperar;
            ruta.Add(p);
            return p;
        }

        static void Unir(PuntoRuta desde, params PuntoRuta[] siguientes) => desde.siguientes = siguientes;

        // =====================================================================
        // Utilidades
        // =====================================================================

        static GameObject Kit(string pieza, Transform padre, Vector3 posicion, float giro = 0f, Material equipo = null,
                              float escala = 1f, bool local = false) =>
            KitObjetos.Poner(pieza, padre, posicion, giro, equipo, escala, local, KitObjetos.KitGirasol);

        static GameObject SoloColision(GameObject go)
        {
            if (go != null && go.TryGetComponent(out MeshRenderer r)) Object.DestroyImmediate(r);
            return go;
        }
    }
}
