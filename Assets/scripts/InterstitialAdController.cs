using UnityEngine;
using GoogleMobileAds.Api;

public class InterstitialAdController : MonoBehaviour
{
    // Google公式テストID
    [SerializeField] private string interstitialAdUnitIdAndroid = "ca-app-pub-9391723777771759/3842014687";
    [SerializeField] private string interstitialAdUnitIdIOS     = "ca-app-pub-9391723777771759~4135626409";

    [Header("Optional")]
    [SerializeField] private BannerAdController banner; // バナーも同時に出したい場合だけセット

    private InterstitialAd _ad;
    private bool _isLoading;

    private string UnitId
    {
        get
        {
#if UNITY_ANDROID
            return interstitialAdUnitIdAndroid;
#elif UNITY_IOS
            return interstitialAdUnitIdIOS;
#else
            return interstitialAdUnitIdAndroid;
#endif
        }
    }

    private void Start()
    {
        // 任意：インタースティシャル起動時にバナーも要求する
        banner?.RequestBanner();

        // インタースティシャル読み込み
        Load();
    }

    public void Load()
    {
        if (_isLoading) return;
        _isLoading = true;

        _ad?.Destroy();
        _ad = null;

        var request = new AdRequest();

        InterstitialAd.Load(UnitId, request, (InterstitialAd ad, LoadAdError error) =>
        {
            _isLoading = false;

            if (error != null)
            {
                Debug.LogWarning($"[Interstitial] Load failed: {error}");
                return;
            }

            _ad = ad;
            if (_ad == null) return;

            _ad.OnAdFullScreenContentClosed += () => Load();
            _ad.OnAdFullScreenContentFailed += (AdError adError) =>
            {
                Debug.LogWarning($"[Interstitial] FullScreen failed: {adError}");
                Load();
            };
        });
    }

    public void ShowIfReady()
    {
        if (_ad == null)
        {
            Debug.Log("[Interstitial] not ready yet");
            Load();
            return;
        }

        _ad.Show();
    }

    private void OnDestroy()
    {
        _ad?.Destroy();
    }
}
