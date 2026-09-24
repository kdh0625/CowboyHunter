using UnityEditor;
using UnityEngine;

namespace CowboyHunter.Editor
{
    // Audio 폴더에 처음 들어오는 소리 파일의 가져오기 설정을 정한다. 이후 인스펙터에서 바꾼 값은 유지된다.
    //   Audio/Sfx/   : 짧은 효과음 → 메모리에 풀어 두고(Decompress On Load) 모노로
    //   Audio/Music/ : 긴 배경음   → 디스크에서 흘려 읽기(Streaming)
    public class AudioImportPostprocessor : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetImporter.importSettingsMissing) return;
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;

            if (assetPath.StartsWith("Assets/_Project/Audio/Sfx/"))
            {
                importer.forceToMono = true;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
            }
            else if (assetPath.StartsWith("Assets/_Project/Audio/Music/"))
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
            }
            else return;

            importer.defaultSampleSettings = settings;
        }
    }
}
