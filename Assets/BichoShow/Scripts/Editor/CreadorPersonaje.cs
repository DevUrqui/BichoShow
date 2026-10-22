using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menú "BichoShow > Personaje: aplicar Triangulín al bicho".
    /// Cambia el aspecto del prefab del bicho por el modelo hecho en Blender
    /// (Arte/Blender/crear_triangulin.py → Assets/BichoShow/Modelos/Triangulin.fbx),
    /// añade brazos y piernas "de fideo" y el animador procedural. No toca los ajustes de
    /// física ni de control del prefab.
    /// </summary>
    public static class CreadorPersonaje
    {
        const string RutaModelo = E.Raiz + "/Modelos/Triangulin.fbx";

        [MenuItem("BichoShow/Personaje: aplicar Triangulín al bicho")]
        public static void Aplicar()
        {
            if (!PrepararImportacion()) return;
            var modeloAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelo);
            if (modeloAsset == null)
            {
                EditorUtility.DisplayDialog("Bicho Show", $"No encuentro el modelo en:\n{RutaModelo}\n\nGenera el modelo con el script de Blender.", "Vale");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(E.RutaPrefabBicho) == null)
            {
                EditorUtility.DisplayDialog("Bicho Show", "No encuentro el prefab del bicho. Crea antes alguna escena desde el menú BichoShow.", "Vale");
                return;
            }

            E.CrearCarpeta(E.Raiz, "Materiales");
            var piel = E.CrearMaterial("Piel", new Color(0.88f, 0.30f, 0.24f), 0.25f);
            var ojo = E.CrearMaterial("OjoTriangulin", new Color(0.97f, 0.94f, 0.88f), 0.65f);
            var pupila = E.CrearMaterial("PupilaTriangulin", new Color(0.06f, 0.04f, 0.04f), 0.85f);
            var brillo = E.CrearMaterial("Brillo", Color.white, 0.9f);
            var boca = E.CrearMaterial("BocaTriangulin", new Color(0.32f, 0.08f, 0.06f), 0.4f);
            var bota = E.CrearMaterial("Bota", new Color(0.06f, 0.42f, 0.42f), 0.4f);
            var suela = E.CrearMaterial("Suela", new Color(0.04f, 0.28f, 0.28f), 0.2f);

            var raiz = PrefabUtility.LoadPrefabContents(E.RutaPrefabBicho);
            try
            {
                // Quitar el aspecto anterior (el bicho de cápsulas o una versión vieja de este).
                foreach (var nombre in new[] { "Cuerpo", "OrejaIzq", "OrejaDer", "OjoIzq", "OjoDer", "Aspecto" })
                {
                    var viejo = raiz.transform.Find(nombre);
                    if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
                }

                var aspecto = new GameObject("Aspecto").transform;
                aspecto.SetParent(raiz.transform, false);

                var modelo = (GameObject)Object.Instantiate(modeloAsset, aspecto);
                modelo.name = "Modelo";
                modelo.transform.localPosition = Vector3.zero;
                modelo.transform.localRotation = Quaternion.identity;
                Orientar(modelo.transform, raiz.transform);

                // Torso (cuerpo y cara), que rebota al andar; grupos de ojos para parpadear.
                var torso = new GameObject("Torso").transform;
                torso.SetParent(aspecto, false);
                foreach (var nombre in new[] { "Cuerpo", "ParpadoIzq", "ParpadoDer", "Boca" })
                    Mover(modelo.transform, nombre, torso);

                var gruposOjo = new List<Transform>();
                foreach (var lado in new[] { "Izq", "Der" })
                {
                    var blanco = Buscar(modelo.transform, $"Ojo{lado}");
                    if (blanco == null) continue;
                    var grupo = new GameObject($"Ojo{lado}Grupo").transform;
                    grupo.SetParent(torso, false);
                    grupo.position = blanco.position;
                    foreach (var parte in new[] { $"Ojo{lado}", $"Pupila{lado}", $"Brillo{lado}" })
                        Mover(modelo.transform, parte, grupo);
                    gruposOjo.Add(grupo);
                }

                // Manos y botas: sueltas, las mueve el animador.
                var manos = new List<Transform> { Mover(modelo.transform, "ManoIzq", aspecto), Mover(modelo.transform, "ManoDer", aspecto) };
                var botas = new List<Transform> { Mover(modelo.transform, "BotaIzq", aspecto), Mover(modelo.transform, "BotaDer", aspecto) };
                manos.RemoveAll(t => t == null);
                botas.RemoveAll(t => t == null);
                // Izquierda = la de X negativa (por si la exportación las ha cruzado).
                manos.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));
                botas.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));

                if (modelo.transform.childCount == 0) Object.DestroyImmediate(modelo);

                // Materiales (URP) por nombre de pieza
                foreach (var r in aspecto.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string n = r.gameObject.name;
                    if (n.StartsWith("Ojo")) r.sharedMaterial = ojo;
                    else if (n.StartsWith("Pupila")) r.sharedMaterial = pupila;
                    else if (n.StartsWith("Brillo")) r.sharedMaterial = brillo;
                    else if (n.StartsWith("Boca")) r.sharedMaterial = boca;
                    else if (n.StartsWith("Bota"))
                    {
                        var filtro = r.GetComponent<MeshFilter>();
                        int submallas = filtro != null && filtro.sharedMesh != null ? filtro.sharedMesh.subMeshCount : 1;
                        r.sharedMaterials = submallas >= 2 ? new[] { bota, suela } : new[] { bota };
                    }
                    else r.sharedMaterial = piel; // cuerpo, párpados, manos
                }

                // Brazos y piernas de fideo (la malla la genera el animador en tiempo real)
                MeshFilter Fideo(string nombre)
                {
                    var go = new GameObject(nombre);
                    go.transform.SetParent(aspecto, false);
                    var filtro = go.AddComponent<MeshFilter>();
                    go.AddComponent<MeshRenderer>().sharedMaterial = piel;
                    return filtro;
                }
                var brazoIzq = Fideo("BrazoIzq");
                var brazoDer = Fideo("BrazoDer");
                var piernaIzq = Fideo("PiernaIzq");
                var piernaDer = Fideo("PiernaDer");

                // Física: cápsula a la medida del nuevo cuerpo (1.5 m de alto).
                var capsula = raiz.GetComponent<CapsuleCollider>();
                if (capsula != null)
                {
                    capsula.radius = 0.45f;
                    capsula.height = 1.4f;
                    capsula.center = new Vector3(0f, 0.7f, 0f);
                }

                // Color de equipo: cuerpo, párpados, manos, brazos y piernas.
                var controlador = raiz.GetComponent<ControladorBicho>();
                var tenibles = new List<Renderer>();
                foreach (var r in aspecto.GetComponentsInChildren<Renderer>(true))
                    if (r.sharedMaterial == piel) tenibles.Add(r);
                controlador.renderersColor = tenibles.ToArray();
                controlador.manoSimple = false;

                if (!raiz.TryGetComponent(out AspectoBicho animador)) animador = raiz.AddComponent<AspectoBicho>();
                animador.torso = torso;
                animador.manoIzq = manos.Count > 0 ? manos[0] : null;
                animador.manoDer = manos.Count > 1 ? manos[1] : null;
                animador.pieIzq = botas.Count > 0 ? botas[0] : null;
                animador.pieDer = botas.Count > 1 ? botas[1] : null;
                animador.ojos = gruposOjo.ToArray();
                animador.brazoIzq = brazoIzq;
                animador.brazoDer = brazoDer;
                animador.piernaIzq = piernaIzq;
                animador.piernaDer = piernaDer;

                PrefabUtility.SaveAsPrefabAsset(raiz, E.RutaPrefabBicho);
                Debug.Log("Bicho Show: Triangulín aplicado al prefab del bicho. Dale a Play en cualquier escena.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        /// <summary>Sin materiales ni animaciones del FBX (usamos los nuestros) y escala 1:1.</summary>
        static bool PrepararImportacion()
        {
            var importador = AssetImporter.GetAtPath(RutaModelo) as ModelImporter;
            if (importador == null)
            {
                EditorUtility.DisplayDialog("Bicho Show",
                    $"No encuentro el modelo en:\n{RutaModelo}\n\nGenéralo con Arte/Blender/crear_triangulin.py.", "Vale");
                return false;
            }

            bool cambios = false;
            if (importador.materialImportMode != ModelImporterMaterialImportMode.None) { importador.materialImportMode = ModelImporterMaterialImportMode.None; cambios = true; }
            if (importador.importAnimation) { importador.importAnimation = false; cambios = true; }
            if (importador.animationType != ModelImporterAnimationType.None) { importador.animationType = ModelImporterAnimationType.None; cambios = true; }
            if (importador.importCameras || importador.importLights) { importador.importCameras = false; importador.importLights = false; cambios = true; }
            if (cambios) importador.SaveAndReimport();
            return true;
        }

        /// <summary>
        /// Deja el modelo de pie, a escala 1, mirando a +Z (los ojos delante) y con los pies en
        /// el suelo, pase lo que pase con los ejes al exportar desde Blender. Con puedeEstarTumbado, si
        /// es más profundo que alto se pone de pie (para exportaciones viejas con Z arriba; no vale para
        /// bichos largos de verdad, como Zumbi con su abdomen).
        /// </summary>
        internal static void Orientar(Transform modelo, Transform raiz, bool puedeEstarTumbado = true)
        {
            Bounds b = Limites(modelo, raiz);
            if (b.size.y > 20f) modelo.localScale *= 0.01f;           // exportado en centímetros
            b = Limites(modelo, raiz);
            if (puedeEstarTumbado && b.size.z > b.size.y * 1.2f)      // tumbado (Z arriba)
                modelo.localRotation = Quaternion.Euler(-90f, 0f, 0f) * modelo.localRotation;

            var cuerpo = Buscar(modelo, "Cuerpo");
            var unOjo = Buscar(modelo, "OjoIzq") ?? Buscar(modelo, "OjoDer");
            if (cuerpo != null && unOjo != null)
            {
                Vector3 centroCuerpo = raiz.InverseTransformPoint(CentroRender(cuerpo));
                Vector3 centroOjo = raiz.InverseTransformPoint(CentroRender(unOjo));
                if (centroOjo.z < centroCuerpo.z)                      // mira hacia atrás
                    modelo.localRotation = Quaternion.Euler(0f, 180f, 0f) * modelo.localRotation;
            }

            b = Limites(modelo, raiz);
            modelo.localPosition += new Vector3(-b.center.x, -b.min.y, 0f);
        }

        static Bounds Limites(Transform modelo, Transform raiz)
        {
            var renderers = modelo.GetComponentsInChildren<Renderer>(true);
            var b = new Bounds(raiz.InverseTransformPoint(modelo.position), Vector3.zero);
            bool primero = true;
            foreach (var r in renderers)
            {
                foreach (var esquina in Esquinas(r.bounds))
                {
                    Vector3 p = raiz.InverseTransformPoint(esquina);
                    if (primero) { b = new Bounds(p, Vector3.zero); primero = false; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        static IEnumerable<Vector3> Esquinas(Bounds b)
        {
            for (int i = 0; i < 8; i++)
                yield return new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z);
        }

        static Vector3 CentroRender(Transform t)
        {
            var r = t.GetComponent<Renderer>();
            return r != null ? r.bounds.center : t.position;
        }

        internal static Transform Buscar(Transform raiz, string nombre)
        {
            foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
                if (t.name == nombre) return t;
            return null;
        }

        static Transform Mover(Transform modelo, string nombre, Transform nuevoPadre)
        {
            var t = Buscar(modelo, nombre);
            if (t != null) t.SetParent(nuevoPadre, true);
            return t;
        }
    }
}
