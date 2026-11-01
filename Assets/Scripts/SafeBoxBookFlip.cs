using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Click the closed book to display the open page immediately.
/// When open, dragging over the page is reserved for EraseYouMouse.
/// </summary>
[DisallowMultipleComponent]
public sealed class SafeBoxBookFlip : MonoBehaviour
{
    public Image bookImage;
    public Image coverImage; // Legacy V3 cover: automatically hidden.
    public Sprite closedBookSprite;
    public Sprite openedBookSprite;
    public Button clickSurface;

    [SerializeField] private bool resetWhenEnabled = true;
    private RectTransform bookRect;
    private Vector2 closedSize;
    private Vector2 closedPosition;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (bookImage == null) return;
        bookRect = bookImage.rectTransform;
        closedSize = bookRect.sizeDelta;
        closedPosition = bookRect.anchoredPosition;
    }

    private void OnEnable()
    {
        if (bookRect == null && bookImage != null) Awake();
        if (resetWhenEnabled) ResetToClosed();
        if (clickSurface != null)
        {
            clickSurface.onClick.RemoveListener(ToggleBook);
            clickSurface.onClick.AddListener(ToggleBook);
            clickSurface.interactable = true;
        }
    }

    private void OnDisable()
    {
        if (clickSurface != null) clickSurface.onClick.RemoveListener(ToggleBook);
    }

    public void ToggleBook()
    {
        // IMPORTANT: Do not toggle closed on pointer-up after brushing.
        if (isOpen || bookImage == null || openedBookSprite == null) return;
        isOpen = true;
        bookImage.sprite = openedBookSprite;
        bookImage.preserveAspect = true;
        UpdateGeometry();
    }

    public void ResetToClosed()
    {
        isOpen = false;
        if (bookImage != null)
        {
            bookImage.sprite = closedBookSprite;
            bookImage.preserveAspect = true;
            bookImage.color = Color.white;
        }
        if (coverImage != null) coverImage.gameObject.SetActive(false);
        UpdateGeometry();
    }

    private void UpdateGeometry()
    {
        if (bookRect == null) return;
        bookRect.anchorMin = bookRect.anchorMax = new Vector2(0.5f, 0.5f);
        bookRect.pivot = new Vector2(0.5f, 0.5f);
        bookRect.localScale = Vector3.one;
        bookRect.localRotation = Quaternion.identity;
        bookRect.anchoredPosition = closedPosition;
        // Opened book: 2x WIDTH, same HEIGHT.
        bookRect.sizeDelta = isOpen
            ? new Vector2(closedSize.x * 2f, closedSize.y)
            : closedSize;
        if (clickSurface != null)
        {
            RectTransform hit = clickSurface.GetComponent<RectTransform>();
            if (hit != null && hit != bookRect)
            {
                hit.anchorMin = hit.anchorMax = new Vector2(0.5f, 0.5f);
                hit.pivot = new Vector2(0.5f, 0.5f);
                hit.anchoredPosition = closedPosition;
                hit.sizeDelta = bookRect.sizeDelta;
            }
        }
    }
}
