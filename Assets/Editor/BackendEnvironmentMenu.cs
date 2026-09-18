using UnityEngine;
using UnityEditor;

/// Tools menu for switching backend environments without touching the Inspector.
public static class BackendEnvironmentMenu
{
    private const string MenuRoot      = "Tools/Dice Blitz/Environment/";
    private const string MenuDev       = MenuRoot + "Development";
    private const string MenuProd      = MenuRoot + "Production";
    private const string MenuOpenAsset = "Tools/Dice Blitz/Open Backend Config";

    // ---------- menu items ----------

    [MenuItem(MenuDev, priority = 1)]
    private static void SetDevelopment()
    {
        SetEnvironment(BackendConfig.AppEnvironment.Development);
    }

    [MenuItem(MenuProd, priority = 2)]
    private static void SetProduction()
    {
        SetEnvironment(BackendConfig.AppEnvironment.Production);
    }

    [MenuItem(MenuOpenAsset, priority = 20)]
    private static void OpenConfig()
    {
        var cfg = FindConfig();
        if (cfg == null) { CreateConfigPrompt(); return; }
        Selection.activeObject = cfg;
        EditorGUIUtility.PingObject(cfg);
    }

    // ---------- checkmarks ----------

    [MenuItem(MenuDev, validate = true)]
    private static bool ValidateDev()
    {
        var cfg = FindConfig();
        UnityEditor.Menu.SetChecked(MenuDev, cfg != null && cfg.activeEnvironment == BackendConfig.AppEnvironment.Development);
        return cfg != null;
    }

    [MenuItem(MenuProd, validate = true)]
    private static bool ValidateProd()
    {
        var cfg = FindConfig();
        UnityEditor.Menu.SetChecked(MenuProd, cfg != null && cfg.activeEnvironment == BackendConfig.AppEnvironment.Production);
        return cfg != null;
    }

    // ---------- helpers ----------

    internal static BackendConfig FindConfig()
    {
        var guids = AssetDatabase.FindAssets("t:BackendConfig");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<BackendConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    internal static void SetEnvironment(BackendConfig.AppEnvironment env)
    {
        var cfg = FindConfig();
        if (cfg == null) { CreateConfigPrompt(); return; }

        cfg.activeEnvironment = env;
        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BackendConfig] Switched to <b>{env}</b>  |  API: {cfg.ApiUrl}  |  Socket: {cfg.SocketUrl}");
    }

    private static void CreateConfigPrompt()
    {
        bool create = EditorUtility.DisplayDialog(
            "Backend Config Missing",
            "No BackendConfig asset found.\n\nCreate one via:\nAssets → Create → Dice Blitz → Backend Config\n\nThen place it in Assets/Resources/ for auto-loading at runtime.",
            "OK"
        );
    }
}
