using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Cambia los cubos de Unity (aristas duras) por bloques con los bordes redondeados, del
    /// mismo estilo "de plastilina" que el personaje. El objeto conserva su escala (así no
    /// cambia nada del juego: colisiones, tamaños, scripts): la malla se genera ya pensada para
    /// esa escala, con un bisel del mismo grosor en todas las direcciones.
    /// Las UV van en metros (1 repetición por metro) para poder poner texturas uniformes.
    ///
    /// Menú "BichoShow > Ambiente: redondear los bloques de todas las escenas"; los
    /// generadores de mapas lo aplican solos al crear piezas.
    /// </summary>
    public static class MallasRedondeadas
    {
        const string Carpeta = E.Raiz + "/Modelos/Bloques";
        const float RadioMaximo = 0.14f;
        const int Segmentos = 3;

        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        // ------------------------------------------------------------------
        // Menú
        // ------------------------------------------------------------------

        [MenuItem("BichoShow/Ambiente: redondear los bloques de todas las escenas")]
        public static void RedondearTodas()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string abierta = EditorSceneManager.GetActiveScene().path;

            int total = 0;
            var escenas = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { E.CarpetaEscenas }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                int n = 0;
                foreach (var raiz in escena.GetRootGameObjects())
                    foreach (var filtro in raiz.GetComponentsInChildren<MeshFilter>(true))
                        if (Redondear(filtro.gameObject)) n++;
                if (n > 0) EditorSceneManager.SaveScene(escena);
                total += n;
                escenas.Add($"{escena.name} ({n})");
            }

            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(abierta)) EditorSceneManager.OpenScene(abierta, OpenSceneMode.Single);
            Debug.Log($"Bicho Show: {total} bloques redondeados en {string.Join(", ", escenas)}.");
        }

        // ------------------------------------------------------------------
        // API para los generadores
        // ------------------------------------------------------------------

        /// <summary>Si el objeto usa el cubo de Unity, le pone la malla redondeada para su escala. Devuelve si lo cambió.</summary>
        public static bool Redondear(GameObject go)
        {
            if (go == null || !go.TryGetComponent(out MeshFilter filtro) || !EsCuboDeUnity(filtro.sharedMesh)) return false;

            Vector3 escala = go.transform.lossyScale;
            escala = new Vector3(Mathf.Abs(escala.x), Mathf.Abs(escala.y), Mathf.Abs(escala.z));
            if (escala.x < 1e-4f || escala.y < 1e-4f || escala.z < 1e-4f) return false;

            filtro.sharedMesh = MallaPara(escala);
            EditorUtility.SetDirty(filtro);
            return true;
        }

        static bool EsCuboDeUnity(Mesh malla) =>
            malla != null && malla.name == "Cube" && AssetDatabase.GetAssetPath(malla) == "Library/unity default resources";

        /// <summary>Malla (guardada como asset y reutilizada) para un bloque de este tamaño.</summary>
        static Mesh MallaPara(Vector3 tamano)
        {
            // Redondear el tamaño a centímetros para reutilizar mallas.
            Vector3 t = new Vector3(Mathf.Round(tamano.x * 100f) / 100f, Mathf.Round(tamano.y * 100f) / 100f, Mathf.Round(tamano.z * 100f) / 100f);
            t = Vector3.Max(t, Vector3.one * 0.01f);
            string clave = $"B_{t.x:0.00}x{t.y:0.00}x{t.z:0.00}".Replace(',', '.');
            if (Cache.TryGetValue(clave, out var enCache) && enCache != null) return enCache;

            E.CrearCarpeta(E.Raiz, "Modelos");
            E.CrearCarpeta(E.Raiz + "/Modelos", "Bloques");
            string ruta = $"{Carpeta}/{clave}.asset";
            var malla = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (malla == null)
            {
                malla = Generar(t);
                malla.name = clave;
                AssetDatabase.CreateAsset(malla, ruta);
            }
            Cache[clave] = malla;
            return malla;
        }

        // ------------------------------------------------------------------
        // Generación
        // ------------------------------------------------------------------

        /// <summary>
        /// Caja redondeada de un tamaño dado, expresada en el espacio de un cubo de 1x1x1 escalado
        /// a ese tamaño (para que el objeto conserve su escala).
        /// Cada cara es una rejilla que en los bordes se curva sobre una esquina de radio r.
        /// </summary>
        static Mesh Generar(Vector3 tamano)
        {
            Vector3 medio = tamano * 0.5f;
            float r = Mathf.Min(RadioMaximo, 0.4f * Mathf.Min(tamano.x, Mathf.Min(tamano.y, tamano.z)));
            Vector3 interior = medio - Vector3.one * r;

            // Coordenadas de la rejilla en cada eje: la esquina curva (Segmentos tramos) a cada lado.
            float[] Coordenadas(float h)
            {
                var c = new float[(Segmentos + 1) * 2];
                for (int i = 0; i <= Segmentos; i++)
                {
                    c[i] = -h + r * i / Segmentos;
                    c[Segmentos + 1 + i] = h - r + r * i / Segmentos;
                }
                return c;
            }

            var vertices = new List<Vector3>();
            var normales = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangulos = new List<int>();

            // (eje normal, signo, eje u, eje v)
            int[,] caras = { { 0, 1, 2, 1 }, { 0, -1, 2, 1 }, { 1, 1, 0, 2 }, { 1, -1, 0, 2 }, { 2, 1, 0, 1 }, { 2, -1, 0, 1 } };
            for (int f = 0; f < 6; f++)
            {
                int ejeN = caras[f, 0], signo = caras[f, 1], ejeU = caras[f, 2], ejeV = caras[f, 3];
                float[] cu = Coordenadas(medio[ejeU]);
                float[] cv = Coordenadas(medio[ejeV]);
                int n = cu.Length;
                int baseIndice = vertices.Count;

                for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = Vector3.zero;
                    p[ejeN] = signo * medio[ejeN];
                    p[ejeU] = cu[i];
                    p[ejeV] = cv[j];

                    // Proyectar sobre la caja redondeada: punto más cercano de la caja interior + r en esa dirección.
                    Vector3 dentro = new Vector3(
                        Mathf.Clamp(p.x, -interior.x, interior.x),
                        Mathf.Clamp(p.y, -interior.y, interior.y),
                        Mathf.Clamp(p.z, -interior.z, interior.z));
                    Vector3 dir = p - dentro;
                    if (dir.sqrMagnitude < 1e-10f) { dir = Vector3.zero; dir[ejeN] = signo; }
                    dir.Normalize();
                    Vector3 superficie = dentro + dir * r;

                    // A espacio de cubo unidad escalado: posiciones / tamaño; normales * tamaño.
                    vertices.Add(new Vector3(superficie.x / tamano.x, superficie.y / tamano.y, superficie.z / tamano.z));
                    normales.Add(new Vector3(dir.x * tamano.x, dir.y * tamano.y, dir.z * tamano.z).normalized);
                    uvs.Add(new Vector2(superficie[ejeU] + medio[ejeU], superficie[ejeV] + medio[ejeV]));
                }

                for (int j = 0; j < n - 1; j++)
                for (int i = 0; i < n - 1; i++)
                {
                    int a = baseIndice + j * n + i, b = a + 1, c = a + n, d = c + 1;
                    Triangulo(triangulos, vertices, normales, a, c, b);
                    Triangulo(triangulos, vertices, normales, b, c, d);
                }
            }

            var malla = new Mesh();
            malla.SetVertices(vertices);
            malla.SetNormals(normales);
            malla.SetUVs(0, uvs);
            malla.SetTriangles(triangulos, 0);
            malla.RecalculateBounds();
            malla.RecalculateTangents();
            return malla;
        }

        /// <summary>Añade el triángulo con el orden que hace que mire hacia fuera.</summary>
        static void Triangulo(List<int> triangulos, List<Vector3> vertices, List<Vector3> normales, int a, int b, int c)
        {
            Vector3 n = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            Vector3 fuera = normales[a] + normales[b] + normales[c]; // en los bordes curvos, la normal de la cara no vale
            if (n.sqrMagnitude < 1e-14f) return; // degenerado (esquinas que colapsan)
            if (Vector3.Dot(n, fuera) < 0f) { triangulos.Add(a); triangulos.Add(c); triangulos.Add(b); }
            else { triangulos.Add(a); triangulos.Add(b); triangulos.Add(c); }
        }
    }
}
