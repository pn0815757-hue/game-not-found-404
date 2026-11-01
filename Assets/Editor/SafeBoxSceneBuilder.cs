#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds an editable Unity scene from the supplied original PNGs.</summary>
[InitializeOnLoad]
public static class SafeBoxSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/SafeBoxPuzzle.unity";
    private const string SpritePath = "Assets/Sprites/SafeBox/";
    private const string NewOpenedBoxName = "Chiếc hộp cổ và bí ẩn.png";
    private const string BookCloseupName = "Sổ tay da cũ phong cách pixel art.png";
    private static bool attempted;

    static SafeBoxSceneBuilder()
    {
        EditorApplication.delayCall += TryAutoCreate;
    }

    private static void TryAutoCreate()
    {
        if (attempted || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        attempted = true;
        // Never overwrite an existing scene or disturb a modified active scene.
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            Debug.Log("[SafeBox] Save your current scene, then choose Tools > Safe Box > Create Demo Scene.");
            return;
        }
        CreateScene();
    }

    [MenuItem("Tools/Safe Box/Update Current Scene (English + Click)")]
    public static void UpdateCurrentScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[SafeBox] Exit Play Mode before updating the scene.");
            return;
        }
        SafeBoxPuzzle puzzle = UnityEngine.Object.FindFirstObjectByType<SafeBoxPuzzle>();
        if (puzzle == null)
        {
            Debug.LogError("[SafeBox] Open SafeBoxPuzzle.unity first, then run this menu command.");
            return;
        }
        Undo.RecordObject(puzzle, "Update safe labels and book feature");

        Sprite newOpened = LoadSpriteWithFallback(NewOpenedBoxName, "BoxOpened.png");
        Sprite bookCloseup = LoadSprite(BookCloseupName);
        if (newOpened != null) puzzle.openedBox = newOpened;
        if (bookCloseup != null) puzzle.bookCloseupSprite = bookCloseup;

        if (puzzle.hintText != null)
        {
            Undo.RecordObject(puzzle.hintText, "Update safe hint");
            puzzle.hintText.text = "CLICK EACH DIGIT TO INCREASE  |  CHECK TO UNLOCK";
            EditorUtility.SetDirty(puzzle.hintText);
        }
        Transform canvas = puzzle.transform.root.Find("SafeBoxCanvas");
        if (canvas == null)
        {
            GameObject canvasObj = GameObject.Find("SafeBoxCanvas");
            if (canvasObj != null) canvas = canvasObj.transform;
        }
        if (canvas != null)
        {
            UpdateText(canvas, "Title", "FOUR-DIGIT SAFE");
            UpdateText(canvas, "Controls/CheckButton/Label", "CHECK");
            UpdateText(canvas, "Controls/ResetButton/Label", "RESET");

            // Setup or link Box CanvasGroup
            Transform boxRoot = canvas.Find("SafeBox");
            if (boxRoot != null)
            {
                CanvasGroup cg = boxRoot.GetComponent<CanvasGroup>();
                if (cg == null) cg = Undo.AddComponent<CanvasGroup>(boxRoot.gameObject);
                puzzle.boxCanvasGroup = cg;

                // Setup BookInsideButton
                Transform bookBtnT = boxRoot.Find("BookInsideButton");
                GameObject bookBtnGo;
                if (bookBtnT == null)
                {
                    bookBtnGo = new GameObject("BookInsideButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    Undo.RegisterCreatedObjectUndo(bookBtnGo, "Create book inside button");
                    bookBtnGo.transform.SetParent(boxRoot, false);
                }
                else bookBtnGo = bookBtnT.gameObject;

                RectTransform bRt = bookBtnGo.GetComponent<RectTransform>();
                Undo.RecordObject(bRt, "Configure book inside button RT");
                bRt.anchorMin = bRt.anchorMax = new Vector2(0.5f, 0.5f);
                bRt.pivot = new Vector2(0.5f, 0.5f);
                bRt.sizeDelta = new Vector2(380, 200);
                bRt.anchoredPosition = new Vector2(0, 15);

                Image bImg = bookBtnGo.GetComponent<Image>();
                Undo.RecordObject(bImg, "Configure book inside button Img");
                bImg.color = new Color(0, 0, 0, 0);
                bImg.raycastTarget = true;

                Button bBtn = bookBtnGo.GetComponent<Button>();
                Undo.RecordObject(bBtn, "Configure book inside button Btn");
                bBtn.transition = Selectable.Transition.None;
                for (int j = bBtn.onClick.GetPersistentEventCount() - 1; j >= 0; --j)
                    UnityEventTools.RemovePersistentListener(bBtn.onClick, j);
                UnityEventTools.AddPersistentListener(bBtn.onClick, puzzle.OpenBookView);
                bookBtnGo.SetActive(false);
                puzzle.bookInsideButton = bBtn;
            }

            // Setup DimOverlay
            Transform dimT = canvas.Find("DimOverlay");
            GameObject dimGo;
            if (dimT == null)
            {
                dimGo = new GameObject("DimOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
                Undo.RegisterCreatedObjectUndo(dimGo, "Create dim overlay");
                dimGo.transform.SetParent(canvas, false);
            }
            else dimGo = dimT.gameObject;

            RectTransform dimRt = dimGo.GetComponent<RectTransform>();
            Undo.RecordObject(dimRt, "Configure dim overlay RT");
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.sizeDelta = Vector2.zero;
            dimRt.anchoredPosition = Vector2.zero;

            Image dimImg = dimGo.GetComponent<Image>();
            Undo.RecordObject(dimImg, "Configure dim overlay Img");
            dimImg.color = new Color(0.02f, 0.02f, 0.03f, 0.8f);
            dimImg.raycastTarget = true;

            Button dimBtn = dimGo.GetComponent<Button>();
            Undo.RecordObject(dimBtn, "Configure dim overlay Btn");
            dimBtn.transition = Selectable.Transition.None;
            for (int j = dimBtn.onClick.GetPersistentEventCount() - 1; j >= 0; --j)
                UnityEventTools.RemovePersistentListener(dimBtn.onClick, j);
            UnityEventTools.AddPersistentListener(dimBtn.onClick, puzzle.CloseBookView);
            dimGo.SetActive(false);
            puzzle.dimOverlayImage = dimImg;

            // Setup BookViewPanel
            Transform bvT = canvas.Find("BookViewPanel");
            GameObject bvGo;
            if (bvT == null)
            {
                bvGo = new GameObject("BookViewPanel", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(bvGo, "Create book view panel");
                bvGo.transform.SetParent(canvas, false);
            }
            else bvGo = bvT.gameObject;

            RectTransform bvRt = bvGo.GetComponent<RectTransform>();
            Undo.RecordObject(bvRt, "Configure book view panel RT");
            bvRt.anchorMin = Vector2.zero;
            bvRt.anchorMax = Vector2.one;
            bvRt.sizeDelta = Vector2.zero;
            bvRt.anchoredPosition = Vector2.zero;

            CanvasGroup bvCg = bvGo.GetComponent<CanvasGroup>();
            if (bvCg == null) bvCg = Undo.AddComponent<CanvasGroup>(bvGo);

            Transform closeupT = bvGo.transform.Find("BookCloseup");
            GameObject closeupGo;
            if (closeupT == null)
            {
                closeupGo = new GameObject("BookCloseup", typeof(RectTransform), typeof(Image), typeof(Button));
                Undo.RegisterCreatedObjectUndo(closeupGo, "Create book closeup");
                closeupGo.transform.SetParent(bvGo.transform, false);
            }
            else closeupGo = closeupT.gameObject;

            RectTransform cRt = closeupGo.GetComponent<RectTransform>();
            Undo.RecordObject(cRt, "Configure book closeup RT");
            cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.sizeDelta = new Vector2(672, 820);
            cRt.anchoredPosition = new Vector2(0, 20);

            Image cImg = closeupGo.GetComponent<Image>();
            Undo.RecordObject(cImg, "Configure book closeup Img");
            if (bookCloseup != null) cImg.sprite = bookCloseup;
            cImg.preserveAspect = true;
            cImg.raycastTarget = true;

            Button cBtn = closeupGo.GetComponent<Button>();
            Undo.RecordObject(cBtn, "Configure book closeup Btn");
            cBtn.transition = Selectable.Transition.None;
            for (int j = cBtn.onClick.GetPersistentEventCount() - 1; j >= 0; --j)
                UnityEventTools.RemovePersistentListener(cBtn.onClick, j);
            UnityEventTools.AddPersistentListener(cBtn.onClick, puzzle.CloseBookView);

            Transform hintT = bvGo.transform.Find("CloseHint");
            if (hintT == null)
            {
                Text hint = MakeText("CloseHint", bvGo.transform, "CLICK TO CLOSE", 24, new Vector2(500, 40), new Vector2(0, -430), new Color(0.85f, 0.8f, 0.72f, 0.8f));
                hint.raycastTarget = false;
            }

            bvGo.SetActive(false);
            puzzle.bookViewPanel = bvGo;
            puzzle.bookViewImage = cImg;
            puzzle.closeBookButton = cBtn;

            // Ensure hierarchy ordering: SafeBox < DimOverlay < BookViewPanel
            if (boxRoot != null) boxRoot.SetSiblingIndex(1);
            dimGo.transform.SetAsLastSibling();
            bvGo.transform.SetAsLastSibling();
        }

        for (int i = 0; i < puzzle.digitImages.Length; ++i)
        {
            Image digit = puzzle.digitImages[i];
            if (digit == null) continue;
            Button button = digit.GetComponent<Button>();
            if (button == null) button = Undo.AddComponent<Button>(digit.gameObject);
            Undo.RecordObject(button, "Enable digit click");
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();
            for (int j = button.onClick.GetPersistentEventCount() - 1; j >= 0; --j)
                UnityEventTools.RemovePersistentListener(button.onClick, j);
            UnityEventTools.AddIntPersistentListener(button.onClick, puzzle.CycleDigit, i);
            digit.raycastTarget = true;
            EditorUtility.SetDirty(digit);
            EditorUtility.SetDirty(button);
        }
        RepairDigitTiles(puzzle);
        EditorUtility.SetDirty(puzzle);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[SafeBox] Updated scene: New opened box with documents + Book view overlay.");
    }

    [MenuItem("Tools/Safe Box/Repair Digit Alignment (v4)")]
    public static void RepairCurrentDigits()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[SafeBox] Stop Play Mode first.");
            return;
        }
        SafeBoxPuzzle puzzle = UnityEngine.Object.FindFirstObjectByType<SafeBoxPuzzle>();
        if (puzzle == null) { Debug.LogError("[SafeBox] Open the SafeBoxPuzzle scene first."); return; }
        RepairDigitTiles(puzzle);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[SafeBox] Digit alignment repaired.");
    }

    [MenuItem("Tools/Safe Box/Preview Opened Safe (In Editor)")]
    public static void PreviewOpenedSafe()
    {
        SafeBoxPuzzle puzzle = UnityEngine.Object.FindFirstObjectByType<SafeBoxPuzzle>();
        if (puzzle == null) { Debug.LogError("[SafeBox] Open the SafeBoxPuzzle scene first."); return; }
        if (puzzle.boxImage != null && puzzle.openedBox != null)
        {
            Undo.RecordObject(puzzle.boxImage, "Preview opened safe");
            puzzle.boxImage.sprite = puzzle.openedBox;
        }
        for (int i = 0; i < puzzle.digitImages.Length; i++)
        {
            if (puzzle.digitImages[i] != null)
            {
                Undo.RecordObject(puzzle.digitImages[i].rectTransform, "Preview opened digit pos");
                puzzle.digitImages[i].rectTransform.anchoredPosition = puzzle.GetOpenedPosition(i);
                puzzle.digitImages[i].gameObject.SetActive(true);
            }
        }
        CloseBookPreview();
        Debug.Log("[SafeBox] Previewing Opened Safe in Scene view.");
    }

    [MenuItem("Tools/Safe Box/Preview Closed Safe (In Editor)")]
    public static void PreviewClosedSafe()
    {
        SafeBoxPuzzle puzzle = UnityEngine.Object.FindFirstObjectByType<SafeBoxPuzzle>();
        if (puzzle == null) { Debug.LogError("[SafeBox] Open the SafeBoxPuzzle scene first."); return; }
        if (puzzle.boxImage != null && puzzle.closedBox != null)
        {
            Undo.RecordObject(puzzle.boxImage, "Preview closed safe");
            puzzle.boxImage.sprite = puzzle.closedBox;
        }
        for (int i = 0; i < puzzle.digitImages.Length; i++)
        {
            if (puzzle.digitImages[i] != null)
            {
                Undo.RecordObject(puzzle.digitImages[i].rectTransform, "Preview closed digit pos");
                puzzle.digitImages[i].rectTransform.anchoredPosition = puzzle.GetClosedPosition(i);
                puzzle.digitImages[i].gameObject.SetActive(true);
            }
        }
        CloseBookPreview();
        Debug.Log("[SafeBox] Previewing Closed Safe in Scene view.");
    }

    [MenuItem("Tools/Safe Box/Preview Book View (In Editor)")]
    public static void PreviewBookView()
    {
        SafeBoxPuzzle puzzle = UnityEngine.Object.FindFirstObjectByType<SafeBoxPuzzle>();
        if (puzzle == null) { Debug.LogError("[SafeBox] Open the SafeBoxPuzzle scene first."); return; }
        Undo.RecordObject(puzzle, "Preview book view");
        if (puzzle.bookViewPanel != null)
        {
            puzzle.bookViewPanel.SetActive(true);
            CanvasGroup cg = puzzle.bookViewPanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
            Transform t = puzzle.bookViewPanel.transform.Find("BookCloseup");
            if (t != null)
            {
                t.localScale = Vector3.one;
                ((RectTransform)t).anchoredPosition = new Vector2(0, 20f);
            }
        }
        if (puzzle.dimOverlayImage != null)
        {
            puzzle.dimOverlayImage.gameObject.SetActive(true);
            Color c = puzzle.dimOverlayImage.color;
            c.a = 0.82f;
            puzzle.dimOverlayImage.color = c;
        }
        if (puzzle.boxCanvasGroup != null)
        {
            puzzle.boxCanvasGroup.alpha = 0.22f;
            puzzle.boxCanvasGroup.blocksRaycasts = false;
        }
        Debug.Log("[SafeBox] Previewing Book View on main screen.");
    }

    [MenuItem("Tools/Safe Box/Close Book Preview (In Editor)")]
    public static void CloseBookPreview()
    {
        SafeBoxPuzzle puzzle = UnityEngine.Object.FindFirstObjectByType<SafeBoxPuzzle>();
        if (puzzle == null) { Debug.LogError("[SafeBox] Open the SafeBoxPuzzle scene first."); return; }
        Undo.RecordObject(puzzle, "Close book preview");
        if (puzzle.bookViewPanel != null) puzzle.bookViewPanel.SetActive(false);
        if (puzzle.dimOverlayImage != null) puzzle.dimOverlayImage.gameObject.SetActive(false);
        if (puzzle.boxCanvasGroup != null)
        {
            puzzle.boxCanvasGroup.alpha = 1f;
            puzzle.boxCanvasGroup.blocksRaycasts = true;
        }
        Debug.Log("[SafeBox] Closed Book View preview.");
    }

    private static Sprite LoadSpriteWithFallback(string primaryName, string fallbackName)
    {
        Sprite s = LoadSprite(primaryName);
        if (s != null) return s;
        if (!string.IsNullOrEmpty(fallbackName)) return LoadSprite(fallbackName);
        return null;
    }

    private static void RepairDigitTiles(SafeBoxPuzzle puzzle)
    {
        Undo.RecordObject(puzzle, "Update safe puzzle positions");
        puzzle.closedDigitPositions = new Vector2[4]
        {
            new Vector2(-129f, -173.5f),
            new Vector2(-30f, -173.5f),
            new Vector2(69f, -173.5f),
            new Vector2(168f, -173.5f)
        };
        puzzle.openedDigitPositions = new Vector2[4]
        {
            new Vector2(-149f, -201f),
            new Vector2(-51.5f, -201f),
            new Vector2(44.5f, -201f),
            new Vector2(142f, -201f)
        };
        EditorUtility.SetDirty(puzzle);

        for (int i = 0; i < 4 && i < puzzle.digitImages.Length; ++i)
        {
            Image digit = puzzle.digitImages[i];
            if (digit == null) continue;
            RectTransform rt = digit.rectTransform;
            Undo.RecordObject(rt, "Align safe digit");
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = new Vector2(80, 108);
            rt.anchoredPosition = puzzle.GetClosedPosition(i);
            digit.preserveAspect = false;
            digit.type = Image.Type.Simple;
            digit.color = Color.white;
            EditorUtility.SetDirty(rt);
            EditorUtility.SetDirty(digit);
        }
        // Find English buttons through their components even when nesting differs.
        GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject go in all)
        {
            if (go.name != "CheckButton" && go.name != "ResetButton") continue;
            Text label = go.GetComponentInChildren<Text>(true);
            if (label == null) continue;
            Undo.RecordObject(label, "Translate safe button");
            label.text = go.name == "CheckButton" ? "CHECK" : "RESET";
            EditorUtility.SetDirty(label);
        }
    }

    private static void UpdateText(Transform root, string path, string value)
    {
        Transform target = root.Find(path);
        if (target == null) return;
        Text text = target.GetComponent<Text>();
        if (text == null) return;
        Undo.RecordObject(text, "Translate safe text");
        text.text = value;
        EditorUtility.SetDirty(text);
    }

    [MenuItem("Tools/Safe Box/Create Demo Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlaying) return;
        if (EditorSceneManager.GetActiveScene().isDirty &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Sprite closed = LoadSprite("BoxClosed.png");
        Sprite opened = LoadSpriteWithFallback(NewOpenedBoxName, "BoxOpened.png");
        Sprite bookCloseup = LoadSprite(BookCloseupName);
        Sprite[] numbers = new Sprite[10];
        for (int i = 0; i < 10; ++i) numbers[i] = LoadSprite("Number" + i + ".png");
        if (closed == null || opened == null || Array.Exists(numbers, s => s == null))
        {
            Debug.LogError("[SafeBox] Missing sprites! Check Assets/Sprites/SafeBox and set Texture Type to Sprite (2D and UI).");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Camera mainCamera = Camera.main;
        if (mainCamera != null) mainCamera.backgroundColor = new Color(0.035f, 0.037f, 0.045f);

        GameObject canvasGo = new GameObject("SafeBoxCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image background = MakeImage("Background", canvasGo.transform, null, new Vector2(1920, 1080), Vector2.zero);
        background.color = new Color(0.055f, 0.053f, 0.059f, 1f);

        GameObject boxRoot = new GameObject("SafeBox", typeof(RectTransform), typeof(CanvasGroup));
        boxRoot.transform.SetParent(canvasGo.transform, false);
        RectTransform boxRT = boxRoot.GetComponent<RectTransform>();
        boxRT.sizeDelta = new Vector2(1128, 753);
        boxRT.anchoredPosition = new Vector2(0, 70);
        Image box = MakeImage("BoxImage", boxRoot.transform, closed, new Vector2(1128, 753), Vector2.zero);
        box.preserveAspect = true;

        GameObject puzzleGo = new GameObject("SafeBoxManager", typeof(SafeBoxPuzzle));
        SafeBoxPuzzle puzzle = puzzleGo.GetComponent<SafeBoxPuzzle>();
        puzzle.boxImage = box;
        puzzle.closedBox = closed;
        puzzle.openedBox = opened;
        puzzle.bookCloseupSprite = bookCloseup;
        puzzle.boxCanvasGroup = boxRoot.GetComponent<CanvasGroup>();
        puzzle.digitSprites = numbers;
        puzzle.digitImages = new Image[4];
        puzzle.secretCode = "0904";
        puzzle.closedDigitPositions = new Vector2[4]
        {
            new Vector2(-129f, -173.5f),
            new Vector2(-30f, -173.5f),
            new Vector2(69f, -173.5f),
            new Vector2(168f, -173.5f)
        };
        puzzle.openedDigitPositions = new Vector2[4]
        {
            new Vector2(-149f, -201f),
            new Vector2(-51.5f, -201f),
            new Vector2(44.5f, -201f),
            new Vector2(142f, -201f)
        };

        // PNGs already include '0000'. The numbered metal tile sprites overlay those four tiles.
        for (int i = 0; i < 4; ++i)
        {
            int index = i;
            Vector2 pos = puzzle.GetClosedPosition(i);
            GameObject digitGo = new GameObject("Digit" + i, typeof(RectTransform), typeof(Image), typeof(Button));
            digitGo.transform.SetParent(boxRoot.transform, false);
            RectTransform rt = digitGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 108);
            rt.anchoredPosition = pos;
            Image digitImage = digitGo.GetComponent<Image>();
            digitImage.sprite = numbers[0];
            digitImage.preserveAspect = false;
            digitImage.raycastTarget = true;
            Button button = digitGo.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            UnityEventTools.AddIntPersistentListener(button.onClick, puzzle.CycleDigit, index);
            puzzle.digitImages[i] = digitImage;
        }

        // Invisible click area on top of the book inside the opened chest
        GameObject bookBtnGo = new GameObject("BookInsideButton", typeof(RectTransform), typeof(Image), typeof(Button));
        bookBtnGo.transform.SetParent(boxRoot.transform, false);
        RectTransform bRt = bookBtnGo.GetComponent<RectTransform>();
        bRt.sizeDelta = new Vector2(380, 200);
        bRt.anchoredPosition = new Vector2(0, 15);
        Image bImg = bookBtnGo.GetComponent<Image>();
        bImg.color = new Color(0, 0, 0, 0);
        bImg.raycastTarget = true;
        Button bBtn = bookBtnGo.GetComponent<Button>();
        bBtn.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(bBtn.onClick, puzzle.OpenBookView);
        bookBtnGo.SetActive(false);
        puzzle.bookInsideButton = bBtn;

        // Dim overlay to blur/darken chest
        GameObject dimGo = new GameObject("DimOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
        dimGo.transform.SetParent(canvasGo.transform, false);
        RectTransform dimRt = dimGo.GetComponent<RectTransform>();
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.sizeDelta = Vector2.zero;
        dimRt.anchoredPosition = Vector2.zero;
        Image dimImg = dimGo.GetComponent<Image>();
        dimImg.color = new Color(0.02f, 0.02f, 0.03f, 0.8f);
        dimImg.raycastTarget = true;
        Button dimBtn = dimGo.GetComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(dimBtn.onClick, puzzle.CloseBookView);
        dimGo.SetActive(false);
        puzzle.dimOverlayImage = dimImg;

        // Book closeup panel
        GameObject bvGo = new GameObject("BookViewPanel", typeof(RectTransform), typeof(CanvasGroup));
        bvGo.transform.SetParent(canvasGo.transform, false);
        RectTransform bvRt = bvGo.GetComponent<RectTransform>();
        bvRt.anchorMin = Vector2.zero;
        bvRt.anchorMax = Vector2.one;
        bvRt.sizeDelta = Vector2.zero;
        bvRt.anchoredPosition = Vector2.zero;

        GameObject closeupGo = new GameObject("BookCloseup", typeof(RectTransform), typeof(Image), typeof(Button));
        closeupGo.transform.SetParent(bvGo.transform, false);
        RectTransform cRt = closeupGo.GetComponent<RectTransform>();
        cRt.sizeDelta = new Vector2(672, 820);
        cRt.anchoredPosition = new Vector2(0, 20);
        Image cImg = closeupGo.GetComponent<Image>();
        cImg.sprite = bookCloseup;
        cImg.preserveAspect = true;
        cImg.raycastTarget = true;
        Button cBtn = closeupGo.GetComponent<Button>();
        cBtn.transition = Selectable.Transition.None;
        UnityEventTools.AddPersistentListener(cBtn.onClick, puzzle.CloseBookView);

        MakeText("CloseHint", bvGo.transform, "CLICK TO CLOSE", 24, new Vector2(500, 40), new Vector2(0, -430), new Color(0.85f, 0.8f, 0.72f, 0.8f));
        bvGo.SetActive(false);
        puzzle.bookViewPanel = bvGo;
        puzzle.bookViewImage = cImg;
        puzzle.closeBookButton = cBtn;

        GameObject controls = new GameObject("Controls", typeof(RectTransform));
        controls.transform.SetParent(canvasGo.transform, false);
        RectTransform controlsRT = controls.GetComponent<RectTransform>();
        controlsRT.sizeDelta = new Vector2(900, 140);
        controlsRT.anchoredPosition = new Vector2(0, -400);
        puzzle.controlsRoot = controls;

        MakeButton("CheckButton", controls.transform, "CHECK", new Vector2(260, 80), new Vector2(-155, 0), puzzle.CheckCode);
        MakeButton("ResetButton", controls.transform, "RESET", new Vector2(260, 80), new Vector2(155, 0), puzzle.ResetPuzzle);
        puzzle.hintText = MakeText("Hint", canvasGo.transform, "CLICK EACH DIGIT TO INCREASE  |  CHECK TO UNLOCK", 32, new Vector2(1550, 85), new Vector2(0, -315), new Color(0.92f, 0.81f, 0.67f));
        MakeText("Title", canvasGo.transform, "FOUR-DIGIT SAFE", 38, new Vector2(1200, 75), new Vector2(0, 480), new Color(0.82f, 0.74f, 0.65f));
        controls.SetActive(false);
        puzzle.hintText.gameObject.SetActive(false);
        Transform titleObj = canvasGo.transform.Find("Title");
        if (titleObj != null) titleObj.gameObject.SetActive(false);

        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetAsLastSibling();
        }
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath);
        Selection.activeGameObject = puzzleGo;
        Debug.Log("[SafeBox] Created: " + ScenePath + " (test password: 0904). Click Play, then click digit tiles.");
    }

    private static Sprite LoadSprite(string filename)
    {
        string path = SpritePath + filename;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point || importer.textureCompression != TextureImporterCompression.Uncompressed))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Image MakeImage(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 position)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private static Font GetFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static Text MakeText(string name, Transform parent, string content, int size, Vector2 rectSize, Vector2 pos, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = rectSize;
        rt.anchoredPosition = pos;
        Text text = obj.GetComponent<Text>();
        text.font = GetFont();
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = content;
        text.raycastTarget = false;
        return text;
    }

    private static void MakeButton(string name, Transform parent, string label, Vector2 size, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        Image bg = obj.GetComponent<Image>();
        bg.color = new Color(0.23f, 0.19f, 0.17f);
        Button button = obj.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, onClick);
        MakeText("Label", obj.transform, label, 30, size, Vector2.zero, new Color(0.96f, 0.84f, 0.68f));
    }
}
#endif
