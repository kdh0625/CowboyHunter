using CowboyHunter.Audio;
using CowboyHunter.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CowboyHunter.UI
{
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] RunConfig config;
        [SerializeField] Button newGameButton;
        [SerializeField] Button continueButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button quitButton;

        void Start()
        {
            Sound.PlayMusic(MusicId.Title);
            newGameButton.onClick.AddListener(() =>
            {
                Sound.Play(SoundId.UiClick);
                GameSession.Run = new RunState(config, new System.Random());
                SceneManager.LoadScene(SceneNames.WantedBoard);
            });

            settingsButton.onClick.AddListener(() => PauseMenu.Instance?.Open());
            quitButton.onClick.AddListener(PauseMenu.QuitGame);

            var saved = SaveSystem.Load(config);
            continueButton.interactable = saved != null;
            continueButton.onClick.AddListener(() =>
            {
                Sound.Play(SoundId.UiClick);
                GameSession.Run = saved;
                SceneManager.LoadScene(saved.Phase == RunPhase.Shop ? SceneNames.Shop : SceneNames.WantedBoard);
            });
        }
    }
}
