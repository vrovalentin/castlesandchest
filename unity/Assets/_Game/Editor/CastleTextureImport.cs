using UnityEditor;

namespace Game.EditorTools
{
    /// <summary>
    /// Building pictures (Art/Resources/Castles): the step layers, blueprints and finished pictures are ~90% of a build.
    /// Crunched compression makes them 2-4 times smaller in the WebGL download and the APK; quality 70 keeps the edges clean.
    /// </summary>
    public sealed class CastleTextureImport : AssetPostprocessor
    {
        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("/Art/Resources/Castles/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.crunchedCompression = true;
            ti.compressionQuality = 70;
        }
    }
}
