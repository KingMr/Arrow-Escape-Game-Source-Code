using UnityEngine;

namespace AdMobWrapper
{
        public static class AdMobConstants
        {
#if UNITY_ANDROID
                public const string BANNER_UNIT_ID = "ca-app-pub-3940256099942544/6300978111"; // test
                public const string INTERSTITIAL_UNIT_ID = "ca-app-pub-3940256099942544/1033173712"; // test
                public const string REWARDED_UNIT_ID = "ca-app-pub-3940256099942544/5224354917"; // test
#elif UNITY_IOS
        public const string BANNER_UNIT_ID     = "ca-app-pub-3940256099942544/2934735716"; // test
        public const string INTERSTITIAL_UNIT_ID = "ca-app-pub-3940256099942544/4411468910"; // test
        public const string REWARDED_UNIT_ID   = "ca-app-pub-3940256099942544/1712485313"; // test
#else
        public const string BANNER_UNIT_ID     = "unused";
        public const string INTERSTITIAL_UNIT_ID = "unused";
        public const string REWARDED_UNIT_ID   = "unused";
#endif

                public const string DEFAULT = "default";
                public const string SHOP_KEY = "shop";
        }
}
