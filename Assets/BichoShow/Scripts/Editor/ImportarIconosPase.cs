using UnityEditor;

namespace BichoShow.Herramientas
{
    /// <summary>
    /// Los iconos del pase (Arte/Blender/crear_iconos_pase.py → Resources/Pase/Iconos) se dibujan en
    /// los menús: como imagen de interfaz, con su transparencia limpia y sin mipmaps.
    /// </summary>
    class ImportarIconosPase : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Pase/Iconos/")) return;
            var importador = (TextureImporter)assetImporter;
            importador.textureType = TextureImporterType.GUI;
            importador.alphaIsTransparency = true;
            importador.mipmapEnabled = false;
            importador.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
