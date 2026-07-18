using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class h980220_BuildAutomation
{
    public static void RebuildScene()
    {
        h980220_GameSceneBuilder.BuildScene();
    }

    public static void BuildWindows()
    {
        h980220_GameSceneBuilder.BuildScene();
        PlayerSettings.SetManagedStrippingLevel(
            BuildTargetGroup.Standalone, ManagedStrippingLevel.High);

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { h980220_GameSceneBuilder.ScenePath },
            locationPathName = "Builds/Windows/HOPAK VIRUS.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Windows build failed: {report.summary.result} " +
                $"({report.summary.totalErrors} errors).");
        }
    }
}
