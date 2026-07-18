using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class h980220_ProjectComplianceTests
{
    private static readonly Regex ClassDeclarationPattern = new Regex(
        @"^\s*(?:(?:public|private|protected|internal|static|abstract|sealed|partial|new|unsafe)\s+)*class\s+([A-Za-z_]\w*)\b",
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

            foreach (Match match in ClassDeclarationPattern.Matches(File.ReadAllText(path)))
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
        Assert.That(settings, Does.Contain("activeInputHandler: 0"));
        Assert.That(manifest, Does.Not.Contain("com.unity.inputsystem"));
        Assert.That(File.Exists(Path.Combine(Application.dataPath, "InputSystem_Actions.inputactions")), Is.False);
        Assert.That(File.Exists(Path.Combine(Application.dataPath, "InputSystem_Actions.inputactions.meta")), Is.False);
    }
}
