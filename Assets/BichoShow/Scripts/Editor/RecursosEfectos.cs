using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Los materiales de las partículas, guardados en Resources (en el juego compilado Unity solo
    /// incluye los shaders y variantes que usa algún material guardado). Usan las texturas hechas en
    /// Blender (Arte/Blender/crear_texturas_particulas.py → Texturas/Efectos): nubecitas de polvo,
    /// estrellas, destello, onda, chispa (los de Efectos) y confeti, burbuja, gota, polen y cristal
    /// (los de cada mapa). Se comprueban solos al abrir el proyecto o recompilar.
    ///
    /// Menú "BichoShow > Efectos: poner las partículas nuevas en todas las escenas": cambia los
    /// materiales de las partículas de los mapas (confeti, espuma, gotas, sal, polen…) sin rehacerlos.
    /// </summary>
    static class RecursosEfectos
    {
        const string Carpeta = E.Raiz + "/Resources";
        const string CarpetaTexturas = E.Raiz + "/Texturas/Efectos";

        /// <summary>Textura y si brilla (aditivo). El material se llama "Efectos" + textura.</summary>
        static readonly (string textura, bool aditivo)[] Lista =
        {
            ("Nube", false), ("Estrella", false), ("Destello", true), ("Onda", true), ("Chispa", true),
            ("Confeti", false), ("Burbuja", false), ("Gota", false), ("Polen", true), ("Cristal", false),
        };

        [InitializeOnLoadMethod]
        static void Comprobar() => EditorApplication.delayCall += Crear;

        internal static void Crear()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            hecho = false;
            foreach (var (textura, aditivo) in Lista) Preparar(textura, aditivo);
            // El polvo de antes, por si falta la textura nueva.
            GuardarAntiguo(Efectos.RecursoPolvo, Efectos.CrearTexturaCirculo);
            if (hecho) AssetDatabase.SaveAssets();
        }

        static bool hecho; // algún material creado o cambiado (para guardar)

        /// <summary>El material de partículas de esa textura (lo crea o lo pone al día). Null si falta la textura.</summary>
        internal static Material Material(string textura)
        {
            foreach (var (t, aditivo) in Lista)
                if (t == textura) return Preparar(t, aditivo);
            return null;
        }

        static Material Preparar(string textura, bool aditivo)
        {
            string rutaTex = $"{CarpetaTexturas}/{textura}.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex);
            if (tex == null) return null;
            if (AssetImporter.GetAtPath(rutaTex) is TextureImporter imp && (!imp.alphaIsTransparency || imp.wrapMode != TextureWrapMode.Clamp))
            {
                imp.alphaSource = TextureImporterAlphaSource.FromInput;
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.mipmapEnabled = true;
                imp.SaveAndReimport();
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex);
            }
            E.CrearCarpeta(E.Raiz, "Resources");
            string ruta = $"{Carpeta}/Efectos{textura}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = Efectos.CrearMaterialParticulas(tex, aditivo);
                AssetDatabase.CreateAsset(m, ruta);
                hecho = true;
            }
            else if (m.GetTexture("_BaseMap") != tex || (m.GetFloat("_Blend") > 1.5f) != aditivo)
            {
                var nuevo = Efectos.CrearMaterialParticulas(tex, aditivo);
                m.CopyPropertiesFromMaterial(nuevo);
                m.shader = nuevo.shader;
                m.renderQueue = nuevo.renderQueue;
                EditorUtility.SetDirty(m);
                hecho = true;
            }
            return m;
        }

        static void GuardarAntiguo(string nombre, System.Func<int, Texture2D> crearTextura)
        {
            string rutaMaterial = $"{Carpeta}/{nombre}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial) != null) return;
            E.CrearCarpeta(E.Raiz, "Resources");
            var textura = crearTextura(64);
            AssetDatabase.CreateAsset(textura, $"{Carpeta}/{nombre}Textura.asset");
            AssetDatabase.CreateAsset(Efectos.CrearMaterialParticulas(textura), rutaMaterial);
        }

        // ------------------------------------------------------------------
        // Las partículas de los mapas
        // ------------------------------------------------------------------

        /// <summary>
        /// Pone a unas partículas de un mapa su textura nueva según su nombre (confeti, espuma, gotas,
        /// salpicadura, granos de sal, géiser de polen). Devuelve si ha cambiado algo.
        /// </summary>
        internal static bool AplicarA(ParticleSystem ps)
        {
            if (ps == null) return false;
            string n = ps.gameObject.name;
            string textura = n.StartsWith("Confeti") ? "Confeti"
                           : n.StartsWith("Espuma") ? "Burbuja"
                           : n.StartsWith("Gotas") || n.StartsWith("Salpicadura") ? "Gota"
                           : n.StartsWith("Granos") ? "Cristal"
                           : n.StartsWith("Geiser") ? "Polen"
                           : null;
            if (textura == null) return false;
            var m = Material(textura);
            if (m == null) return false;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = m;
            r.renderMode = ParticleSystemRenderMode.Billboard;

            var hojas = ps.textureSheetAnimation;
            hojas.enabled = textura == "Confeti";
            if (hojas.enabled)
            {
                // Cada trocito, una forma al azar (tira, círculo, estrella o serpentina).
                hojas.mode = ParticleSystemAnimationMode.Grid;
                hojas.numTilesX = 2;
                hojas.numTilesY = 2;
                hojas.animation = ParticleSystemAnimationType.WholeSheet;
                hojas.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
                hojas.startFrame = new ParticleSystem.MinMaxCurve(0f, 3.999f);
                hojas.cycleCount = 1;
                var main = ps.main;
                main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            }
            if (textura == "Burbuja")
            {
                var main = ps.main;
                main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
                main.gravityModifier = -0.1f; // las pompas suben un poco
            }
            EditorUtility.SetDirty(ps);
            EditorUtility.SetDirty(r);
            return true;
        }

        [MenuItem("BichoShow/Efectos: poner las partículas nuevas en todas las escenas")]
        static void PonerEnTodas()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string abierta = EditorSceneManager.GetActiveScene().path;
            int total = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { E.CarpetaEscenas }))
            {
                var escena = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid), OpenSceneMode.Single);
                int aqui = 0;
                foreach (var raiz in escena.GetRootGameObjects())
                    foreach (var ps in raiz.GetComponentsInChildren<ParticleSystem>(true))
                        if (AplicarA(ps)) aqui++;
                if (aqui > 0)
                {
                    EditorSceneManager.MarkSceneDirty(escena);
                    EditorSceneManager.SaveScene(escena);
                    total += aqui;
                }
            }
            if (!string.IsNullOrEmpty(abierta)) EditorSceneManager.OpenScene(abierta, OpenSceneMode.Single);
            Debug.Log($"Bicho Show: partículas nuevas puestas en {total} sistemas de partículas.");
        }
    }
}
