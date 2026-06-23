using System;
using AdMobWrapper;
using Core;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.UI;

public class NotEnoughCoinScreenUI : MonoBehaviour
{
    public Button closeButton;
    public Button watchRVButton;


    private GameObject activeWhenPanelClose;
    private bool isNeedToActive;

    void Start()
    {
        closeButton.onClick.AddListener(OnClickClose);
        watchRVButton.onClick.AddListener(OnClickRV);
    }

    private void OnClickRV()
    {
        AudioManager.Instance?.PlayLightImpactHaptic();
        AudioManager.Instance?.PlayButtonSound();

        AdMobManager.Instance.ShowRewarded(AdMobConstants.DEFAULT, (Reward) =>
        {
            //TODO:- Give Reward
            CurrencyManager.Instance.AddCoins(10);
            Hide();
        });
    }

    private void OnClickClose()
    {
        AudioManager.Instance?.PlayLightImpactHaptic();
        AudioManager.Instance?.PlayButtonSound();
        Hide();
    }

    public void Show(GameObject hideObj, bool isNeedToHide)
    {
        activeWhenPanelClose = hideObj;
        isNeedToActive = isNeedToHide;
        Show();
    }
    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);

        if (activeWhenPanelClose != null)
        {
            activeWhenPanelClose.SetActive(true);
            activeWhenPanelClose = null;
        }
    }
}
