using System;
using Core;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class HomeScreenUI : MonoBehaviour
{
    [Header("Scripts")]
    [SerializeField]
    private ShopUI shopUI;
    [SerializeField]
    private SettingScreenUI settingScreenUI;


    [Header("Variables")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI currencyText;
    public Button settingButton;
    public Button shopButton;
    public Button playButton;

    void OnEnable()
    {
    }
    void Start()
    {
        playButton.onClick.AddListener(OnClickPlay);
        shopButton.onClick.AddListener(OnClickShop);
        settingButton.onClick.AddListener(OnClickSetting);

        levelText.text = $"{LevelManager.Instance.currentLevelIndex + 1}";
    }

    private void OnClickSetting()
    {
        AudioManager.Instance?.PlayButtonSound();
        settingScreenUI.Show();
    }

    private void OnClickShop()
    {
        AudioManager.Instance?.PlayButtonSound();
        gameObject.SetActive(false);
        shopUI.Show(gameObject, true);
    }

    private void OnClickPlay()
    {
        Hide();
        AudioManager.Instance?.PlayButtonSound();
        LevelManager.Instance.LoadAndStart();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        levelText.text = $"{LevelManager.Instance.currentLevelIndex + 1}";
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
