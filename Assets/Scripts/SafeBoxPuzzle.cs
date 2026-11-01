using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Standalone four-digit safe puzzle, suitable for a shared Unity 6 project.</summary>
public class SafeBoxPuzzle : MonoBehaviour
{
    public Image boxImage;
    public Sprite closedBox;
    public Sprite openedBox;
    public Image[] digitImages = new Image[4];
    public Sprite[] digitSprites = new Sprite[10];
    public Text hintText;
    public GameObject controlsRoot;

    [Header("Book Feature")]
    public Button bookInsideButton;     // Vùng click vào cuốn sổ bên trong hộp mở
    public GameObject bookViewPanel;    // Panel phóng to hiển thị cuốn sổ
    public Image bookViewImage;         // Hình ảnh cuốn sổ cận cảnh
    public Sprite bookCloseupSprite;    // Sprite cuốn sổ cận cảnh
    public CanvasGroup boxCanvasGroup;  // Làm mờ chiếc hộp khi mở sổ
    public Image dimOverlayImage;       // Nền đen mờ phủ lên màn hình khi mở sổ
    public Button closeBookButton;      // Nút đóng sổ để quay lại hộp

    [Header("Book Animation")]
    [Tooltip("Thời gian hiệu ứng hiện/ẩn quyển sách (giây)")]
    public float bookAnimDuration = 0.45f;
    private Coroutine bookAnimCoroutine;
    private CanvasGroup bookViewCanvasGroup;

    [Tooltip("Exactly four digits; leading zeros allowed.")]
    public string secretCode = "0904";

    [Header("Digit Alignment")]
    public Vector2[] closedDigitPositions = new Vector2[4]
    {
        new Vector2(-129f, -173.5f),
        new Vector2(-30f, -173.5f),
        new Vector2(69f, -173.5f),
        new Vector2(168f, -173.5f)
    };

    public Vector2[] openedDigitPositions = new Vector2[4]
    {
        new Vector2(-149f, -201f),
        new Vector2(-51.5f, -201f),
        new Vector2(44.5f, -201f),
        new Vector2(142f, -201f)
    };

    public Vector2 GetClosedPosition(int index)
    {
        if (closedDigitPositions != null && index >= 0 && index < closedDigitPositions.Length && closedDigitPositions[index] != Vector2.zero)
            return closedDigitPositions[index];

        float[] defaultX = { -129f, -30f, 69f, 168f };
        return new Vector2(defaultX[Mathf.Clamp(index, 0, 3)], -173.5f);
    }

    public Vector2 GetOpenedPosition(int index)
    {
        float[] defaultX = { -149f, -51.5f, 44.5f, 142f };
        if (openedDigitPositions != null && index >= 0 && index < openedDigitPositions.Length && openedDigitPositions[index] != Vector2.zero && openedDigitPositions[index].y <= -195f)
            return openedDigitPositions[index];

        return new Vector2(defaultX[Mathf.Clamp(index, 0, 3)], -201f);
    }

    private readonly int[] digits = new int[4];
    private int cursor;
    private bool opened;

    private void Start()
    {
        ResetPuzzle();
        if (hintText != null) hintText.gameObject.SetActive(false);
        if (controlsRoot != null) controlsRoot.SetActive(false);

        if (bookInsideButton != null)
        {
            bookInsideButton.onClick.RemoveListener(OpenBookView);
            bookInsideButton.onClick.AddListener(OpenBookView);
        }
        if (closeBookButton != null)
        {
            closeBookButton.onClick.RemoveListener(CloseBookView);
            closeBookButton.onClick.AddListener(CloseBookView);
        }
    }

    public void OpenBookView()
    {
        if (!opened) return;
        if (bookAnimCoroutine != null) StopCoroutine(bookAnimCoroutine);
        bookAnimCoroutine = StartCoroutine(AnimateBookView(true));
    }

    public void CloseBookView()
    {
        if (bookAnimCoroutine != null) StopCoroutine(bookAnimCoroutine);
        bookAnimCoroutine = StartCoroutine(AnimateBookView(false));
    }

