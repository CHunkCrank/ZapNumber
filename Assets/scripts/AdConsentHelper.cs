using UnityEngine;

#if UNITY_IOS && !UNITY_EDITOR
using Unity.Advertisement.IosSupport;
#endif

public static class AdConsentHelper
{
    private static bool _requested;

    public static void RequestTrackingAuthorizationIfNeeded()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (_requested) return;
        _requested = true;

        ATTrackingStatusBinding.RequestAuthorizationTracking();
        Debug.Log("ATT authorization request triggered.");
#endif
    }
}
