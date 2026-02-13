#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class IOSPostProcessATT
{
    [PostProcessBuild(0)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        plist.root.SetString(
            "NSUserTrackingUsageDescription",
            "This identifier may be used to deliver more relevant ads."
        );

        var iosAppId = ReadIosAppIdFromGoogleMobileAdsSettings();
        if (!string.IsNullOrEmpty(iosAppId))
        {
            plist.root.SetString("GADApplicationIdentifier", iosAppId);
        }

        File.WriteAllText(plistPath, plist.WriteToString());
    }

    private static string ReadIosAppIdFromGoogleMobileAdsSettings()
    {
        const string settingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        if (!File.Exists(settingsPath)) return string.Empty;

        foreach (var line in File.ReadAllLines(settingsPath))
        {
            const string key = "adMobIOSAppId:";
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(key)) continue;

            var value = trimmed.Substring(key.Length).Trim();
            return value;
        }

        return string.Empty;
    }
}
#endif
