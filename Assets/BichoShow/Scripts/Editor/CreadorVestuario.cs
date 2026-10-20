using UnityEditor;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Menú "BichoShow > Personaje: preparar vestuario" (también lo hace "aplicar Bichete").
    /// Convierte cada pieza del kit de vestuario (Arte/Blender/crear_vestuario.py →
    /// Modelos/KitVestuario.fbx) en un prefab de Resources/Vestuario, con materiales de URP,
    /// para que el juego pueda ponerlas en tiempo real. El gorro con hélice lleva sus aspas
    /// aparte ("HeliceAspas") para que giren.
    /// </summary>
    public static class CreadorVestuario
    {
        const string Kit = E.Raiz + "/Modelos/KitVestuario.fbx";
        const string Carpeta = E.Raiz + "/Resources/Vestuario";

        [MenuItem("BichoShow/Personaje: preparar vestuario")]
        public static void Preparar()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Kit) == null)
            {
                Debug.LogWarning($"Bicho Show: no encuentro {Kit}. Genéralo con Arte/Blender/crear_vestuario.py.");
                return;
            }
            E.CrearCarpeta(E.Raiz, "Resources");
            E.CrearCarpeta(E.Raiz + "/Resources", "Vestuario");

            int hechas = 0;
            foreach (var prenda in Vestuario.Gorros) if (Crear(prenda)) hechas++;
            foreach (var prenda in Vestuario.Complementos) if (Crear(prenda)) hechas++;
            AssetDatabase.SaveAssets();
            Debug.Log($"Bicho Show: vestuario preparado ({hechas} prendas en {Carpeta}).");
        }

        static bool Crear(string prenda)
        {
            if (string.IsNullOrEmpty(prenda)) return false;
            var raiz = new GameObject(prenda);
            try
            {
                KitObjetos.Poner(prenda, raiz.transform, Vector3.zero, local: true, kit: Kit);
                if (prenda == "GorroHelice")
                {
                    // Las aspas, en lo alto del eje, en su propio objeto (el juego las hace girar).
                    var aspas = new GameObject("HeliceAspas").transform;
                    aspas.SetParent(raiz.transform, false);
                    aspas.localPosition = new Vector3(0f, 0.31f, 0f);
                    KitObjetos.Poner("HeliceAspas", aspas, Vector3.zero, local: true, kit: Kit);
                }
                foreach (var r in raiz.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                PrefabUtility.SaveAsPrefabAsset(raiz, $"{Carpeta}/{prenda}.prefab");
                return true;
            }
            finally
            {
                Object.DestroyImmediate(raiz);
            }
        }
    }
}
