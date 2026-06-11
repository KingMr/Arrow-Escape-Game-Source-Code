using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Core;
using System;

namespace UI
{
    public class ShopUI : MonoBehaviour
    {
        [Header("References")]
        public GameObject shopPanel;
        public Transform contentContainer;
        public GameObject shopItemPrefab;
        public Button closeButton;
        public Button playButton;

        private List<ShopItemUI> instantiatedItems = new List<ShopItemUI>();

        private GameObject activeWhenPanelClose;
        private bool isNeedToActive;

        void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnClickPlay);
            }

            // Listen for theme changes to update UI if open
            if (ThemeManager.Instance != null)
            {
                // Optional: ThemeManager event could trigger refresh
            }
        }

        private void OnClickPlay()
        {
            if (activeWhenPanelClose != null)
            {
                if (activeWhenPanelClose.activeSelf)
                {
                    activeWhenPanelClose.SetActive(false);
                }
                activeWhenPanelClose = null;
            }

            Hide();
            LevelManager.Instance.LoadAndStart();
        }

        private bool wasWinPanelActive = false;

        [EasyButtons.Button]
        public void Show()
        {
            if (UIManager.Instance != null && UIManager.Instance.IsSequenceRunning) return;

            if (shopPanel != null) shopPanel.SetActive(true);

            // Check if Win Panel is active and hide it
            if (UIManager.Instance != null && UIManager.Instance.winPanel != null)
            {
                wasWinPanelActive = UIManager.Instance.winPanel.activeSelf;
                if (wasWinPanelActive)
                {
                    UIManager.Instance.winPanel.SetActive(false);
                }
            }

            PopulateShop();
        }

        public void Show(GameObject hideObj, bool isNeedToHide)
        {
            activeWhenPanelClose = hideObj;
            isNeedToActive = isNeedToHide;
            Show();
        }
        public void Hide()
        {
            if (shopPanel != null) shopPanel.SetActive(false);

            if (activeWhenPanelClose != null)
            {
                activeWhenPanelClose.SetActive(true);
                activeWhenPanelClose = null;
            }


            AudioManager.Instance?.PlayButtonSound();

            // Restore Win Panel if it was active
            if (wasWinPanelActive && UIManager.Instance != null && UIManager.Instance.winPanel != null)
            {
                UIManager.Instance.winPanel.SetActive(true);
            }
            wasWinPanelActive = false;
        }

        public void Toggle()
        {
            if (shopPanel != null)
            {
                if (shopPanel.activeSelf) Hide();
                else Show();
            }
        }

        private void PopulateShop()
        {
            // Clear existing
            foreach (var item in instantiatedItems)
            {
                Destroy(item.gameObject);
            }
            instantiatedItems.Clear();

            if (ThemeManager.Instance == null) return;

            foreach (var theme in ThemeManager.Instance.allThemes)
            {
                GameObject go = Instantiate(shopItemPrefab, contentContainer);
                ShopItemUI itemUI = go.GetComponent<ShopItemUI>();
                if (itemUI != null)
                {
                    itemUI.Setup(theme, this);
                    instantiatedItems.Add(itemUI);
                }
            }
        }

        public void RefreshAllItems()
        {
            foreach (var item in instantiatedItems)
            {
                item.RefreshState();
            }

        }
    }
}
