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

        void Start()
        {
            newGameButton.onClick.AddListener(() =>
            {
                GameSession.Run = new RunState(config, new System.Random());
                SceneManager.LoadScene(SceneNames.WantedBoard);
            });
        }
    }
}
