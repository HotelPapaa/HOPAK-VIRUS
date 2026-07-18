using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class h980220_ProjectComplianceTests
{
    private static readonly Regex TypeDeclarationPattern = new Regex(
        @"^\s*(?:\[[^\r\n]+\]\s*)*(?:(?:public|private|protected|internal|static|abstract|sealed|partial|new|unsafe)\s+)*(?:class|interface|enum)\s+([A-Za-z_]\w*)\b",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    [Test]
    public void EveryAssetScriptUsesRequiredPrefix()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            Assert.That(fileName, Does.StartWith("h980220_"), path);

            foreach (Match match in TypeDeclarationPattern.Matches(File.ReadAllText(path)))
            {
                string className = match.Groups[1].Value;
                Assert.That(className, Does.StartWith("h980220_"), $"{path}: {className}");
            }
        }
    }

    [Test]
    public void LegacyInputIsExclusive()
    {
        string settings = File.ReadAllText(Path.Combine(ProjectRoot, "ProjectSettings", "ProjectSettings.asset"));
        string manifest = File.ReadAllText(Path.Combine(ProjectRoot, "Packages", "manifest.json"));
        string packageLock = File.ReadAllText(Path.Combine(ProjectRoot, "Packages", "packages-lock.json"));
        string editorBuildSettings = File.ReadAllText(Path.Combine(
            ProjectRoot, "ProjectSettings", "EditorBuildSettings.asset"));
        Assert.That(settings, Does.Contain("activeInputHandler: 0"));
        Assert.That(manifest, Does.Not.Contain("com.unity.inputsystem"));
        Assert.That(packageLock, Does.Not.Contain("com.unity.inputsystem"));
        Assert.That(editorBuildSettings, Does.Not.Contain("com.unity.input.settings.actions"));
        Assert.That(Directory.EnumerateFiles(Application.dataPath, "*.inputactions",
            SearchOption.AllDirectories), Is.Empty);
    }

    [Test]
    public void ProjectContainsNoExternalArtAudioAnimationOrForbiddenProductionTypes()
    {
        string[] prohibitedExtensions =
        {
            ".png", ".jpg", ".jpeg", ".tga", ".psd", ".fbx", ".obj", ".blend",
            ".wav", ".mp3", ".ogg", ".aif", ".aiff", ".anim", ".controller"
        };
        string[] prohibitedAssets = Directory.EnumerateFiles(
                Application.dataPath, "*", SearchOption.AllDirectories)
            .Where(path => prohibitedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            .Select(path => Path.GetRelativePath(ProjectRoot, path))
            .ToArray();
        Assert.That(prohibitedAssets, Is.Empty);

        string productionRoot = Path.Combine(Application.dataPath, "HOPAK VIRUS");
        string combinedProductionSource = string.Join("\n", Directory.EnumerateFiles(
                productionRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}"))
            .Select(File.ReadAllText));
        Assert.That(combinedProductionSource, Does.Not.Contain("UnityEngine.InputSystem"));
        Assert.That(combinedProductionSource, Does.Not.Contain("AudioSource"));
        Assert.That(combinedProductionSource, Does.Not.Contain("ParticleSystem"));
        Assert.That(combinedProductionSource, Does.Not.Contain("TrailRenderer"));
        Assert.That(combinedProductionSource, Does.Not.Contain("LineRenderer"));
    }

    [Test]
    public void FutureBuilderIsHeadlessAndWindowsBuildPreservesSavedScene()
    {
        string builderPath = Path.Combine(
            Application.dataPath, "HOPAK VIRUS", "Editor", "h980220_GameSceneBuilder.cs");
        string automationPath = Path.Combine(
            Application.dataPath, "HOPAK VIRUS", "Editor", "h980220_BuildAutomation.cs");
        string builderSource = File.ReadAllText(builderPath);
        string automationSource = File.ReadAllText(automationPath);

        Assert.That(builderSource, Does.Not.Contain("VisualCube(\"Head\""));
        Assert.That(builderSource, Does.Contain("exactly five collider-free Cube visuals"));

        int buildWindowsStart = automationSource.IndexOf(
            "public static void BuildWindows()", System.StringComparison.Ordinal);
        Assert.That(buildWindowsStart, Is.GreaterThanOrEqualTo(0));
        string buildWindowsSource = automationSource.Substring(buildWindowsStart);
        Assert.That(buildWindowsSource,
            Does.Not.Contain("h980220_GameSceneBuilder.BuildScene();"));
    }
}
