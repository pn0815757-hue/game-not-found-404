#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One-click, non-destructive setup for the existing SafeBoxPuzzle scene.</summary>
public static class SafeBoxBookFlipSetup
{
    private const string SpriteFolder = "Assets/Sprites/SafeBox/";
    private const string ClosedFile = "Sổ tay da cũ phong cách pixel art.png";
    private const string OpenedFile = "Sổ phòng pixel art cổ điển.png";

    [MenuItem("Tools/Safe Box/Install Book Opening Animation")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[SafeBox] Exit Play Mode first.");
            return;
        }
        SafeBoxPuzzle puzzle = Object.FindFirstObjectByType<SafeBoxPuzzle>(FindObjectsInactive.Include);
        if (puzzle == null || puzzle.bookViewPanel == null || puzzle.bookViewImage == null)
        {
            Debug.LogError("[SafeBox] Open Assets/Scenes/SafeBoxPuzzle.unity and run Tools > Safe Box > Update Current Scene (English + Click) first, if book view is missing.");
            return;
        }

        Sprite closed = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + ClosedFile);
        Sprite opened = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + OpenedFile);
        if (closed == null || opened == null)
        {
            Debug.LogError("[SafeBox] One or both book sprites were not imported as Sprite (2D and UI). Check Assets/Sprites/SafeBox.");
            return;
        }

        GameObject panel = puzzle.bookViewPanel;
        SafeBoxBookFlip flip = panel.GetComponent<SafeBoxBookFlip>();
        if (flip == null) flip = Undo.AddComponent<SafeBoxBookFlip>(panel);
        RectTransform imageRect = puzzle.bookViewImage.rectTransform;
        Undo.RecordObject(imageRect, "Normalize book display bounds");
        imageRect.anchorMin = imageRect.anchorMax = new Vector2(.5f, .5f);
        imageRect.pivot = new Vector2(.5f, .5f);
        imageRect.anchoredPosition = new Vector2(0f, 20f);
        imageRect.sizeDelta = new Vector2(672f, 820f);
        imageRect.localScale = Vector3.one;
        imageRect.localRotation = Quaternion.identity;

        Undo.RecordObject(puzzle.bookViewImage, "Assign closed book sprite");
        puzzle.bookViewImage.sprite = closed;
        puzzle.bookViewImage.preserveAspect = true;

        // A transparent, foreground button intercepts clicks before the old
        // BookCloseup button (which currently calls CloseBookView()).
        Transform found = panel.transform.Find("BookFlipClickSurface");
        GameObject clickObj;
        if (found == null)
        {
            clickObj = new GameObject("BookFlipClickSurface", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(clickObj, "Create book flip click surface");
            clickObj.transform.SetParent(panel.transform, false);
        }
        else clickObj = found.gameObject;

        RectTransform clickRect = clickObj.GetComponent<RectTransform>();
        Undo.RecordObject(clickRect, "Match click area to book image");
        clickRect.anchorMin = clickRect.anchorMax = new Vector2(.5f, .5f);
        clickRect.pivot = new Vector2(.5f, .5f);
        clickRect.anchoredPosition = imageRect.anchoredPosition;
        clickRect.sizeDelta = imageRect.sizeDelta;
        clickRect.localScale = Vector3.one;
        // Cover sits above the base image; invisible click surface sits above cover.
        Transform coverFound = panel.transform.Find("BookTurningCover");
        GameObject coverGo;
        if (coverFound == null)
        {
            coverGo = new GameObject("BookTurningCover", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(coverGo, "Create turning book cover");
            coverGo.transform.SetParent(panel.transform, false);
        }
        else coverGo = coverFound.gameObject;
        RectTransform coverRt = coverGo.GetComponent<RectTransform>();
        coverRt.anchorMin = coverRt.anchorMax = new Vector2(.5f, .5f);
        coverRt.pivot = new Vector2(0f, .5f);
        coverRt.sizeDelta = imageRect.sizeDelta;
        coverRt.anchoredPosition = imageRect.anchoredPosition + new Vector2(-imageRect.sizeDelta.x * .5f, 0f);
        Image coverImg = coverGo.GetComponent<Image>();
        coverImg.sprite = closed;
        coverImg.preserveAspect = true;
        coverImg.raycastTarget = false;
        coverGo.SetActive(false);
        coverGo.transform.SetAsLastSibling();
        clickObj.transform.SetAsLastSibling();
        Image hitImage = clickObj.GetComponent<Image>();
        Undo.RecordObject(hitImage, "Make book click target transparent");
        hitImage.color = Color.clear;
        hitImage.raycastTarget = true;
        Button hitButton = clickObj.GetComponent<Button>();
        Undo.RecordObject(hitButton, "Set book click interaction");
        hitButton.transition = Selectable.Transition.None;
        hitButton.onClick.RemoveAllListeners();

        Undo.RecordObject(flip, "Configure book opening flip");
        flip.bookImage = puzzle.bookViewImage;
        flip.coverImage = coverImg;
        flip.closedBookSprite = closed;
        flip.openedBookSprite = opened;
        flip.clickSurface = hitButton;

        // Force the existing initial zoom to use the closed-book image.
        Undo.RecordObject(puzzle, "Set book closeup image");
        puzzle.bookCloseupSprite = closed;

        EditorUtility.SetDirty(coverImg);
        EditorUtility.SetDirty(flip);
        EditorUtility.SetDirty(puzzle);
        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(panel.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[SafeBox] Book flip installed. Click closed book to open; click again to close. Click dark overlay or press Escape to leave the book view.");
    }
}
#endif
