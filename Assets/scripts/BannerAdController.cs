using UnityEngine;
using GoogleMobileAds.Api;

public class BannerAdController : MonoBehaviour
{
    [SerializeField] private string _adUnitIdAndroid = "ca-app-pub-9391723777771759/5444198019";
    [SerializeField] private string _adUnitIdIOS     = "ca-app-pub-9391723777771759/6745493782";

    private BannerView _bannerView;
    private bool _bannerRequested;

    private string AdUnitId =>
#if UNITY_ANDROID
        _adUnitIdAndroid;
#elif UNITY_IOS
        _adUnitIdIOS;
#else
        "unused";
#endif

    private void Start()
    {
        Debug.Log("Banner Start called");
        AdConsentHelper.RequestTrackingAuthorizationIfNeeded();

        MobileAdsInitializer.Initialize(() =>
        {
            Debug.Log("Banner MobileAds.Initialize callback called");
            RequestBannerIfNeeded();
        });

        // Fallback in case initialize callback is delayed/missed.
        Invoke(nameof(RequestBannerWithInitFallback), 1.0f);
    }

    private void RequestBannerIfNeeded()
    {
        Debug.Log($"RequestBannerIfNeeded called. requested={_bannerRequested}");
        if (_bannerRequested) return;
        _bannerRequested = true;
        RequestBanner();
    }

    private void RequestBannerWithInitFallback()
    {
        MobileAdsInitializer.Initialize(RequestBannerIfNeeded);
    }

    public void RequestBanner()
    {
        Debug.Log("RequestBanner called");

        if (_bannerView != null) _bannerView.Destroy();

        Debug.Log("Creating BannerView");
        _bannerView = new BannerView(AdUnitId, AdSize.Banner, AdPosition.Bottom);
        _bannerView.OnBannerAdLoaded += () => Debug.Log("Banner loaded");
        _bannerView.OnBannerAdLoadFailed += error =>
            Debug.LogError($"Banner failed: {error.GetMessage()}");

        _bannerView.LoadAd(new AdRequest());
    }

    private void OnDestroy()
    {
        Debug.Log("Banner OnDestroy called");
        CancelInvoke(nameof(RequestBannerWithInitFallback));
        _bannerView?.Destroy();
    }
}
