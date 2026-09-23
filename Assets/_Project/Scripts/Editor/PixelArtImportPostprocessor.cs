using UnityEditor;
using UnityEngine;

namespace CowboyHunter.Editor
{
    // Art 폴더에 처음 들어오는 텍스처를 픽셀 아트용으로 자동 설정한다.
    // 최초 임포트 시에만 적용하므로, 이후 인스펙터에서 바꾼 값은 유지된다.
    public class PixelArtImportPostprocessor : AssetPostprocessor
    {
        public const int PixelsPerUnit = 16;
        const string ArtRoot = "Assets/_Project/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot) || !assetImporter.importSettingsMissing) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
