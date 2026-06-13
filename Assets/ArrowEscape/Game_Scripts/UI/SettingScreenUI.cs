using System;
using Core;
using UnityEngine;
using UnityEngine.UI;

public class SettingScreenUI : MonoBehaviour
{
    public Toggle soundToggle;
    public Toggle vibrationToggle;
    public Button closeButton;

    private GameObject previewsObj;

    private bool isSoundOn;
    private bool isVibrationOn;

    public static event Action<bool> OnVibrationUpDate;
    public static event Action<bool> OnSoundUpdate;

    void Start()
    {
        soundToggle.onValueChanged.AddListener(OnSoundValueChanged);
        vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
        closeButton.onClick.AddListener(OnCloseClick);
    }

    private void OnCloseClick()
    {
        AudioManager.Instance?.PlayButtonSound();
        Hide();
    }

    private void OnVibrationChanged(bool arg0)
    {
        AudioManager.Instance?.PlayButtonSound();
        isVibrationOn = arg0;
        OnVibrationUpDate?.Invoke(arg0);
    }

    private void OnSoundValueChanged(bool arg0)
    {
        AudioManager.Instance?.PlayButtonSound();
        isSoundOn = arg0;
        OnSoundUpdate?.Invoke(arg0);
    }

    public void Show(GameObject previewsObj)
    {
        this.previewsObj = previewsObj;
        Show();
    }
    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
        if (previewsObj != null)
        {
            previewsObj.SetActive(true);
        }
    }
}
