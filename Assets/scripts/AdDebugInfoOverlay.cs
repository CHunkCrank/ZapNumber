using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#if UNITY_IOS && !UNITY_EDITOR
using Unity.Advertisement.IosSupport;
using UnityEngine.iOS;
#endif

public sealed class AdDebugInfoOverlay : MonoBehaviour
{
    private static AdDebugInfoOverlay _instance;
    private bool _visible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;

        var go = new GameObject("AdDebugInfoOverlay");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<AdDebugInfoOverlay>();
    }

    private void OnGUI()
    {
        var sw = Screen.width;

        if (!_visible)
        {
            if (GUI.Button(new Rect(160, 10, 160, 60), "AD-INFO"))
            {
                _visible = true;
            }

            return;
        }

        GUI.Box(new Rect(10, 80, sw - 20, 280), "Ad Debug Info");

        if (GUI.Button(new Rect(sw - 120, 90, 100, 50), "Close"))
        {
            _visible = false;
            return;
        }

        var info = BuildInfoText();
        GUI.TextArea(new Rect(20, 140, sw - 40, 180), info);

        if (GUI.Button(new Rect(20, 90, 120, 50), "Copy"))
        {
            GUIUtility.systemCopyBuffer = info;
            Debug.Log("AdDebugInfo copied to clipboard.");
        }
    }

    private static string BuildInfoText()
    {
#if UNITY_IOS && !UNITY_EDITOR
        var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
        var idfa = Device.advertisingIdentifier;
        var trackingEnabled = Device.advertisingTrackingEnabled;
        return
            "Platform: iOS\n" +
            "ATT: " + status + "\n" +
            "IDFA: " + idfa + "\n" +
            "AdvertisingTrackingEnabled: " + trackingEnabled + "\n" +
            "Note: IDFA is all-zero when ATT is denied/not determined.";
#else
        return "Platform: non-iOS (or Editor)\nIDFA is only available on iOS device.";
#endif
    }
}
#endif
