using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System;

public class CountdownTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    private int remainingSeconds;
    private Coroutine countDownCoroutine;

    public static event Action OnTimerComplete;

    private void Start()
    {
        // remainingSeconds = totalSeconds;
        // UpdateDisplay(remainingSeconds);
        // StartCoroutine(CountdownRoutine());
    }
    void OnDisable()
    {
        Debug.Log($"[{nameof(CountdownTimer)}] On Disable Call");
        StopCountDownCoroutine();
    }

    public void SetTimerAndStart(int amountInSecond)
    {
        StopCountDownCoroutine();
        remainingSeconds = amountInSecond;
        UpdateDisplay(remainingSeconds);
        countDownCoroutine = StartCoroutine(CountdownRoutine());
    }
    public void StopCountDownCoroutine()
    {
        if (countDownCoroutine != null)
        {
            StopCoroutine(countDownCoroutine);
            countDownCoroutine = null;
        }
    }

    public bool IsTimerComplete => remainingSeconds == 0;
    public void SetActive(bool value)
    {
        if (gameObject.activeSelf == value) return;
        gameObject.SetActive(value);
    }
    private IEnumerator CountdownRoutine()
    {
        while (remainingSeconds > 0)
        {
            yield return new WaitForSeconds(1f);
            remainingSeconds--;
            UpdateDisplay(remainingSeconds);
        }
        timerText.text = "00:00";
        OnTimerComplete?.Invoke();
    }

    private void UpdateDisplay(int seconds)
    {
        timerText.text = SecondsToMinutesSeconds(seconds);
    }

    public static string SecondsToMinutesSeconds(int totalSeconds)
    {
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return $"{minutes:D2}:{secs:D2}";
    }
}
