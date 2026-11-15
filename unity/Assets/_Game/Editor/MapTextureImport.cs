using UnityEditor;

namespace Game.EditorTools
{
    /// <summary>
    /// Import settings of the kingdom map pictures (Art/Resources/Map) and the zone backgrounds (Art/Resources/Backgrounds): no mipmaps (they are drawn at about their own
    /// size), at most 1024 px, compressed. The map is a JPEG of ~300 KB; in a build it takes about as much.
    /// </summary>
    public sealed class MapTextureImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("/Art/Resources/Map/") && !path.Contains("/Art/Resources/Backgrounds/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.mipmapEnabled = false;
            ti.maxTextureSize = 1024;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.crunchedCompression = true;
            ti.compressionQuality = 60;
            ti.alphaIsTransparency = !assetPath.EndsWith(".jpg");
            ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        }
    }
}
