using UnityEditor;
using UnityEngine;

public class ClearPlayerPrefsTool
{
    [MenuItem("Tools/Clear PlayerPrefs")]
    public static void ClearPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("✅ <color=#00FF00>PlayerPrefs Cleared! Old tokens removed.</color>");
    }
}
