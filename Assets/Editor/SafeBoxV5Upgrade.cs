#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SafeBoxV5Upgrade
{
    [MenuItem("Tools/Safe Box/Apply v5 - Auto Open 0904 (No Text)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SafeBox] Stop Play Mode before applying v5.");
            return;
        }
        SafeBoxPuzzle puzzle = Object.FindFirstObjectByType<SafeBoxPuzzle>();
        if (puzzle == null)
        {
            Debug.LogError("[SafeBox] Open SafeBoxPuzzle.unity first.");
            return;
        }
        Undo.RecordObject(puzzle, "SafeBox v5 settings");
        puzzle.secretCode = "0904";
        EditorUtility.SetDirty(puzzle);
        if (puzzle.hintText != null) Hide(puzzle.hintText.gameObject);
        if (puzzle.controlsRoot != null) Hide(puzzle.controlsRoot);
        GameObject canvas = GameObject.Find("SafeBoxCanvas");
        if (canvas != null)
        {
            Transform title = canvas.transform.Find("Title");
            if (title != null) Hide(title.gameObject);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[SafeBox] v5 applied: no captions/buttons; auto unlock code 0904.");
    }
    private static void Hide(GameObject obj)
    {
        if (obj == null) return;
        Undo.RecordObject(obj, "Hide safe UI");
        obj.SetActive(false);
        EditorUtility.SetDirty(obj);
    }
}
#endif