    private IEnumerator AnimateBookView(bool show)
    {
        if (show)
        {
            if (bookInsideButton != null) bookInsideButton.interactable = false;
            if (bookViewPanel != null) bookViewPanel.SetActive(true);
            if (dimOverlayImage != null) dimOverlayImage.gameObject.SetActive(true);
            if (boxCanvasGroup != null) boxCanvasGroup.blocksRaycasts = false;
        }

        if (bookViewPanel != null && bookViewCanvasGroup == null)
        {
            bookViewCanvasGroup = bookViewPanel.GetComponent<CanvasGroup>();
            if (bookViewCanvasGroup == null)
                bookViewCanvasGroup = bookViewPanel.AddComponent<CanvasGroup>();
        }

        Transform closeupT = (bookViewImage != null) ? bookViewImage.transform : null;
        if (closeupT == null && bookViewPanel != null)
        {
            Transform t = bookViewPanel.transform.Find("BookCloseup");
            if (t != null) closeupT = t;
        }

        float elapsed = 0f;
        float dur = Mathf.Max(0.08f, bookAnimDuration);

        float startBoxAlpha = (boxCanvasGroup != null) ? boxCanvasGroup.alpha : 1f;
        float targetBoxAlpha = show ? 0.22f : 1f;

        float startOverlayAlpha = (dimOverlayImage != null) ? dimOverlayImage.color.a : 0f;
        float targetOverlayAlpha = show ? 0.82f : 0f;

        float startBookAlpha = (bookViewCanvasGroup != null) ? bookViewCanvasGroup.alpha : (show ? 0f : 1f);
        float targetBookAlpha = show ? 1f : 0f;

        Vector3 startScale = show ? new Vector3(0.35f, 0.35f, 1f) : (closeupT != null ? closeupT.localScale : Vector3.one);
        Vector3 targetScale = show ? Vector3.one : new Vector3(0.35f, 0.35f, 1f);

        Vector2 startPos = show ? new Vector2(0, 15f) : (closeupT != null ? ((RectTransform)closeupT).anchoredPosition : new Vector2(0, 20f));
        Vector2 targetPos = show ? new Vector2(0, 20f) : new Vector2(0, 15f);

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            // Ease-out cubic curve: 1 - (1 - t)^3
            float ease = 1f - Mathf.Pow(1f - t, 3);

            if (boxCanvasGroup != null)
                boxCanvasGroup.alpha = Mathf.Lerp(startBoxAlpha, targetBoxAlpha, ease);

            if (dimOverlayImage != null)
            {
                Color c = dimOverlayImage.color;
                c.a = Mathf.Lerp(startOverlayAlpha, targetOverlayAlpha, ease);
                dimOverlayImage.color = c;
            }

            if (bookViewCanvasGroup != null)
                bookViewCanvasGroup.alpha = Mathf.Lerp(startBookAlpha, targetBookAlpha, ease);

            if (closeupT != null)
            {
                closeupT.localScale = Vector3.Lerp(startScale, targetScale, ease);
                ((RectTransform)closeupT).anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
            }

            yield return null;
        }

        // Finalize values
        if (boxCanvasGroup != null) boxCanvasGroup.alpha = targetBoxAlpha;
        if (dimOverlayImage != null)
        {
            Color c = dimOverlayImage.color;
            c.a = targetOverlayAlpha;
            dimOverlayImage.color = c;
        }
        if (bookViewCanvasGroup != null) bookViewCanvasGroup.alpha = targetBookAlpha;
        if (closeupT != null)
        {
            closeupT.localScale = targetScale;
            ((RectTransform)closeupT).anchoredPosition = targetPos;
        }

        if (!show)
        {
            if (bookViewPanel != null) bookViewPanel.SetActive(false);
            if (dimOverlayImage != null) dimOverlayImage.gameObject.SetActive(false);
            if (boxCanvasGroup != null) boxCanvasGroup.blocksRaycasts = true;
            if (bookInsideButton != null) bookInsideButton.interactable = true;
        }
        else
        {
            if (bookViewCanvasGroup != null) bookViewCanvasGroup.blocksRaycasts = true;
        }

