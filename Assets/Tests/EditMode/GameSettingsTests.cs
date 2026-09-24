using CowboyHunter.Core;
using NUnit.Framework;
using UnityEngine;

namespace CowboyHunter.Tests
{
    public class GameSettingsTests
    {
        float _sfx, _music;

        [SetUp] public void Remember() { _sfx = GameSettings.SfxVolume; _music = GameSettings.MusicVolume; }
        [TearDown] public void Restore() { GameSettings.SfxVolume = _sfx; GameSettings.MusicVolume = _music; }

        [Test]
        public void Volumes_AreClampedTo0And1_AndRaiseChanged()
        {
            int changed = 0;
            void OnChanged() => changed++;
            GameSettings.Changed += OnChanged;
            try
            {
                GameSettings.SfxVolume = 1.5f;
                GameSettings.MusicVolume = -0.2f;
                Assert.AreEqual(1f, GameSettings.SfxVolume);
                Assert.AreEqual(0f, GameSettings.MusicVolume);
                Assert.AreEqual(2, changed);
            }
            finally { GameSettings.Changed -= OnChanged; }
        }
    }
}
