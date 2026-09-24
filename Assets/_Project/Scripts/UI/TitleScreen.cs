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

        void Start()
        {
            newGameButton.onClick.AddListener(() =>
            {
                GameSession.Run = new RunState(config, new System.Random());
                SceneManager.LoadScene(SceneNames.WantedBoard);
            });

            var saved = SaveSystem.Load(config);
            continueButton.interactable = saved != null;
            continueButton.onClick.AddListener(() =>
            {
                GameSession.Run = saved;
                SceneManager.LoadScene(saved.Phase == RunPhase.Shop ? SceneNames.Shop : SceneNames.WantedBoard);
            });
        }
    }
}
