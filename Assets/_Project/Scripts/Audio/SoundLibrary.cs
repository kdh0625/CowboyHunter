using System;
using System.Collections.Generic;
using UnityEngine;

namespace CowboyHunter.Audio
{
    // 소리 이름마다 오디오 파일을 연결해 두는 목록. 비어 있는 칸은 재생하지 않는다.
    // Resources 폴더에 SoundLibrary라는 이름으로 두면 게임 시작 시 자동으로 불러온다.
    [CreateAssetMenu(menuName = "CowboyHunter/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public class SfxEntry
        {
            public SoundId id;
            [Tooltip("여러 개를 넣으면 무작위로 하나를 재생")] public AudioClip[] clips = Array.Empty<AudioClip>();
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("재생할 때마다 음높이를 이만큼 무작위로 흔든다")][Range(0f, 0.3f)] public float pitchJitter = 0.05f;

            public AudioClip PickClip(System.Random rng)
            {
                if (clips == null || clips.Length == 0) return null;
                var clip = clips[rng.Next(clips.Length)];
                return clip;
            }
        }

        [Serializable]
        public class MusicEntry
        {
            public MusicId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.6f;
        }

        public List<SfxEntry> sfx = new();
        public List<MusicEntry> music = new();

        public SfxEntry Find(SoundId id) => sfx.Find(e => e.id == id);
        public MusicEntry Find(MusicId id) => music.Find(e => e.id == id);

        // 소리 이름마다 칸이 하나씩 있도록 채운다. 이미 있는 칸과 연결된 파일은 그대로 둔다.
        public void EnsureAllEntries()
        {
            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
                if (id != SoundId.None && Find(id) == null) sfx.Add(new SfxEntry { id = id });
            foreach (MusicId id in Enum.GetValues(typeof(MusicId)))
                if (id != MusicId.None && Find(id) == null) music.Add(new MusicEntry { id = id });
            sfx.Sort((a, b) => a.id.CompareTo(b.id));
            music.Sort((a, b) => a.id.CompareTo(b.id));
        }

        void OnValidate() => EnsureAllEntries();
    }
}
