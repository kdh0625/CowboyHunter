using System.Collections.Generic;
using CowboyHunter.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CowboyHunter.UI
{
    // 수배서 게시판. 수배서를 고르면 전투 씬으로 넘어간다.
    public class WantedBoardScreen : MonoBehaviour
    {
        [Tooltip("이 씬을 단독 실행할 때 새 런을 만드는 데 쓴다")]
        [SerializeField] RunConfig fallbackConfig;

        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text remainingText;
        [SerializeField] Transform posterRow;
        [SerializeField] Button posterTemplate;
        [SerializeField] Button bossButton;

        static readonly Color PosterColor = new(0.91f, 0.84f, 0.66f);
        static readonly Color EliteColor = new(0.95f, 0.70f, 0.45f);
        static readonly Color LockedColor = new(0.55f, 0.55f, 0.55f);

        void Start()
        {
            GameSession.Run ??= new RunState(fallbackConfig, new System.Random());
            var run = GameSession.Run;

            posterTemplate.gameObject.SetActive(false);
            for (int i = 0; i < run.Posters.Count; i++)
            {
                int index = i;
                var poster = run.Posters[i];
                var view = Instantiate(posterTemplate, posterRow);
                view.gameObject.SetActive(true);
                view.image.color = poster.IsElite ? EliteColor : PosterColor;
                view.GetComponentInChildren<TMP_Text>().text =
                    $"WANTED\n\n{poster.Enemy.displayName}\n\n{(poster.IsElite ? "[엘리트]\n" : "")}{poster.Bounty} GOLD";
                view.onClick.AddListener(() => { run.Accept(index); SceneManager.LoadScene(SceneNames.Battle); });
            }

            bossButton.interactable = run.BossUnlocked;
            bossButton.image.color = run.BossUnlocked ? PosterColor : LockedColor;
            bossButton.GetComponentInChildren<TMP_Text>().text = run.BossUnlocked
                ? $"CHAPTER BOSS\n\n{run.BossPoster.Enemy.displayName}\n\n{run.BossPoster.Bounty} GOLD"
                : "CHAPTER BOSS\n\n???";
            bossButton.onClick.AddListener(() => { run.AcceptBoss(); SceneManager.LoadScene(SceneNames.Battle); });

            statusText.text = $"{run.Chapter.displayName} / {run.ChapterCount}   HP {run.PlayerHp}/{run.PlayerMaxHp}   {run.Gold} GOLD";
            remainingText.text = $"남은 적 : {run.RemainingEnemies}";
        }
    }
}
