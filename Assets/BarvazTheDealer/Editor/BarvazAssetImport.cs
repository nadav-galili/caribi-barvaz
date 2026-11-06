using UnityEditor;
using UnityEngine;

namespace BarvazTheDealer.EditorTools
{
    /// <summary>
    /// Import settings for the game's art and audio: crisp UI sprites (no mipmaps, no compression),
    /// 9-slice borders on frames, and streaming for the music.
    /// </summary>
    public class BarvazAssetImport : AssetPostprocessor
    {
        private const string Root = "Assets/BarvazTheDealer/Resources/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 2048;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.npotScale = TextureImporterNPOTScale.None;

            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (file.StartsWith("btn_")) ti.spriteBorder = new Vector4(48, 40, 48, 44);
            else if (file == "panel") ti.spriteBorder = new Vector4(36, 36, 36, 36);
            else if (file == "bubble") ti.spriteBorder = new Vector4(44, 44, 44, 44);
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root)) return;
            var ai = (AudioImporter)assetImporter;
            var settings = ai.defaultSampleSettings;
            if (assetPath.Contains("music"))
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
            }
            ai.defaultSampleSettings = settings;
        }

        [MenuItem("Barvaz/Reimport Game Art")]
        private static void Reimport()
        {
            AssetDatabase.ImportAsset("Assets/BarvazTheDealer/Resources", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }
    }
}
