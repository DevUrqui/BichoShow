using UnityEditor;
using UnityEngine;
using E = BichoShow.Herramientas.CreadorEscenaPrueba;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Guarda en Resources los materiales de las partículas de Efectos (polvo y estrellitas).
    /// En el editor funcionarían igual creados al vuelo, pero en el juego compilado Unity solo
    /// incluye los shaders (y sus variantes transparentes) que usa algún material guardado.
    /// Se ejecuta solo al abrir el proyecto o recompilar; no hace nada si ya existen.
    /// </summary>
    static class RecursosEfectos
    {
        const string Carpeta = E.Raiz + "/Resources";

        [InitializeOnLoadMethod]
        static void Comprobar() => EditorApplication.delayCall += Crear;

        internal static void Crear()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Guardar(Efectos.RecursoPolvo, Efectos.CrearTexturaCirculo);
            Guardar(Efectos.RecursoEstrella, Efectos.CrearTexturaEstrella);
        }

        static void Guardar(string nombre, System.Func<int, Texture2D> crearTextura)
        {
            string rutaMaterial = $"{Carpeta}/{nombre}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial) != null) return;

            E.CrearCarpeta(E.Raiz, "Resources");
            var textura = crearTextura(64);
            AssetDatabase.CreateAsset(textura, $"{Carpeta}/{nombre}Textura.asset");
            var material = Efectos.CrearMaterialParticulas(textura);
            AssetDatabase.CreateAsset(material, rutaMaterial);
            AssetDatabase.SaveAssets();
            Debug.Log($"Bicho Show: creado {rutaMaterial} (partículas de efectos).");
        }
    }
}
