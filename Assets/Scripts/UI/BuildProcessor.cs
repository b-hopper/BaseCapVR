#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        // Set version to "v0.0.{CurrentDate}"
        PlayerSettings.bundleVersion = $"v0.0.{System.DateTime.Now:yyyyMMdd}";
    }
}
#endif
