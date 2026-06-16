using System;
using System.Collections.Generic;
using System.Linq;
using GoogleMobileAds.Api;
using UnityEngine;

namespace AdMobWrapper
{
    [Serializable]
    public class AdPlacement
    {
        public string key = "default";
        public string adUnitId;
    }

    public class AdMobManager : MonoBehaviour
    {
        private static AdMobManager _instance;
        public static AdMobManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var existing = FindFirstObjectByType<AdMobManager>();
                    if (existing != null)
                    {
                        _instance = existing;
                        return _instance;
                    }

                    var go = new GameObject("[AdMobManager]");
                    _instance = go.AddComponent<AdMobManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public bool enableAds;

        // ─── Serialized Inspector Fields ───────────────────────────
        [Header("Banner")]
        [SerializeField] private string _bannerAdUnitId;
        [SerializeField] private AdSizeType _bannerSize = AdSizeType.Banner;
        [SerializeField] private AdPosition _bannerPosition = AdPosition.Bottom;

        [Header("Interstitial Placements")]
        [SerializeField] private AdPlacement[] _interstitialPlacements;

        [Header("Rewarded Placements")]
        [SerializeField] private AdPlacement[] _rewardedPlacements;

        [Header("Auto Load on Initialize")]
        [SerializeField] private bool _autoLoadOnInit = true;

        private BannerView _bannerView;
        private readonly Dictionary<string, InterstitialAd> _interstitialAds = new();
        private readonly Dictionary<string, RewardedAd> _rewardedAds = new();

        public bool IsInitialized { get; private set; }

        // ─── Events ───────────────────────────────────────────────
        public event Action OnInitialized;

        public event Action OnBannerAdLoaded;
        public event Action<LoadAdError> OnBannerAdFailed;
        public event Action OnBannerAdClicked;
        public event Action<AdValue> OnBannerAdPaid;

        public event Action<string> OnInterstitialAdLoaded;
        public event Action<string, LoadAdError> OnInterstitialAdFailed;
        public event Action<string> OnInterstitialAdOpened;
        public event Action<string> OnInterstitialAdClosed;
        public event Action<string, AdError> OnInterstitialAdFailedToShow;
        public event Action<string> OnInterstitialAdImpression;
        public event Action<string, AdValue> OnInterstitialAdPaid;

        public event Action<string> OnRewardedAdLoaded;
        public event Action<string, LoadAdError> OnRewardedAdFailed;
        public event Action<string> OnRewardedAdOpened;
        public event Action<string> OnRewardedAdClosed;
        public event Action<string, AdError> OnRewardedAdFailedToShow;
        public event Action<string> OnRewardedAdImpression;
        public event Action<string, Reward> OnRewardedAdEarned;
        public event Action<string, AdValue> OnRewardedAdPaid;

        // ─── Singleton Lifecycle ───────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        // ─── Initialize ───────────────────────────────────────────
        public void Initialize()
        {
            if (IsInitialized) return;

            MobileAds.SetiOSAppPauseOnBackground(true);

            MobileAds.Initialize(initStatus =>
            {
                IsInitialized = true;
                OnInitialized?.Invoke();
                Debug.Log("[AdMob] SDK initialized successfully");

                if (_autoLoadOnInit)
                    LoadAllFromInspector();
            });
        }

        public void LoadAllFromInspector()
        {
            if (!string.IsNullOrEmpty(_bannerAdUnitId))
                LoadBanner(_bannerAdUnitId, AdSizeFromType(_bannerSize), _bannerPosition);

            if (_interstitialPlacements != null)
                foreach (var p in _interstitialPlacements)
                    if (!string.IsNullOrEmpty(p.adUnitId))
                        LoadInterstitial(p.adUnitId, p.key);

            if (_rewardedPlacements != null)
                foreach (var p in _rewardedPlacements)
                    if (!string.IsNullOrEmpty(p.adUnitId))
                        LoadRewarded(p.adUnitId, p.key);
        }

        // ─── Banner ───────────────────────────────────────────────
        public void LoadBanner(string adUnitId, AdSize adSize = null, AdPosition position = AdPosition.Bottom)
        {
            DestroyBanner();
            _bannerAdUnitId = adUnitId;

            _bannerView = new BannerView(_bannerAdUnitId, adSize ?? AdSize.Banner, position);
            RegisterBannerEvents();
            _bannerView.LoadAd(new AdRequest());
        }

        public void ShowBanner()
        {
            if (_bannerView != null)
                _bannerView.Show();
            else
                Debug.LogWarning("[AdMob] Banner not loaded");
        }

        public void HideBanner()
        {
            if (_bannerView != null)
                _bannerView.Hide();
        }

        public void DestroyBanner()
        {
            if (_bannerView != null)
            {
                _bannerView.Destroy();
                _bannerView = null;
            }
        }

        // ─── Interstitial ─────────────────────────────────────────
        public void LoadInterstitial(string adUnitId, string key = "default")
        {
            DestroyInterstitial(key);

            InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null)
                {
                    OnInterstitialAdFailed?.Invoke(key, error);
                    Debug.LogError($"[AdMob] Interstitial [{key}] failed: {error.GetMessage()}");
                    return;
                }

