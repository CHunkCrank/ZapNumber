using UnityEditor;
using UnityEngine;

public class BuildScript
{
    public static void BuildIOS()
    {
        // iOS用のビルド設定
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = GetEnabledScenes();
        buildPlayerOptions.locationPathName = "ios"; // "ios"というフォルダに出力
        buildPlayerOptions.target = BuildTarget.iOS;
        buildPlayerOptions.options = BuildOptions.None;

        // ビルド実行
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        string[] enabledScenes = new string[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            enabledScenes[i] = scenes[i].path;
        }
        return enabledScenes;
    }
}