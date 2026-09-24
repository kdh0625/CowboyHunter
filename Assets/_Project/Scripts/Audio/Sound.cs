using UnityEngine;

namespace CowboyHunter.Audio
{
    // 어디서든 Sound.Play(SoundId.Shot) 한 줄로 소리를 낸다.
    // 파일이 연결되지 않은 소리는 조용히 넘어간다.
    public static class Sound
    {
        public const string LibraryResourcePath = "SoundLibrary";

        static SoundPlayer _player;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreatePlayer()
        {
            var library = Resources.Load<SoundLibrary>(LibraryResourcePath);
            var go = new GameObject("SoundPlayer");
            Object.DontDestroyOnLoad(go);
            _player = go.AddComponent<SoundPlayer>();
            _player.Init(library);
        }

        public static void Play(SoundId id)
        {
            if (_player != null) _player.PlaySfx(id);
        }

        public static void PlayMusic(MusicId id)
        {
            if (_player != null) _player.PlayMusic(id);
        }
    }
}
