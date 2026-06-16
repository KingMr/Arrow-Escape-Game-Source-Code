using UnityEngine;
using UnityEngine.UI;

namespace AdMobWrapper.Demo
{
    public class AdMobDemo : MonoBehaviour
    {
        [SerializeField] private Button _shopInterstitialBtn;
        [SerializeField] private Button _levelCompleteBtn;
        [SerializeField] private Button _rewardedBtn;
        [SerializeField] private Text _statusText;

        private void Start()
        {
            AdMobManager.Instance.OnInitialized += () =>
            {
                Log("AdMob initialized! Inspector placements loading...");
            };

            AdMobManager.Instance.OnInterstitialAdLoaded += key =>
                Log($"Interstitial [{key}] ready");
            AdMobManager.Instance.OnInterstitialAdClosed += key =>
                Log($"Interstitial [{key}] closed");

            AdMobManager.Instance.OnRewardedAdEarned += (key, reward) =>
                Log($"Reward [{key}]: {reward.Amount} {reward.Type}");
            AdMobManager.Instance.OnRewardedAdClosed += key =>
                Log($"Rewarded [{key}] closed");

            AdMobManager.Instance.OnBannerAdLoaded += () =>
                Log("Banner loaded");

            if (_shopInterstitialBtn)
                _shopInterstitialBtn.onClick.AddListener(() =>
                {
                    if (AdMobManager.Instance.IsInterstitialReady("shop"))
                        AdMobManager.Instance.ShowInterstitial("shop");
                    else
                    {
                        Log("Loading shop interstitial...");
                        AdMobManager.Instance.LoadInterstitial(AdMobConstants.INTERSTITIAL_UNIT_ID, "shop");
                    }
                });

            if (_levelCompleteBtn)
                _levelCompleteBtn.onClick.AddListener(() =>
                {
                    if (AdMobManager.Instance.IsInterstitialReady("level_complete"))
                        AdMobManager.Instance.ShowInterstitial("level_complete");
                    else
                    {
                        Log("Loading level interstitial...");
                        AdMobManager.Instance.LoadInterstitial(AdMobConstants.INTERSTITIAL_UNIT_ID, "level_complete");
                    }
                });

            if (_rewardedBtn)
                _rewardedBtn.onClick.AddListener(() =>
                {
                    if (AdMobManager.Instance.IsRewardedReady("rewarded"))
                        AdMobManager.Instance.ShowRewarded("rewarded");
                    else
                        Log("Rewarded not ready yet");
                });

            AdMobManager.Instance.Initialize();
        }

        private void Log(string msg)
        {
            Debug.Log($"[AdMobDemo] {msg}");
            if (_statusText) _statusText.text = msg;
        }
    }
}
