using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Coloca piezas de los kits hechos en Blender (Arte/Blender/crear_kit_*.py → Modelos/Kit*.fbx).
    /// Cada pieza se busca por nombre dentro del FBX; sus materiales se cambian por materiales
    /// de URP (en Materiales/Kit) con el mismo color que en Blender. "Equipo" se cambia por el
    /// material de equipo que se le pase.
    /// </summary>
    public static class KitObjetos
    {
        public const string KitBarrio = E.Raiz + "/Modelos/KitBarrio.fbx";
        const string CarpetaMateriales = E.CarpetaMateriales + "/Kit";

        static readonly HashSet<string> Emisivos = new HashSet<string> { "Faro", "Luz" };
        static readonly HashSet<string> Brillantes = new HashSet<string> { "Cristal", "Ventana", "Pantalla", "Metal", "Llanta" };
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        /// <summary>
        /// Pone una pieza del kit. La posición es la de su base (el origen de la pieza en Blender).
        /// </summary>
        public static GameObject Poner(string pieza, Transform padre, Vector3 posicion, float giroY = 0f,
                                       Material equipo = null, float escala = 1f, bool local = false, string kit = KitBarrio)
        {
            PrepararImportacion(kit);
            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(kit);
            var original = modelo != null ? modelo.transform.Find(pieza) : null;
            if (original == null)
            {
                Debug.LogError($"Bicho Show: no encuentro la pieza '{pieza}' en {kit}. Genera el kit con su script de Blender.");
                return new GameObject(pieza);
            }

            var go = (GameObject)Object.Instantiate(original.gameObject, padre);
            go.name = pieza;
            if (local)
            {
                go.transform.localPosition = posicion;
                go.transform.localRotation = Quaternion.Euler(0f, giroY, 0f) * original.localRotation;
            }
            else
            {
                go.transform.position = posicion;
                go.transform.rotation = Quaternion.Euler(0f, giroY, 0f) * original.localRotation;
            }
            go.transform.localScale = original.localScale * escala;

            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var materiales = r.sharedMaterials;
                for (int i = 0; i < materiales.Length; i++)
                    materiales[i] = MaterialPara(materiales[i], equipo);
                r.sharedMaterials = materiales;
            }
            return go;
        }

        static Material MaterialPara(Material importado, Material equipo)
        {
            if (importado == null) return null;
            string nombre = importado.name.Replace(" (Instance)", "");
            if (nombre == "Equipo" && equipo != null) return equipo;
            if (Cache.TryGetValue(nombre, out var enCache) && enCache != null) return enCache;

            E.CrearCarpeta(E.CarpetaMateriales, "Kit");
            string ruta = $"{CarpetaMateriales}/Kit{nombre}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (mat == null)
            {
                Color color = importado.HasProperty("_BaseColor") ? importado.GetColor("_BaseColor")
                            : importado.HasProperty("_Color") ? importado.color : Color.white;
                color.a = 1f;
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Smoothness", Brillantes.Contains(nombre) ? 0.75f : 0.2f);
                if (Emisivos.Contains(nombre))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    mat.SetColor("_EmissionColor", color * 2.5f);
                }
                AssetDatabase.CreateAsset(mat, ruta);
            }
            Cache[nombre] = mat;
            return mat;
        }

        /// <summary>Que el FBX traiga sus materiales (para leer los colores) y nada de animaciones.</summary>
        static void PrepararImportacion(string kit)
        {
            var importador = AssetImporter.GetAtPath(kit) as ModelImporter;
            if (importador == null) return;
            bool cambios = false;
            if (importador.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                importador.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                cambios = true;
            }
            if (importador.importAnimation) { importador.importAnimation = false; cambios = true; }
            if (importador.animationType != ModelImporterAnimationType.None) { importador.animationType = ModelImporterAnimationType.None; cambios = true; }
            if (importador.importCameras || importador.importLights) { importador.importCameras = false; importador.importLights = false; cambios = true; }
            if (cambios) importador.SaveAndReimport();
        }
    }
}
