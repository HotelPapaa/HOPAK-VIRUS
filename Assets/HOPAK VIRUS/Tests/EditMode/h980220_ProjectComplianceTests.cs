using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class h980220_ProjectComplianceTests
{
    private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    [Test]
    public void EveryAssetScriptUsesRequiredPrefix()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            Assert.That(fileName, Does.StartWith("h980220_"), path);
        }
    }

    [Test]
    public void LegacyInputIsExclusive()
    {
        string settings = File.ReadAllText(Path.Combine(ProjectRoot, "ProjectSettings", "ProjectSettings.asset"));
        string manifest = File.ReadAllText(Path.Combine(ProjectRoot, "Packages", "manifest.json"));
        Assert.That(settings, Does.Contain("activeInputHandler: 0"));
        Assert.That(manifest, Does.Not.Contain("com.unity.inputsystem"));
        Assert.That(File.Exists(Path.Combine(Application.dataPath, "InputSystem_Actions.inputactions")), Is.False);
    }
}
