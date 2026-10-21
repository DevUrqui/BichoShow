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
    /// (440 m, casi 4 vueltas, pendiente del 13 %) alrededor del tallo de un girasol gigante, dentro
    /// de un invernadero con público. Sin puntos de control; la savia sube desde el suelo.
    ///  · Sección 1 (m 0-110, 13 m de ancho): mariquitas que rebotan (quietas y patrullando) y hojas trampolín.
    ///  · Sección 2 (m 110-230, 9 m): bolas de polen de cuatro clases por todo el ancho, mariquitas que barren,
    ///    valla con huecos y puentes de hoja.
    ///  · Sección 3 (m 230-340, 7 m): suelo de néctar resbaladizo, mariquitas rápidas y ventiladores.
    ///  · Sección 4 (m 340-428, 6 m): la pista se rompe; los huecos se cruzan sobre pétalos que suben y bajan.
    ///  · La cima: la cabeza del girasol gira y, en su centro, el arco de META gira con ella.
    /// La pista es una malla hecha aquí (carriles de colores: verde a los lados, amarillo y naranja
    /// en el centro, por donde bajan las bolas); se guarda en Modelos/Girasol.
    /// </summary>
    public static class CreadorCumbre
    {
        public const string RutaEscena = E.CarpetaEscenas + "/Cumbre.unity";
        /// <summary>Súbela al cambiar el generador: así, al darle a Play, Unity avisa si la escena es vieja (EscenasAnticuadas).</summary>
        public const int Version = 4;
        const string CarpetaMallas = E.Raiz + "/Modelos/Girasol";

        // --- La espiral ---
        const float Largo = 440f;
        /// <summary>Desde aquí la pista ya no sube: es la recta final, a ras de la cabeza.</summary>
        const float SubidaHasta = 428f;
        const float RadioInterior = 14f;
        const float Pendiente = 0.13f;
        const float AlturaSalida = 0.5f;
        const float Grosor = 0.6f;
        const float RadioTallo = 6.5f;
        const float RadioCabeza = 14.3f;
        static readonly float AlturaCima = AlturaSalida + Pendiente * SubidaHasta;
        static readonly float[] Carriles = { 0f, 0.28f, 0.72f, 1f };
        /// <summary>Tramos sin valla por fuera (m de pista).</summary>
        static readonly Vector2[] SinValla =
        {
            new Vector2(88f, 98f),
            new Vector2(128f, 140f), new Vector2(150f, 158f), new Vector2(172f, 182f), new Vector2(196f, 204f), new Vector2(214f, 222f),
            new Vector2(252f, 260f), new Vector2(288f, 294f), new Vector2(322f, 328f),
        };
        /// <summary>Huecos de la sección 4 (desde, hasta, pétalos que lo cruzan).</summary>
        static readonly Vector3[] Huecos =
        {
            new Vector3(348f, 353f, 1f), new Vector3(359f, 364f, 1f), new Vector3(370f, 378f, 2f),
            new Vector3(385f, 390f, 1f), new Vector3(396f, 404f, 2f), new Vector3(410f, 415f, 1f),
        };

        static float[] angulos;
        static Transform mapa, pista, obstaculos, decorado, rutaBots;
        static readonly List<PuntoRuta> ruta = new List<PuntoRuta>();
        static Material verdeA, verdeB, amarillo, naranja, rocioA, rocioB, nectarA, nectarB, borde, bajo, vallaLado, vallaTope;
        static Material tallo, hojaTrampolin, hojaPuente, petalo, cesped, baldosa, savia, banderaA, banderaB, banderaC, plataforma, oro;
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

        /// <summary>Embudo: 13 m en la base, 9 en el bombardeo, 7 en el tallo ventoso y 6 arriba.</summary>
        static float Ancho(float s)
        {
            if (s < 110f) return 13f;
            if (s < 125f) return Mathf.Lerp(13f, 9f, (s - 110f) / 15f);
            if (s < 230f) return 9f;
            if (s < 240f) return Mathf.Lerp(9f, 7f, (s - 230f) / 10f);
            if (s < 340f) return 7f;
            if (s < 350f) return Mathf.Lerp(7f, 6f, (s - 340f) / 10f);
            return 6f;
        }

        static float Altura(float s) => AlturaSalida + Pendiente * Mathf.Min(s, SubidaHasta);

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

        static Vector3 Direccion(float radianes) => new Vector3(Mathf.Sin(radianes), 0f, Mathf.Cos(radianes));

        /// <summary>Hacia dónde se avanza (en horizontal).</summary>
        static Vector3 Adelante(float s)
        {
            Vector3 d = Punto(s + 0.5f, 0.5f) - Punto(s - 0.5f, 0.5f);
            d.y = 0f;
            return d.normalized;
        }

        static Quaternion Giro(float s) => Quaternion.LookRotation(Adelante(s), Vector3.up);

        static bool EnHueco(float s)
        {
            foreach (var h in Huecos) if (s >= h.x && s < h.y) return true;
            return false;
        }

        static bool HayValla(float s)
        {
            if (EnHueco(s)) return false;
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
            banderaA = E.CrearMaterial("CumbreBanderaA", new Color(0.36f, 0.62f, 1f), 0.3f);
            banderaB = E.CrearMaterial("CumbreBanderaB", new Color(1f, 0.42f, 0.66f), 0.3f);
            banderaC = E.CrearMaterial("CumbreBanderaC", new Color(1f, 0.6f, 0.15f), 0.3f);
            plataforma = E.CrearMaterial("CumbrePlataforma", new Color(0.42f, 0.24f, 0.6f), 0.3f);
            oro = E.CrearMaterial("CumbreMeta", new Color(1f, 0.78f, 0.15f), 0.7f);
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
            // Cámara en órbita, tranquila: no gira hasta que el grupo se aleja del centro del plano.
            var camara = A.Camara(20f, 46f);
            camara.orbitar = true;
            camara.ejeOrbita = Vector3.zero;
            camara.alturaOrbita = 0.8f;
            camara.margenOrbita = 16f;
            camara.velocidadMaximaOrbita = 22f;
            camara.adelantoOrbita = -10f; // un poco por delante: la carrera va hacia ángulos menores
            camara.anticipacion = 0.6f;   // trampolines y atajos: que no se salgan por arriba
            camara.centroSinJugadores = Punto(6f, 0.5f);
            camara.ColocarAlInstante();

            mapa = new GameObject("Mapa").transform;
            new GameObject(EscenasAnticuadas.Marca + Version);
            pista = M.Grupo(mapa, "Pista");
            obstaculos = M.Grupo(mapa, "Obstaculos");
            decorado = M.Grupo(mapa, "Decorado");
            rutaBots = M.Grupo(mapa, "RutaBots");
            // El decorado que se ponga entre la cámara y un bicho se vuelve invisible (deja la sombra).
            camara.gameObject.AddComponent<CamaraSinEstorbos>().raices = new[] { decorado };

            CrearPista();
            CrearValla();
            CrearTallo();
            Seccion1();
            var bolas = Seccion2();
            Seccion3();
            var petalos = Seccion4();
            var (meta, delante, detras) = Cima();
            var saviaGo = CrearSavia();
            var confeti = Invernadero();
            var dron = Abejorro();

            // Sin puntos de control: quien cae al suelo del invernadero vuelve a la salida.
            var suelo = M.ZonaTrigger<ZonaMortal>("SueloCaida", mapa, new Vector3(0f, -1.7f, 0f), new Vector3(170f, 4f, 170f));
            suelo.motivoObjeto = "se ha caído al suelo del invernadero";

            // Parrilla de salida escalonada.
            var apariciones = new[] { Punto(4f, 0.3f), Punto(4f, 0.7f), Punto(8f, 0.3f), Punto(8f, 0.7f) };
            var gestor = A.Jugadores(prefabBicho, apariciones, Punto(16f, 0.5f));
            foreach (var p in gestor.puntosAparicion) p.rotation = Giro(6f);

            var inicio = RutaBots(petalos, delante, detras);

            var rondaGo = new GameObject("RondaCumbre");
            var ronda = rondaGo.AddComponent<RondaCumbre>();
            ronda.gestor = gestor;
            ronda.saboteadorMaquina = false;
            ronda.savia = saviaGo;
            ronda.bolas = bolas;
            ronda.dron = dron;
            ronda.meta = meta;
            ronda.confeti = confeti;
            ronda.alturaCima = AlturaCima;
            ronda.duracionMinima = 210f;
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
                if (EnHueco(s0 + 0.5f)) continue;
                int baldosaN = (i / 4) % 2;
                bool resbala = s0 >= 230f && s0 < 340f;
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
            // Tapas de los extremos y de los huecos.
            var tapas = new List<(float s, bool alFinal)> { (0f, false), (Largo, true) };
            foreach (var h in Huecos) { tapas.Add((h.x, true)); tapas.Add((h.y, false)); }
            foreach (var (s, alFinal) in tapas)
            {
                Vector3 a = Punto(s, 0f), b = Punto(s, 1f);
                m.Cara(8, a, b, b + abajo, a + abajo, alFinal ? Adelante(s) : -Adelante(s), Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
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
            for (int i = 0; i <= pasos; i++)
            {
                float s0 = i, s1 = i + 1;
                bool hay = i < pasos && HayValla(s0);
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
            float alto = AlturaCima - 1f;
            M.Decorado(PrimitiveType.Cylinder, "Tallo", t, new Vector3(0f, alto * 0.5f - 0.5f, 0f), new Vector3(RadioTallo * 2f, alto * 0.5f, RadioTallo * 2f), tallo);
            float escala = 1.45f * (RadioInterior - RadioTallo) / 5.5f;
            for (float s = 10f; s < SubidaHasta; s += 16f)
            {
                if (EnHueco(s)) continue;
                Kit("HojaTallo", t, Fuera(s) * (RadioTallo - 0.3f) + Vector3.up * (Altura(s) - 2.4f), Angulo(s) * Mathf.Rad2Deg, null, escala);
            }
        }

        // =====================================================================
        // Sección 1: mariquitas y hojas trampolín (m 0-110)
        // =====================================================================

        static void Seccion1()
        {
            var g = M.Grupo(obstaculos, "Seccion 1");
            MariquitaPista(g, 20f, 0.5f, 0.5f, 0.5f, 4f, 0f);
            MariquitaPista(g, 38f, 0.25f, 0.25f, 0.25f, 4f, 0f);
            MariquitaPista(g, 56f, 0.75f, 0.75f, 0.75f, 4f, 0f);
            MariquitaPista(g, 84f, 0.5f, 0.5f, 0.5f, 4f, 0f);
            MariquitaPista(g, 28f, 0.5f, 0.15f, 0.85f, 4f, 0f);
            MariquitaPista(g, 47f, 0.5f, 0.15f, 0.85f, 3.5f, 0.3f);
            MariquitaPista(g, 66f, 0.5f, 0.15f, 0.85f, 3f, 0.6f);
            MariquitaPista(g, 76f, 0.5f, 0.15f, 0.85f, 2.6f, 0.1f);
            MariquitaPista(g, 93f, 0.5f, 0.15f, 0.85f, 2.6f, 0.5f); // donde falta la valla
            MariquitaPista(g, 103f, 0.5f, 0.2f, 0.8f, 3f, 0.8f);
            Trampolin(g, 14f, 0.08f, 33f);
            Trampolin(g, 44f, 0.92f, 63f);
            Trampolin(g, 70f, 0.08f, 89f);
        }

        /// <summary>Mariquita en el metro s de la pista. Si fA ≠ fB, patrulla de lado a lado entre esas dos fracciones del ancho.</summary>
        static MariquitaBumper MariquitaPista(Transform padre, float s, float f, float fA, float fB, float periodo, float desfase) =>
            Mariquita(padre, $"Mariquita m{s:0}", Punto(s, f), -Adelante(s), Punto(s, fA), Punto(s, fB), periodo, desfase);

        /// <summary>Mariquita acolchada que rebota a quien la toca; patrulla entre a y b (si son distintos).</summary>
        static MariquitaBumper Mariquita(Transform padre, string nombre, Vector3 posicion, Vector3 mirar, Vector3 a, Vector3 b, float periodo, float desfase)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.SetPositionAndRotation(posicion, Quaternion.LookRotation(mirar, Vector3.up)); // mira a quien sube
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
            bumper.puntoA = a;
            bumper.puntoB = b;
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
            // Una hoja pequeña pintada donde se aterriza, para que se entienda adónde lleva.
            Kit("HojaRedonda", decorado, Punto(sDestino, f) + Vector3.up * 0.01f, 0f, verdeB, 0.6f);
            return hoja;
        }

        // =====================================================================
        // Sección 2: bombardeo de polen y puentes de hoja (m 110-230)
        // =====================================================================

        static BolasPolen Seccion2()
        {
            var g = M.Grupo(obstaculos, "Seccion 2");
            // Las bolas: salen del cañón en el metro 226 y bajan rodando hasta el 104.
            var camino = new List<Vector3>();
            var fuera = new List<Vector3>();
            var ancho = new List<float>();
            for (float s = 226f; s >= 104f; s -= 2f)
            {
                camino.Add(Punto(s, 0.5f));
                fuera.Add(Fuera(s));
                ancho.Add(Ancho(s));
            }
            var bolasGo = new GameObject("Bolas de polen");
            bolasGo.transform.SetParent(g, false);
            var bolas = bolasGo.AddComponent<BolasPolen>();
            var lista = new List<Rigidbody>();
            for (int i = 0; i < 14; i++)
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
            bolas.cadencia = 2.4f;
            bolas.velocidad = 8f;
            // Dos mariquitas que barren la pista de lado a lado: empujan hacia los carriles de las bolas.
            MariquitaPista(g, 146f, 0.5f, 0.12f, 0.88f, 3.2f, 0f);
            MariquitaPista(g, 186f, 0.5f, 0.12f, 0.88f, 3.2f, 0.5f);
            // El cañón de polen, arriba.
            KitObjetos.Poner("CanonConfeti", decorado, Punto(229f, 0.5f), Angulo(229f) * Mathf.Rad2Deg + 90f, null, 2f, false, KitObjetos.KitPlato);

            // Puentes de hoja: atajos por dentro de la espiral (de una curva a la siguiente).
            PuenteHoja(g, 134f, 160f);
            PuenteHoja(g, 166f, 192f);
            PuenteHoja(g, 198f, 224f);
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
        // Sección 3: el tallo ventoso (m 230-340)
        // =====================================================================

        static void Seccion3()
        {
            var g = M.Grupo(obstaculos, "Seccion 3");
            var zona = new GameObject("Nectar resbaladizo").AddComponent<ZonaResbaladiza>();
            zona.transform.SetParent(g, false);
            zona.alturaDesde = Altura(230f) - 0.5f;
            zona.alturaHasta = Altura(340f) + 1.5f;
            zona.radioDesde = RadioInterior - 0.5f;
            zona.radioHasta = RadioInterior + 9.5f;

            float[] sitios = { 244f, 262f, 276f, 302f, 316f, 334f };
            for (int i = 0; i < sitios.Length; i++) MariquitaPista(g, sitios[i], 0.5f, 0.12f, 0.88f, 2f, i * 0.3f);

            float[] ventiladores = { 255f, 290f, 325f };
            for (int i = 0; i < ventiladores.Length; i++)
            {
                float s = ventiladores[i];
                float giro = Angulo(s) * Mathf.Rad2Deg;
                var ventilador = Kit("Ventilador", g, Punto(s, 0.03f), giro, banderaA, 1.2f);
                var aspas = Kit("AspasVentilador", ventilador.transform, new Vector3(0f, 2.8f, 0.2f), 0f, banderaA, 1f, local: true);
                var viento = new GameObject($"Viento m{s:0}").AddComponent<VientoLateral>();
                viento.transform.SetParent(g, false);
                viento.transform.SetPositionAndRotation(Punto(s, 0f), Quaternion.LookRotation(Fuera(s), Vector3.up));
                viento.tamanoZona = new Vector3(10f, 4f, Ancho(s) + 2f);
                viento.aspas = aspas.transform;
                viento.desfase = i / (float)ventiladores.Length;
                KitObjetos.Poner("CanonConfeti", decorado, Punto(s + 4f, 0.05f), giro, null, 1.2f, false, KitObjetos.KitPlato);
            }
        }

        // =====================================================================
        // Sección 4: teclas de pétalo (m 340-428)
        // =====================================================================

        /// <summary>Un pétalo piano en cada hueco de la pista (dos en los largos), atravesado de lado a lado.</summary>
        static PetaloPiano[][] Seccion4()
        {
            var g = M.Grupo(obstaculos, "Seccion 4");
            var todos = new PetaloPiano[Huecos.Length][];
            int k = 0;
            for (int h = 0; h < Huecos.Length; h++)
            {
                var hueco = Huecos[h];
                int n = Mathf.RoundToInt(hueco.z);
                todos[h] = new PetaloPiano[n];
                for (int j = 0; j < n; j++)
                {
                    float s = Mathf.Lerp(hueco.x, hueco.y, (j + 1f) / (n + 1f));
                    var go = new GameObject($"Petalo m{s:0}");
                    go.transform.SetParent(g, false);
                    go.transform.SetPositionAndRotation(Punto(s, 0.5f) + Vector3.down * 0.15f, Quaternion.LookRotation(Fuera(s), Vector3.up));
                    go.AddComponent<Rigidbody>().isKinematic = true;
                    var caja = go.AddComponent<BoxCollider>();
                    caja.size = new Vector3(2.2f, 0.3f, 4.4f);
                    Kit("Petalo", go.transform, Vector3.zero, 0f, petalo, 1f, local: true);
                    var p = go.AddComponent<PetaloPiano>();
                    p.amplitud = 0.8f;
                    p.periodo = 3f;
                    // En los huecos dobles, los dos pétalos van a contratiempo.
                    p.desfase = j == 0 ? (k * 0.17f) % 1f : (k * 0.17f + 0.5f) % 1f;
                    todos[h][j] = p;
                }
                k++;
            }
            return todos;
        }

        // =====================================================================
        // La cima: la cabeza giratoria y el arco de META
        // =====================================================================

        static (MetaGiratoria meta, PuntoRuta delante, PuntoRuta detras) Cima()
        {
            var g = M.Grupo(obstaculos, "Cima");
            float aFin = Angulo(Largo);

            // La cabeza del girasol: un disco que gira despacio (el girasol busca el sol).
            var cabeza = new GameObject("Cabeza del girasol");
            cabeza.transform.SetParent(g, false);
            cabeza.transform.position = new Vector3(0f, AlturaCima, 0f);
            cabeza.AddComponent<Rigidbody>().isKinematic = true;
            SoloColision(A.Disco(cabeza.transform, "Disco", Vector3.zero, AlturaCima, RadioCabeza, 0.4f, tallo));
            var visual = Kit("CabezaGirasol", cabeza.transform, Vector3.zero, 0f, null, 1f, local: true);
            visual.transform.localScale = Vector3.Scale(visual.transform.localScale, new Vector3(RadioCabeza / 6f, 1f, RadioCabeza / 6f));
            var giratoria = cabeza.AddComponent<PlataformaGiratoria>();
            giratoria.velocidad = 12f;
            giratoria.radio = RadioCabeza;
            giratoria.alturaArriba = 0f;

            // El arco de META en el centro, girando con la cabeza: solo vale cruzarlo por su boca.
            var arco = new GameObject("Arco de META");
            arco.transform.SetParent(cabeza.transform, false);
            arco.transform.rotation = Quaternion.LookRotation(-Fuera(Largo), Vector3.up);
            KitObjetos.Poner("ArcoMeta", arco.transform, Vector3.zero, 0f, oro, 1f, true, KitObjetos.KitPlato);
            var boca = arco.AddComponent<BoxCollider>();
            boca.isTrigger = true;
            boca.center = new Vector3(0f, 2.2f, 0f);
            boca.size = new Vector3(8.2f, 4.4f, 2.6f);
            foreach (float lado in new[] { -1f, 1f })
            {
                var pata = arco.AddComponent<CapsuleCollider>();
                pata.direction = 1;
                pata.center = new Vector3(lado * 4.7f, 2f, 0f);
                pata.height = 4.5f;
                pata.radius = 0.6f;
            }
            var meta = arco.AddComponent<MetaGiratoria>();
            meta.mediaBoca = 4f;
            var delante = PuntoEn(arco.transform, "Delante del arco", new Vector3(0f, 0f, -3.5f));
            var detras = PuntoEn(arco.transform, "Detras del arco", new Vector3(0f, 0f, 3.5f));
            M.Cartel(arco.transform, "META", arco.transform.position + Vector3.up * 6.3f, 2.4f, Color.white);

            // Dos mariquitas guardianas patrullan la cabeza (no giran con ella: hay que esquivarlas).
            foreach (float grados in new[] { 110f, 230f })
            {
                Vector3 d = Direccion(aFin - grados * Mathf.Deg2Rad);
                Vector3 y = Vector3.up * AlturaCima;
                Mariquita(g, $"Mariquita guardiana {grados:0}", d * 8f + y, -d, d * 5.5f + y, d * 12f + y, 3f, grados / 360f);
            }

            // Corona de pétalos alrededor (quietos, de adorno), dejando libre la llegada de la pista.
            var corona = M.Grupo(decorado, "Petalos de la cabeza");
            for (float grados = 15f; grados <= 255f; grados += 16f)
            {
                Vector3 d = Direccion(aFin - grados * Mathf.Deg2Rad);
                var adorno = Kit("Petalo", corona, d * (RadioCabeza + 1.8f) + Vector3.up * (AlturaCima - 0.3f), 0f, petalo, 1.2f);
                adorno.transform.rotation = Quaternion.LookRotation(d, Vector3.up) * Quaternion.Euler(-20f, 0f, 0f) * adorno.transform.rotation;
            }

            // Géiser de polen en el centro (de adorno) y una losa dorada bajo el arco.
            var geiser = M.Particulas("Geiser de polen", arco.transform, arco.transform.position + Vector3.up * 0.1f, new Color(1f, 0.85f, 0.3f), new Color(1f, 0.95f, 0.6f), 30, 2f, 4f, particula);
            var main = geiser.main;
            main.loop = true;
            main.playOnAwake = true;
            main.gravityModifier = -0.05f;
            var emision = geiser.emission;
            emision.rateOverTime = 20f;
            var forma = geiser.shape;
            forma.angle = 8f;
            forma.radius = 0.8f;
            return (meta, delante, detras);
        }

        // =====================================================================
        // Savia, invernadero, público y el abejorro
        // =====================================================================

        static Savia CrearSavia()
        {
            var go = new GameObject("Savia");
            go.transform.SetParent(mapa, false);
            go.transform.position = new Vector3(0f, -1f, 0f);
            M.Decorado(PrimitiveType.Cylinder, "Superficie", go.transform, new Vector3(0f, -0.5f, 0f), new Vector3(150f, 0.5f, 150f), savia, local: true);
            var s = go.AddComponent<Savia>();
            s.velocidad = 0.36f;
            s.retraso = 10f;
            s.alturaMaxima = AlturaCima - 2.5f;
            return s;
        }

        static ParticleSystem[] Invernadero()
        {
            var inv = M.Grupo(decorado, "Invernadero");
            // Suelo: baldosas y un círculo de césped alrededor del tallo.
            M.Decorado(PrimitiveType.Cylinder, "Suelo", inv, new Vector3(0f, -0.25f, 0f), new Vector3(152f, 0.25f, 152f), baldosa);
            M.Decorado(PrimitiveType.Cylinder, "Cesped", inv, new Vector3(0f, -0.2f, 0f), new Vector3(28f, 0.25f, 28f), cesped);
            // Costillas de hierro de la cúpula y tres anillos.
            const float radio = 74f;
            for (int k = 0; k < 12; k++) Kit("ArcoInvernadero", inv, Vector3.zero, k * 15f, null, radio);
            foreach (float h in new[] { 22f, 42f, 60f })
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
                float h = 2f + k * 9f;
                var sitio = Direccion(a) * 54f + Vector3.up * h;
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
                var pie = Direccion((k * 60f + 50f) * Mathf.Deg2Rad) * 62f;
                KitObjetos.Poner("TorreFocos", inv, pie, KitObjetos.GiroMirando(pie, Vector3.up * 35f), null, 3.5f, false, KitObjetos.KitPlato);
            }
            foreach (float grados in new[] { 140f, 260f })
            {
                var sitio = Direccion(grados * Mathf.Deg2Rad) * 60f + Vector3.up * 28f;
                KitObjetos.Poner("Pantalla", inv, sitio, KitObjetos.GiroMirando(sitio, Vector3.zero), null, 1.2f, false, KitObjetos.KitPlato);
                M.Cartel(inv, "BICHO SHOW", sitio + Vector3.up * 5.5f - sitio.normalized * 0.6f, 2.6f, Color.white);
            }
            // Banderolas colgando de la cúpula.
            Material[] telas = { banderaA, banderaB, banderaC };
            for (int k = 0; k < 9; k++)
            {
                var sitio = Direccion((k * 40f + 10f) * Mathf.Deg2Rad) * 48f + Vector3.up * (30f + (k % 3) * 6f);
                Kit("Banderola", inv, sitio, KitObjetos.GiroMirando(sitio, Vector3.zero), telas[k % telas.Length], 1.8f);
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
            go.transform.position = new Vector3(0f, AlturaCima + 8f, 20f);
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
            dron.reposo = new Vector3(0f, AlturaCima + 8f, 0f); // revolotea sobre la cabeza
            dron.ejeTallo = Vector3.zero;
            dron.alaIzquierda = izq;
            dron.alaDerecha = der;
            return dron;
        }

        // =====================================================================
        // Ruta de los bots
        // =====================================================================

        static PuntoRuta RutaBots(PetaloPiano[][] petalos, PuntoRuta delante, PuntoRuta detras)
        {
            // La espiral, cada 7 m: por el centro, salvo en el polen (por el carril de dentro).
            // En los huecos: esperar al pétalo, saltar a él y saltar al otro lado.
            var normal = new List<(float s, PuntoRuta p)>();
            var secuencia = new List<PuntoRuta>();
            int h = 0;
            float s = 6f;
            while (s <= Largo - 2f)
            {
                if (h < Huecos.Length && s > Huecos[h].x - 2.5f)
                {
                    var hueco = Huecos[h];
                    var pets = petalos[h];
                    secuencia.Add(Ruta($"Antes del hueco m{hueco.x:0}", Punto(hueco.x - 0.9f, 0.5f), 0.3f, Giro(hueco.x), saltar: true, esperar: pets[0]));
                    for (int j = 0; j < pets.Length; j++)
                        secuencia.Add(Ruta($"Petalo del hueco m{hueco.x:0}", pets[j].transform.position + Vector3.up * 0.15f, 0.2f, Giro(hueco.x),
                                           saltar: true, esperar: j + 1 < pets.Length ? pets[j + 1] : null));
                    var despues = Ruta($"Despues del hueco m{hueco.y:0}", Punto(hueco.y + 1.2f, 0.5f), 0.6f, Giro(hueco.y));
                    secuencia.Add(despues);
                    normal.Add((hueco.y + 1.2f, despues));
                    s = hueco.y + 5f;
                    h++;
                    continue;
                }
                bool polen = s >= 110f && s < 230f;
                float f = polen ? 0.16f : 0.5f;
                float anchura = s < 110f ? 3f : polen ? 0.7f : s < 340f ? 1.2f : 0.8f;
                var p = Ruta($"m{s:0}", Punto(s, f), anchura, Giro(s));
                secuencia.Add(p);
                normal.Add((s, p));
                s += 7f;
            }
            for (int i = 0; i + 1 < secuencia.Count; i++) Unir(secuencia[i], secuencia[i + 1]);

            // Puentes de hoja: atajo para los hábiles.
            Atajo(normal, 131f, 134f, 160f, 164f);
            Atajo(normal, 163f, 166f, 192f, 196f);
            Atajo(normal, 195f, 198f, 224f, 228f);

            // A la cabeza, y a cruzar el arco por su boca (los dos puntos giran con él).
            var entrada = Ruta("Entrada a la cabeza", Fuera(Largo) * (RadioCabeza - 2.5f) + Vector3.up * AlturaCima, 1.5f, Giro(Largo));
            Unir(secuencia[secuencia.Count - 1], entrada);
            Unir(entrada, delante);
            Unir(delante, detras);
            Unir(detras, delante);
            return secuencia[0];
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
            if (cruce == null || vuelta == null || cruce.siguientes == null || cruce.siguientes.Length == 0) return;
            var entrada = Ruta($"Entrada puente m{sEntrada:0}", Punto(sEntrada, 0.12f), 0.3f, Giro(sEntrada));
            var medio = Ruta($"Puente m{sEntrada:0}", (Punto(sEntrada, 0.1f) + Punto(sSalida, 0.1f)) * 0.5f, 0.3f, null);
            var salida = Ruta($"Salida puente m{sSalida:0}", Punto(sSalida + 1.5f, 0.15f), 0.4f, Giro(sSalida));
            Unir(entrada, medio);
            Unir(medio, salida);
            Unir(salida, vuelta);
            Unir(cruce, entrada, cruce.siguientes[0]);
        }

        static PuntoRuta Ruta(string nombre, Vector3 posicion, float anchura, Quaternion? giro, bool saltar = false, MonoBehaviour esperar = null)
        {
            var go = new GameObject($"Ruta {ruta.Count + 1}: {nombre}");
            go.transform.SetParent(rutaBots, false);
            go.transform.SetPositionAndRotation(posicion, giro ?? Quaternion.identity); // "right" = a lo ancho de la pista
            var p = go.AddComponent<PuntoRuta>();
            p.anchura = anchura;
            p.saltar = saltar;
            p.esperarA = esperar;
            ruta.Add(p);
            return p;
        }

        /// <summary>Punto de ruta pegado a algo que se mueve (gira con él).</summary>
        static PuntoRuta PuntoEn(Transform padre, string nombre, Vector3 local)
        {
            var go = new GameObject($"Ruta {ruta.Count + 1}: {nombre}");
            go.transform.SetParent(padre, false);
            go.transform.localPosition = local;
            var p = go.AddComponent<PuntoRuta>();
            p.anchura = 0.3f;
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
