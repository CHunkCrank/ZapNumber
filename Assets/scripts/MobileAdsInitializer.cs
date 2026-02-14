using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;

public static class MobileAdsInitializer
{
    private static bool _isInitializing;
    private static bool _isInitialized;
    private static readonly List<Action> _pending = new List<Action>();

    public static void Initialize(Action onInitialized)
    {
        if (onInitialized != null)
        {
            _pending.Add(onInitialized);
        }

        if (_isInitialized)
        {
            FlushPending();
            return;
        }

        if (_isInitializing) return;

        _isInitializing = true;

        MobileAds.Initialize(_ =>
        {
            _isInitializing = false;
            _isInitialized = true;
            FlushPending();
        });
    }

    private static void FlushPending()
    {
        if (_pending.Count == 0) return;

        var callbacks = _pending.ToArray();
        _pending.Clear();

        foreach (var callback in callbacks)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"MobileAdsInitializer callback error: {e.Message}");
            }
        }
    }
}
