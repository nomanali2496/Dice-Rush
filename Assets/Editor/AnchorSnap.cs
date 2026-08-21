using UnityEditor;
using UnityEngine;

public class AnchorSnap : Editor
{
    /// <summary>
    /// Snaps the anchors of the selected UI element(s) to their current Rect bounds.
    /// Shortcut: Ctrl + Alt + S (Windows) / Cmd + Option + S (Mac)
    /// Also accessible from GameObject > UI > Snap Anchors to Corners
    /// </summary>
    [MenuItem("GameObject/UI/Snap Anchors to Corners %&s", false, -1)]
    static void SnapAnchors()
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("No UI object selected. Please select a RectTransform to snap its anchors.");
            return;
        }

        foreach (GameObject obj in Selection.gameObjects)
        {
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform == null) continue;

            RectTransform parent = rectTransform.parent as RectTransform;
            if (parent == null)
            {
                Debug.LogWarning($"Cannot snap anchors for '{obj.name}' because it has no RectTransform parent.");
                continue;
            }

            Undo.RecordObject(rectTransform, "Snap Anchors");

            Vector2 offsetMin = rectTransform.offsetMin;
            Vector2 offsetMax = rectTransform.offsetMax;
            Vector2 anchorMin = rectTransform.anchorMin;
            Vector2 anchorMax = rectTransform.anchorMax;

            float parentWidth = parent.rect.width;
            float parentHeight = parent.rect.height;

            if (parentWidth == 0 || parentHeight == 0)
            {
                Debug.LogWarning($"Parent of '{obj.name}' has 0 width or height, skipping anchor snap.");
                continue;
            }

            Vector2 anchorMinNew = new Vector2(
                anchorMin.x + (offsetMin.x / parentWidth),
                anchorMin.y + (offsetMin.y / parentHeight)
            );
            
            Vector2 anchorMaxNew = new Vector2(
                anchorMax.x + (offsetMax.x / parentWidth),
                anchorMax.y + (offsetMax.y / parentHeight)
            );

            rectTransform.anchorMin = anchorMinNew;
            rectTransform.anchorMax = anchorMaxNew;

            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
