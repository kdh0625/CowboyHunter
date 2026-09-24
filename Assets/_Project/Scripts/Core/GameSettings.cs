using System;
using UnityEngine;

namespace CowboyHunter.Core
{
    // 플레이어 설정. 볼륨은 PlayerPrefs에 저장되고, 화면 모드는 Unity가 스스로 기억한다.
    public static class GameSettings
    {
        const string SfxKey = "settings.sfxVolume";
        const string MusicKey = "settings.musicVolume";

        public static event Action Changed;

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 1f);
            set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.7f);
            set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        public static bool Fullscreen
        {
            get => Screen.fullScreenMode != FullScreenMode.Windowed;
            set
            {
                if (value) Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                else Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                Changed?.Invoke();
            }
        }
    }
}
