using UnityEngine;
using GoogleMobileAds.Api;

public class InterstitialAdController : MonoBehaviour
{
    [Header("Ad Unit IDs")]
    [SerializeField] private string interstitialAdUnitIdAndroid = "ca-app-pub-9391723777771759/5444198019"; // テスト用Android
    [SerializeField] private string interstitialAdUnitIdIOS     = "ca-app-pub-9391723777771759/6745493782"; // テスト用iOS

    private InterstitialAd _interstitialAd;
    private bool _isLoading;

    private string AdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return interstitialAdUnitIdAndroid;
#elif UNITY_IOS
            return interstitialAdUnitIdIOS;
#else
            return "unused";
#endif
        }
    }

    private void Start()
    {
#if UNITY_IOS
        // iOSの場合、ATTのステータスを確認
        var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
        if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            // まだ許可も拒否もしていない場合、ポップアップを表示
            ATTrackingStatusBinding.RequestAuthorizationTracking();
            // ポップアップが消えるのを待ってから初期化するために少し遅延させるか、
            // ステータスが変わるまでループする処理が必要ですが、簡易的にはUpdate等で監視します。
        }
#endif
        // AdMobの初期化を開始
        MobileAds.Initialize(_ => RequestBanner());
    }


    public void LoadInterstitialAd()
    {
        if (_isLoading) return;
        _isLoading = true;

        // 古い広告を破棄
        _interstitialAd?.Destroy();
        _interstitialAd = null;

        var adRequest = new AdRequest();

        InterstitialAd.Load(AdUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            _isLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogError($"[AdMob] Interstitial load failed: {error}");
                return;
            }

            Debug.Log("[AdMob] Interstitial loaded.");
            _interstitialAd = ad;

            // 閉じたら次を自動ロード
            _interstitialAd.OnAdFullScreenContentClosed += () => LoadInterstitialAd();
            _interstitialAd.OnAdFullScreenContentFailed += (AdError adError) =>
            {
                Debug.LogError($"[AdMob] Interstitial show failed: {adError}");
                LoadInterstitialAd();
            };
        });
    }

    public void ShowIfReady()
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            _interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("[AdMob] Interstitial ad is not ready yet.");
            LoadInterstitialAd();
        }
    }

    private void OnDestroy()
    {
        _interstitialAd?.Destroy();
    }
}

