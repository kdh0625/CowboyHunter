using System.Collections.Generic;
using System.Linq;
using CowboyHunter.Audio;
using CowboyHunter.Battle;
using CowboyHunter.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CowboyHunter.UI
{
    // 마차 상점. 물건을 사고, 총의 유물을 바꾸고, 다음 수배서로 출발한다.
    public class ShopScreen : MonoBehaviour
    {
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text messageText;
        [SerializeField] Transform offerRow;
        [SerializeField] Button offerTemplate;
        [SerializeField] Button[] slotButtons = new Button[4];   // 총구, 조준경, 실린더, 손잡이 순서
        [SerializeField] Transform inventoryList;
        [SerializeField] Button inventoryTemplate;
        [SerializeField] GameObject removalPanel;
        [SerializeField] Transform removalList;
        [SerializeField] Button removalTemplate;
        [SerializeField] Button leaveButton;

        // 스킨 스프라이트에 곱해지는 색
        static readonly Color ItemColor = Color.white;
        static readonly Color SoldColor = new(0.55f, 0.55f, 0.55f);

        readonly List<GameObject> _spawned = new();
        RunState _run;

        void Start()
        {
            _run = GameSession.Run;
            Sound.PlayMusic(MusicId.Shop);
            offerTemplate.gameObject.SetActive(false);
            inventoryTemplate.gameObject.SetActive(false);
            removalTemplate.gameObject.SetActive(false);
            removalPanel.SetActive(false);
            messageText.text = "";

            for (int i = 0; i < slotButtons.Length; i++)
            {
                var slot = (RelicSlot)i;
                slotButtons[i].onClick.AddListener(() => { _run.Unequip(slot); Sound.Play(SoundId.Equip); Refresh(); });
            }
            leaveButton.onClick.AddListener(() =>
            {
                Sound.Play(SoundId.UiClick);
                _run.LeaveShop();
                SceneManager.LoadScene(SceneNames.WantedBoard);
            });
            Refresh();
        }

        // 상점에서 무언가 할 때마다 호출되므로, 여기서 자동 저장한다.
        void Refresh()
        {
            SaveSystem.Save(_run);
            foreach (var go in _spawned) Destroy(go);
            _spawned.Clear();
            var shop = _run.Shop;
            var config = _run.Config;

            statusText.text = $"마차 상점     HP {_run.PlayerHp}/{_run.PlayerMaxHp}     {_run.Gold} GOLD     탄창 {_run.Deck.Count}발";

            for (int i = 0; i < shop.Bullets.Count; i++)
            {
                int index = i;
                var offer = shop.Bullets[i];
                AddOffer($"{offer.Bullet.displayName}\n<size=70%>{offer.Bullet.description}</size>\n{offer.Bullet.price} GOLD", offer.Sold,
                    () => Try(_run.BuyBullet(index), $"{offer.Bullet.displayName}을(를) 탄창에 넣었습니다."), offer.Bullet.icon);
            }
            if (shop.Relic != null)
            {
                var relic = shop.Relic;
                AddOffer($"[유물 · {RelicData.SlotName(relic.slot)}]\n{relic.displayName}\n<size=70%>{relic.description}</size>\n{relic.price} GOLD", shop.RelicSold,
                    () => Try(_run.BuyRelic(), $"{relic.displayName}을(를) 얻었습니다."), relic.icon);
            }
            AddOffer($"위스키\n<size=70%>체력 {config.whiskeyHeal} 회복</size>\n{config.whiskeyPrice} GOLD", shop.WhiskeySold,
                () => { if (Try(_run.BuyWhiskey(), "체력을 회복했습니다.")) Sound.Play(SoundId.Heal); });
            AddOffer($"탄환 제거\n<size=70%>탄창에서 1발 제거</size>\n{config.removalPrice} GOLD", shop.RemovalUsed,
                () => { removalPanel.SetActive(!removalPanel.activeSelf); Refresh(); });

            for (int i = 0; i < slotButtons.Length; i++)
            {
                var slot = (RelicSlot)i;
                var relic = _run.GetEquipped(slot);
                slotButtons[i].GetComponentInChildren<TMP_Text>().text = relic != null
                    ? $"{RelicData.SlotName(slot)}\n{relic.displayName}\n<size=70%>{relic.description}</size>"
                    : $"{RelicData.SlotName(slot)}\n(비어 있음)";
                slotButtons[i].image.color = relic != null ? ItemColor : SoldColor;
                UiSprites.Show(UiSprites.Child(slotButtons[i], "Icon"), relic != null ? relic.icon : null);
            }

            foreach (var relic in _run.Inventory)
            {
                var item = Spawn(inventoryTemplate, inventoryList, $"{relic.displayName} <size=70%>({RelicData.SlotName(relic.slot)})</size>");
                UiSprites.Show(UiSprites.Child(item, "Icon"), relic.icon);
                item.onClick.AddListener(() => { _run.Equip(relic); Sound.Play(SoundId.Equip); Refresh(); });
            }

            if (removalPanel.activeSelf)
            {
                foreach (var group in _run.Deck.GroupBy(b => b).OrderBy(g => g.Key.displayName))
                {
                    var bullet = group.Key;
                    var item = Spawn(removalTemplate, removalList, $"{bullet.displayName} x{group.Count()}");
                    item.onClick.AddListener(() =>
                    {
                        if (Try(_run.RemoveBullet(bullet), $"{bullet.displayName} 1발을 제거했습니다.")) removalPanel.SetActive(false);
                        Refresh();
                    });
                }
            }
        }

        void AddOffer(string label, bool sold, UnityEngine.Events.UnityAction onBuy, Sprite icon = null)
        {
            var button = Spawn(offerTemplate, offerRow, sold ? label + "\n<size=70%>(판매 완료)</size>" : label);
            UiSprites.Show(UiSprites.Child(button, "Icon"), icon);
            button.interactable = !sold;
            button.image.color = sold ? SoldColor : ItemColor;
            button.onClick.AddListener(() => { onBuy(); Refresh(); });
        }

        Button Spawn(Button template, Transform parent, string label)
        {
            var button = Instantiate(template, parent);
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<TMP_Text>().text = label;
            _spawned.Add(button.gameObject);
            return button;
        }

        bool Try(bool success, string successMessage)
        {
            messageText.text = success ? successMessage : "골드가 부족하거나 살 수 없습니다.";
            Sound.Play(success ? SoundId.Buy : SoundId.BuyFail);
            return success;
        }
    }
}
