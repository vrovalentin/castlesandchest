using System.Linq;
using UnityEditor;

namespace Game.EditorTools
{
    /// <summary>
    /// Building pictures (Art/Resources/Castles): the step layers, blueprints and finished pictures are ~90% of a build.
    /// Crunched compression makes them 2-4 times smaller in the WebGL download and the APK; quality 70 keeps the edges clean.
    /// </summary>
    public sealed class CastleTextureImport : AssetPostprocessor
    {
        // The buildings, and the storage icons, interface and board pictures (a third of the download before).
        private static readonly string[] Folders =
            { "/Art/Resources/Castles/", "/Art/Resources/Parts/", "/Art/Resources/UI/", "/Art/Resources/Board/" };

        public override uint GetVersion() => 2;

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!Folders.Any(path.Contains)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.crunchedCompression = true;
            ti.compressionQuality = 70;
        }
    }
}