                _interstitialAds[key] = ad;
                RegisterInterstitialEvents(ad, key);
                OnInterstitialAdLoaded?.Invoke(key);
                Debug.Log($"[AdMob] Interstitial [{key}] loaded");
            });
        }

        public bool IsInterstitialReady(string key = "default")
        {
            return _interstitialAds.TryGetValue(key, out var ad) && ad.CanShowAd();
        }

        public void ShowInterstitial(string key = "default")
        {
            if (IsInterstitialReady(key))
                _interstitialAds[key].Show();
            else
                Debug.LogWarning($"[AdMob] Interstitial [{key}] not ready");
        }

        public void DestroyInterstitial(string key = "default")
        {
            if (_interstitialAds.TryGetValue(key, out var ad))
            {
                ad.Destroy();
                _interstitialAds.Remove(key);
            }
        }

        public void DestroyAllInterstitial()
        {
            foreach (var ad in _interstitialAds.Values)
                ad.Destroy();
            _interstitialAds.Clear();
        }

        // ─── Rewarded ─────────────────────────────────────────────
        public void LoadRewarded(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_rewardedPlacements != null)
            {
                AdPlacement adPlacement = _rewardedPlacements.FirstOrDefault(i => i.key == key);
                if (adPlacement != null && !string.IsNullOrEmpty(adPlacement.adUnitId))
                {
                    LoadRewarded(adPlacement.adUnitId, key);
                }
            }
        }
        public void LoadRewarded(string adUnitId, string key = "default")
        {
            DestroyRewarded(key);

            RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null)
                {
                    OnRewardedAdFailed?.Invoke(key, error);
                    Debug.LogError($"[AdMob] Rewarded [{key}] failed: {error.GetMessage()}");
                    return;
                }

                _rewardedAds[key] = ad;
                RegisterRewardedEvents(ad, key);
                OnRewardedAdLoaded?.Invoke(key);
                Debug.Log($"[AdMob] Rewarded [{key}] loaded");
            });
        }

        public bool IsRewardedReady(string key = "default")
        {
            return _rewardedAds.TryGetValue(key, out var ad) && ad.CanShowAd();
        }

        public void ShowRewarded(string key = "default", Action<Reward> OnReward = null)
        {
            if (!IsRewardedReady(key))
            {
                Debug.LogWarning($"[AdMob] Rewarded [{key}] not ready");
                return;
            }

            _rewardedAds[key].Show(reward =>
            {
                OnReward?.Invoke(reward);
                // OnRewardedAdEarned?.Invoke(key, reward);
            });
        }

        public void DestroyRewarded(string key = "default")
        {
            if (_rewardedAds.TryGetValue(key, out var ad))
            {
                ad.Destroy();
                _rewardedAds.Remove(key);
            }
        }

        public void DestroyAllRewarded()
        {
            foreach (var ad in _rewardedAds.Values)
                ad.Destroy();
            _rewardedAds.Clear();
        }

        // ─── Event Wiring ─────────────────────────────────────────
        private void RegisterBannerEvents()
        {
            _bannerView.OnBannerAdLoaded += () =>
            {
                OnBannerAdLoaded?.Invoke();
                Debug.Log("[AdMob] Banner ad loaded");
            };

            _bannerView.OnBannerAdLoadFailed += error =>
            {
                OnBannerAdFailed?.Invoke(error);
                Debug.LogError($"[AdMob] Banner ad failed: {error.GetMessage()}");
            };

            _bannerView.OnAdClicked += () => OnBannerAdClicked?.Invoke();
            _bannerView.OnAdPaid += value => OnBannerAdPaid?.Invoke(value);
        }

        private void RegisterInterstitialEvents(InterstitialAd ad, string key)
        {
            ad.OnAdFullScreenContentOpened += () => OnInterstitialAdOpened?.Invoke(key);
            ad.OnAdFullScreenContentClosed += () =>
            {
                OnInterstitialAdClosed?.Invoke(key);
                if (_interstitialAds.ContainsKey(key))
                    _interstitialAds.Remove(key);
            };
            ad.OnAdFullScreenContentFailed += error =>
            {
                OnInterstitialAdFailedToShow?.Invoke(key, error);
                Debug.LogError($"[AdMob] Interstitial [{key}] show failed: {error.GetMessage()}");
            };
            ad.OnAdImpressionRecorded += () => OnInterstitialAdImpression?.Invoke(key);
            ad.OnAdPaid += value => OnInterstitialAdPaid?.Invoke(key, value);
        }

        private void RegisterRewardedEvents(RewardedAd ad, string key)
        {
            ad.OnAdFullScreenContentOpened += () => OnRewardedAdOpened?.Invoke(key);
            ad.OnAdFullScreenContentClosed += () =>
            {
                OnRewardedAdClosed?.Invoke(key);
                if (_rewardedAds.ContainsKey(key))
                    _rewardedAds.Remove(key);
            };
            ad.OnAdFullScreenContentFailed += error =>
            {
                OnRewardedAdFailedToShow?.Invoke(key, error);
                Debug.LogError($"[AdMob] Rewarded [{key}] show failed: {error.GetMessage()}");
            };
            ad.OnAdImpressionRecorded += () => OnRewardedAdImpression?.Invoke(key);
            ad.OnAdPaid += value => OnRewardedAdPaid?.Invoke(key, value);
        }

        // ─── AdSize Helper ─────────────────────────────────────────
        private static AdSize AdSizeFromType(AdSizeType type)
        {
            return type switch
            {
                AdSizeType.Banner => AdSize.Banner,
                AdSizeType.IABBanner => AdSize.IABBanner,
                AdSizeType.LargeBanner => AdSize.LargeBanner,
                AdSizeType.Leaderboard => AdSize.Leaderboard,
                AdSizeType.AnchoredAdaptive => AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth),
                _ => AdSize.Banner
            };
        }

        // ─── Cleanup ──────────────────────────────────────────────
        private void OnDestroy()
        {
            DestroyBanner();
            DestroyAllInterstitial();
            DestroyAllRewarded();
        }
    }

    public enum AdSizeType
    {
        Banner,
        IABBanner,
        LargeBanner,
        Leaderboard,
        AnchoredAdaptive
    }
}
