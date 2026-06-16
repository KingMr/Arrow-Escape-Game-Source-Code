using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core;
using AdMobWrapper;
using System;

namespace UI
{
    public class ShopItemUI : MonoBehaviour
    {
        [Header("UI References")]
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI priceText;
        public Button actionButton;
        public TextMeshProUGUI actionButtonText;
        public GameObject selectedIndicator; // e.g. a checkmark or "Selected" text overlay
        public GameObject rvObj;
        public TextMeshProUGUI rvCountText;
        public Button rvButton;
        public GameObject buttonCoinImageObj; //action button coin image

        private ArrowTheme myTheme;
        private ShopUI shopUI;

        public void Setup(ArrowTheme theme, ShopUI ui)
        {
            myTheme = theme;
            shopUI = ui;

            if (iconImage != null) iconImage.sprite = theme.icon;
            if (nameText != null) nameText.text = theme.themeName;

            myTheme.watchedRV = PlayerPrefs.GetInt($"{myTheme.id}_watched_rv", 0);

            RefreshState();

            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionButtonClicked);

            rvButton.onClick.RemoveAllListeners();
            rvButton.onClick.AddListener(OnClickRVButton);
        }

        private void OnClickRVButton()
        {
            if (!AdMobManager.Instance.IsRewardedReady(AdMobConstants.SHOP_KEY))
            {
                AdMobManager.Instance.LoadRewarded(AdMobConstants.SHOP_KEY);
                ShowRewarded(AdMobConstants.DEFAULT);
            }
            else
            {
                ShowRewarded(AdMobConstants.SHOP_KEY);
            }


            void ShowRewarded(string key)
            {
                AdMobManager.Instance.ShowRewarded(key, (reward) =>
                {
                    myTheme.watchedRV += 1;
                    PlayerPrefs.SetInt($"{myTheme.id}_watched_rv", myTheme.watchedRV);

                    if (myTheme.CheckAllRVWatched())
                    {
                        UnlockAndSelect();
                    }
                    else
                    {
                        RefreshState();
                    }
                });
            }

        }

        void Update()
        {
            if (myTheme == null) return;

            // If locked:
            // - If Ads Enabled: Check Ad Ready
            // - If Ads Disabled: Check Coin Balance
            if (!ThemeManager.Instance.IsThemeUnlocked(myTheme.id))
            {
                bool useAds = AdMobManager.Instance != null && AdMobManager.Instance.enableAds;

                // if (useAds)
                // {
                //     bool adReady = AdMobManager.Instance.IsRewardedReady();
                //     if (actionButton != null) actionButton.interactable = adReady;
                // }
                // else
                {
                    // Use Coins
                    int price = myTheme.price > 0 ? myTheme.price : 500; // Default price if not set
                    bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.Coins >= price;
                    if (actionButton != null) actionButton.interactable = canAfford;
                }
            }
        }

        public void RefreshState()
        {
            if (myTheme == null) return;

            bool isUnlocked = ThemeManager.Instance.IsThemeUnlocked(myTheme.id);
            bool isSelected = ThemeManager.Instance.IsThemeSelected(myTheme.id);

            if (selectedIndicator != null) selectedIndicator.SetActive(isSelected);

            if (isSelected)
            {
                rvObj.SetActive(false);
                actionButton.gameObject.SetActive(false);

                // Already selected
                if (actionButtonText != null) actionButtonText.text = "Selected";
                actionButton.interactable = false;
                if (priceText != null) priceText.text = "";
            }
            else if (isUnlocked)
            {
                rvObj.SetActive(false);
                buttonCoinImageObj.SetActive(false);
                // Unlocked but not selected
                if (actionButtonText != null) actionButtonText.text = "Select";
                actionButton.interactable = true;
                if (priceText != null) priceText.text = "Owned";
            }
            else
            {

                rvObj.SetActive(myTheme.canPurchaseUsingRV);
                actionButton.gameObject.SetActive(true);
                buttonCoinImageObj.SetActive(true);

                if (myTheme.canPurchaseUsingRV)
                {

                    // if (actionButtonText != null) actionButtonText.text = "Watch Ad";
                    if (rvCountText != null) rvCountText.text = $"{myTheme.watchedRV}/{myTheme.needToWatchRV}";
                    // Interactable handled in Update()
                }

                {
                    int price = myTheme.price > 0 ? myTheme.price : 500;
                    if (actionButtonText != null) actionButtonText.text = "Buy";
                    if (priceText != null) priceText.text = $"{price}";
                    // Interactable handled in Update()
                }
            }
        }

        private void OnActionButtonClicked()
        {
            if (myTheme == null) return;

            bool isUnlocked = ThemeManager.Instance.IsThemeUnlocked(myTheme.id);

            if (isUnlocked)
            {
                // Select it
                ThemeManager.Instance.SelectTheme(myTheme.id);
                shopUI.RefreshAllItems();
                AudioManager.Instance?.PlayButtonSound();
            }
            else
            {
                // Unlock Logic
                // bool useAds = AdMobManager.Instance != null && AdMobManager.Instance.enableAds;

                // if (useAds)
                // {
                //     // Try to unlock via Ad
                //     AdMobManager.Instance.ShowRewarded(AdMobConstants.DEFAULT, (reward) =>
                //     {
                //         UnlockAndSelect();
                //         Debug.Log($"Theme {myTheme.id} unlocked via Ad!");
                //     });
                // }
                // else
                {
                    // Try to unlock via Coins
                    int price = myTheme.price > 0 ? myTheme.price : 500;
                    if (CurrencyManager.Instance != null && CurrencyManager.Instance.SpendCoins(price))
                    {
                        UnlockAndSelect();
                        Debug.Log($"Theme {myTheme.id} unlocked via Coins!");
                    }
                    else
                    {
                        Debug.Log("Not enough coins!");
                        AudioManager.Instance?.PlayBlockedSound();
                    }
                }
            }
        }

        private void UnlockAndSelect()
        {
            ThemeManager.Instance.UnlockTheme(myTheme.id);
            // Optionally auto-select? The previous code didn't, but usually shops do. 
            // The previous code REFRESHED, so it would show as "Select". 
            // Let's just Unlock it for now, user can then click Select.
            shopUI.RefreshAllItems();
            AudioManager.Instance?.PlayCoinSpendSound();
        }
    }
}
