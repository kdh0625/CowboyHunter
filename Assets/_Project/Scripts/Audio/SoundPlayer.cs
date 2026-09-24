using CowboyHunter.Core;
using UnityEngine;

namespace CowboyHunter.Audio
{
    // 효과음은 AudioSource 여러 개를 돌려 가며 쓰고, 배경음은 하나를 반복 재생한다.
    public class SoundPlayer : MonoBehaviour
    {
        const int SfxVoices = 8;

        SoundLibrary _library;
        AudioSource[] _sfx;
        AudioSource _music;
        MusicId _currentMusic;
        float _musicBaseVolume;
        int _nextVoice;
        readonly System.Random _rng = new();

        public void Init(SoundLibrary library)
        {
            _library = library;
            _sfx = new AudioSource[SfxVoices];
            for (int i = 0; i < SfxVoices; i++)
            {
                _sfx[i] = gameObject.AddComponent<AudioSource>();
                _sfx[i].playOnAwake = false;
            }
            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            GameSettings.Changed += ApplyVolume;
        }

        void OnDestroy() => GameSettings.Changed -= ApplyVolume;

        void ApplyVolume() => _music.volume = _musicBaseVolume * GameSettings.MusicVolume;

        public void PlaySfx(SoundId id)
        {
            var entry = _library != null ? _library.Find(id) : null;
            var clip = entry?.PickClip(_rng);
            if (clip == null) return;

            var voice = _sfx[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _sfx.Length;
            voice.pitch = 1f + (float)(_rng.NextDouble() * 2 - 1) * entry.pitchJitter;
            voice.PlayOneShot(clip, entry.volume * GameSettings.SfxVolume);
        }

        // 같은 곡이 이미 나오고 있으면 처음부터 다시 틀지 않는다.
        public void PlayMusic(MusicId id)
        {
            if (id == _currentMusic && _music.isPlaying) return;
            _currentMusic = id;
            var entry = _library != null ? _library.Find(id) : null;
            if (entry == null || entry.clip == null)
            {
                _music.Stop();
                return;
            }
            _music.clip = entry.clip;
            _musicBaseVolume = entry.volume;
            ApplyVolume();
            _music.Play();
        }
    }
}
