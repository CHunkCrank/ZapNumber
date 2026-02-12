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

        // ATTの理由文（必須）
        plist.root.SetString(
            "NSUserTrackingUsageDescription",
            "広告の最適化のために端末の識別子を使用して広告を表示する場合があります。"
        );

        File.WriteAllText(plistPath, plist.WriteToString());
    }
}
#endif
