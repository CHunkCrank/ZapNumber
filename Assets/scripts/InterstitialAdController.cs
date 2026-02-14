using UnityEngine;
using GoogleMobileAds.Api;

public class InterstitialAdController : MonoBehaviour
{
    [SerializeField] private string interstitialAdUnitIdAndroid = "ca-app-pub-9391723777771759/3842014687";
    [SerializeField] private string interstitialAdUnitIdIOS     = "ca-app-pub-9391723777771759/3371133594";

    private InterstitialAd _interstitialAd;
    private bool _isLoading;

    private string AdUnitId =>
#if UNITY_ANDROID
        interstitialAdUnitIdAndroid;
#elif UNITY_IOS
        interstitialAdUnitIdIOS;
#else
        "unused";
#endif

    private void Start()
    {
        AdConsentHelper.RequestTrackingAuthorizationIfNeeded();
        MobileAdsInitializer.Initialize(LoadInterstitialAd);
        Invoke(nameof(LoadInterstitialWithInitFallback), 1.0f);
    }

    public void LoadInterstitialAd()
    {
        if (_isLoading) return;

        _isLoading = true;
        _interstitialAd?.Destroy();

        InterstitialAd.Load(AdUnitId, new AdRequest(), (ad, error) =>
        {
            _isLoading = false;

            if (error != null)
            {
                Debug.LogError($"Interstitial failed: {error.GetMessage()}");
                return;
            }

            _interstitialAd = ad;
            Debug.Log("Interstitial loaded");
        });
    }

    public void ShowIfReady()
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            _interstitialAd.Show();
            return;
        }

        Debug.Log("Interstitial not ready, reloading.");
        LoadInterstitialAd();
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(LoadInterstitialWithInitFallback));
        _interstitialAd?.Destroy();
    }

    private void LoadInterstitialWithInitFallback()
    {
        MobileAdsInitializer.Initialize(LoadInterstitialAd);
    }
}
