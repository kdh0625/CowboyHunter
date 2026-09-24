using System;
using System.Linq;
using CowboyHunter.Audio;
using NUnit.Framework;
using UnityEngine;

namespace CowboyHunter.Tests
{
    public class SoundLibraryTests
    {
        [Test]
        public void EnsureAllEntries_OneSlotPerName_KeepsExisting()
        {
            var lib = ScriptableObject.CreateInstance<SoundLibrary>();
            var clip = AudioClip.Create("shot", 100, 1, 44100, false);
            lib.sfx.Add(new SoundLibrary.SfxEntry { id = SoundId.Shot, clips = new[] { clip } });

            lib.EnsureAllEntries();
            lib.EnsureAllEntries();   // 두 번 불러도 중복되지 않음

            int sfxNames = Enum.GetValues(typeof(SoundId)).Length - 1;
            int musicNames = Enum.GetValues(typeof(MusicId)).Length - 1;
            Assert.AreEqual(sfxNames, lib.sfx.Count);
            Assert.AreEqual(musicNames, lib.music.Count);
            Assert.AreEqual(sfxNames, lib.sfx.Select(e => e.id).Distinct().Count());
            Assert.AreSame(clip, lib.Find(SoundId.Shot).clips[0]);
        }

        [Test]
        public void PickClip_EmptySlotGivesNull_OtherwiseOneOfClips()
        {
            var rng = new System.Random(1);
            var empty = new SoundLibrary.SfxEntry { id = SoundId.Hit };
            Assert.IsNull(empty.PickClip(rng));

            var a = AudioClip.Create("a", 100, 1, 44100, false);
            var b = AudioClip.Create("b", 100, 1, 44100, false);
            var entry = new SoundLibrary.SfxEntry { id = SoundId.Hit, clips = new[] { a, b } };
            var picked = Enumerable.Range(0, 50).Select(_ => entry.PickClip(rng)).Distinct().ToList();
            CollectionAssert.AreEquivalent(new[] { a, b }, picked);
        }
    }
}
