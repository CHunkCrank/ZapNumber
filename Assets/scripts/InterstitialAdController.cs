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

    private void Start() => LoadInterstitialAd();

    public void LoadInterstitialAd()
    {
        if (_isLoading) return;
        _isLoading = true;
        _interstitialAd?.Destroy();
        
        InterstitialAd.Load(AdUnitId, new AdRequest(), (ad, error) => {
            _isLoading = false;
            if (error != null) return;
            _interstitialAd = ad;
        });
    }

    public void ShowIfReady()
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
            _interstitialAd.Show();
        else
            LoadInterstitialAd();
    }
}
