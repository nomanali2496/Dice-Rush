using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Intercepts Android and iOS builds and prompts the developer to switch to
/// Production if the active environment is still Development.
public class BackendBuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        var target = report.summary.platform;
        if (target != BuildTarget.Android && target != BuildTarget.iOS)
            return;

        var cfg = BackendEnvironmentMenu.FindConfig();
        if (cfg == null)
        {
            Debug.LogWarning("[BackendConfig] BackendConfig asset not found — skipping environment check.");
            return;
        }

        if (cfg.activeEnvironment == BackendConfig.AppEnvironment.Production)
        {
            Debug.Log($"[BackendConfig] Build target: {target} | Environment: Production  ({cfg.ApiUrl})");
            return;
        }

        // Currently on Development — ask the developer what to do.
        int choice = EditorUtility.DisplayDialogComplex(
            "Backend Environment: Development",
            $"You are building for {target} but the backend is pointed at the DEVELOPMENT environment.\n\n" +
            $"  API URL:    {cfg.ApiUrl}\n" +
            $"  Socket URL: {cfg.SocketUrl}\n\n" +
            "Switch to Production before building?",
            "Switch to Production",   // 0
            "Cancel Build",           // 1
            "Keep Development"        // 2
        );

        switch (choice)
        {
            case 0:
                BackendEnvironmentMenu.SetEnvironment(BackendConfig.AppEnvironment.Production);
                Debug.Log("[BackendConfig] Switched to Production — proceeding with build.");
                break;

            case 1:
                // Throwing BuildFailedException cancels the build cleanly.
                throw new BuildFailedException("[BackendConfig] Build cancelled — environment not changed.");

            case 2:
                Debug.LogWarning($"[BackendConfig] Building {target} with DEVELOPMENT backend: {cfg.ApiUrl}");
                break;
        }
    }
}