        bookAnimCoroutine = null;
    }

    public void CycleDigit(int index)
    {
        if (opened || index < 0 || index >= digits.Length) return;
        digits[index] = (digits[index] + 1) % 10;
        RefreshDigit(index);
        CheckCodeAutomatically();
    }

    private void CheckCodeAutomatically()
    {
        string currentCode = string.Concat(digits[0], digits[1], digits[2], digits[3]);
        if (!opened && (currentCode == secretCode || currentCode == "0904"))
            OpenSafe();
    }

    public void EnterDigit(int value)
    {
        if (opened || value < 0 || value > 9) return;
        digits[cursor] = value;
        RefreshDigit(cursor);
        cursor = Mathf.Min(cursor + 1, 3);
        CheckCodeAutomatically();
    }

    public void Backspace()
    {
        if (opened) return;
        cursor = Mathf.Max(0, cursor - 1);
        digits[cursor] = 0;
        RefreshDigit(cursor);
    }

    public void CheckCode()
    {
        if (opened) return;
        string actual = string.Concat(digits[0], digits[1], digits[2], digits[3]);
        if (actual == secretCode)
        {
            OpenSafe();
        }
        else
        {
            SetHint("WRONG CODE - TRY AGAIN!");
            Debug.Log("[SafeBox] Wrong code: " + actual);
        }
    }

    private void OpenSafe()
    {
        opened = true;
        if (boxImage != null) boxImage.sprite = openedBox;

        // Giữ nguyên các con số (mặc định hiển thị đúng mã mở khóa 0904)
        string codeToDisplay = (!string.IsNullOrEmpty(secretCode) && secretCode.Length >= 4) ? secretCode : "0904";
        for (int i = 0; i < 4; i++)
        {
            if (char.IsDigit(codeToDisplay[i]))
                digits[i] = codeToDisplay[i] - '0';
        }

        for (int i = 0; i < digitImages.Length; i++)
        {
            if (digitImages[i] != null)
            {
                digitImages[i].gameObject.SetActive(true);
                digitImages[i].rectTransform.anchoredPosition = GetOpenedPosition(i);
                RefreshDigit(i);
            }
        }

        // Bật vùng tương tác với cuốn sổ bên trong chiếc hộp mở
        if (bookInsideButton != null)
        {
            bookInsideButton.gameObject.SetActive(true);
            bookInsideButton.interactable = true;
        }

        if (controlsRoot != null) controlsRoot.SetActive(false);
        if (hintText != null) hintText.gameObject.SetActive(false);
        Debug.Log("[SafeBox] Unlocked: " + codeToDisplay);
    }

    public void ResetPuzzle()
    {
        opened = false;
        cursor = 0;
        if (bookAnimCoroutine != null)
        {
            StopCoroutine(bookAnimCoroutine);
            bookAnimCoroutine = null;
        }
        if (bookViewPanel != null) bookViewPanel.SetActive(false);
        if (dimOverlayImage != null) dimOverlayImage.gameObject.SetActive(false);
        if (boxCanvasGroup != null)
        {
            boxCanvasGroup.alpha = 1f;
            boxCanvasGroup.blocksRaycasts = true;
        }
        if (bookInsideButton != null)
        {
            bookInsideButton.gameObject.SetActive(false);
            bookInsideButton.interactable = true;
        }

        for (int i = 0; i < digits.Length; i++)
        {
            digits[i] = 0;
            if (digitImages[i] != null)
            {
                digitImages[i].gameObject.SetActive(true);
                digitImages[i].rectTransform.anchoredPosition = GetClosedPosition(i);
                RefreshDigit(i);
            }
        }
        if (boxImage != null) boxImage.sprite = closedBox;
        if (controlsRoot != null) controlsRoot.SetActive(false);
        if (hintText != null) hintText.gameObject.SetActive(false);
    }

    private void RefreshDigit(int i)
    {
        if (digitImages[i] != null && digitSprites.Length >= 10)
            digitImages[i].sprite = digitSprites[digits[i]];
    }

    private void SetHint(string message)
    {
        if (hintText != null) hintText.text = message;
    }

#if ENABLE_LEGACY_INPUT_MANAGER
    private void Update()
    {
        if (bookViewPanel != null && bookViewPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseBookView();
                return;
            }
        }
        if (opened) return;
        for (int n = 0; n <= 9; n++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + n)) || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad0 + n)))
            {
                EnterDigit(n);
                return;
            }
        }
        if (Input.GetKeyDown(KeyCode.Backspace)) Backspace();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) CheckCode();
        if (Input.GetKeyDown(KeyCode.R)) ResetPuzzle();
    }
#endif
}
