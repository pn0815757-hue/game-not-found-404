
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class EraseYouMouse : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler, IEndDragHandler
{
    [Header("References")]
    public SafeBoxBookFlip book;

    [Header("Erase Area - normalized UV")]
    public Rect eraseArea = new Rect(0.54f, 0.13f, 0.09f, 0.10f);

    [Header("Brush Settings")]
    [Range(2, 80)]
    public int brushRadiusPixels = 20;

    [Range(0.01f, 1f)]
    public float strengthPerStroke = 0.35f;

    [Range(2, 40)]
    public int cleanPaperMargin = 12;

    private Texture2D workingTexture;
    private Sprite workingSprite;
    private Color32[] originalPixels;
    private Color32[] pixels;

    private int width;
    private int height;

    private bool dragging;
    private Vector2 previousUV;

    private bool EnsureTexture()
    {
        if (book == null || book.bookImage == null ||
            book.openedBookSprite == null)
            return false;

        if (workingTexture != null)
            return true;

        Sprite sourceSprite = book.openedBookSprite;
        Texture2D source = sourceSprite.texture;

        try
        {
            width = Mathf.RoundToInt(sourceSprite.rect.width);
            height = Mathf.RoundToInt(sourceSprite.rect.height);

            if (source.width != width ||
                source.height != height)
            {
                Debug.LogError(
                    "[EraseYou] Sprite must be Single and Full Rect."
                );
                return false;
            }

            originalPixels = source.GetPixels32();
            pixels = (Color32[])originalPixels.Clone();

            workingTexture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false
            );

            workingTexture.filterMode = source.filterMode;
            workingTexture.wrapMode = TextureWrapMode.Clamp;
            workingTexture.SetPixels32(pixels);
            workingTexture.Apply(false);

            Vector2 pivot = new Vector2(
                sourceSprite.pivot.x / width,
                sourceSprite.pivot.y / height
            );

            workingSprite = Sprite.Create(
                workingTexture,
                new Rect(0, 0, width, height),
                pivot,
                sourceSprite.pixelsPerUnit
            );

            workingSprite.name = "Book_Erased_Runtime";
            book.bookImage.sprite = workingSprite;
            book.bookImage.preserveAspect = true;

            return true;
        }
        catch (UnityException e)
        {
            Debug.LogError("[EraseYou] " + e.Message);
            Cleanup();
            return false;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dragging = false;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (book == null || !book.IsOpen)
            return;

        Vector2 uv;
        if (!ScreenToUV(eventData, out uv))
            return;

        if (!eraseArea.Contains(uv))
            return;

        if (!EnsureTexture())
            return;

        dragging = true;
        previousUV = uv;
        EraseAt(uv);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || book == null || !book.IsOpen)
            return;

        Vector2 uv;
        if (!ScreenToUV(eventData, out uv))
            return;

        float distance = Vector2.Distance(previousUV, uv);

        int steps = Mathf.Clamp(
            Mathf.CeilToInt(
                distance * Mathf.Max(width, height) /
                Mathf.Max(1f, brushRadiusPixels * 0.35f)
            ),
            1,
            160
        );

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            EraseAt(Vector2.Lerp(previousUV, uv, t));
        }

        previousUV = uv;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        dragging = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
    }

    private bool ScreenToUV(
        PointerEventData eventData,
        out Vector2 uv)
    {
        uv = Vector2.zero;

        if (book == null || book.bookImage == null ||
            book.openedBookSprite == null)
            return false;

        RectTransform rt = book.bookImage.rectTransform;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt,
            eventData.position,
            eventData.pressEventCamera,
            out local))
            return false;

        Rect visibleRect = rt.rect;

        if (book.bookImage.preserveAspect)
        {
            float spriteAspect =
                book.openedBookSprite.rect.width /
                book.openedBookSprite.rect.height;

            float frameAspect =
                visibleRect.width / visibleRect.height;

            if (frameAspect > spriteAspect)
            {
                float actualWidth = visibleRect.height * spriteAspect;

                visibleRect = new Rect(
                    visibleRect.center.x - actualWidth * 0.5f,
                    visibleRect.yMin,
                    actualWidth,
                    visibleRect.height
                );
            }
            else
            {
                float actualHeight = visibleRect.width / spriteAspect;

                visibleRect = new Rect(
                    visibleRect.xMin,
                    visibleRect.center.y - actualHeight * 0.5f,
                    visibleRect.width,
                    actualHeight
                );
            }
        }

        uv.x = (local.x - visibleRect.xMin) / visibleRect.width;
        uv.y = (local.y - visibleRect.yMin) / visibleRect.height;

        return uv.x >= 0f && uv.x <= 1f &&
               uv.y >= 0f && uv.y <= 1f;
    }

    private void EraseAt(Vector2 uv)
    {
        if (workingTexture == null || !eraseArea.Contains(uv))
            return;

        int cx = Mathf.RoundToInt(uv.x * (width - 1));
        int cy = Mathf.RoundToInt(uv.y * (height - 1));

        int radius = Mathf.Max(1, brushRadiusPixels);

        int areaLeft = Mathf.Clamp(
            Mathf.CeilToInt(eraseArea.xMin * width), 0, width - 1);

        int areaRight = Mathf.Clamp(
            Mathf.FloorToInt(eraseArea.xMax * width), 0, width - 1);

        int areaBottom = Mathf.Clamp(
            Mathf.CeilToInt(eraseArea.yMin * height), 0, height - 1);

        int areaTop = Mathf.Clamp(
            Mathf.FloorToInt(eraseArea.yMax * height), 0, height - 1);

        int x0 = Mathf.Max(areaLeft, cx - radius);
        int x1 = Mathf.Min(areaRight, cx + radius);
        int y0 = Mathf.Max(areaBottom, cy - radius);
        int y1 = Mathf.Min(areaTop, cy + radius);

        if (x1 < x0 || y1 < y0)
            return;

        int leftSampleX = Mathf.Max(
            0, areaLeft - cleanPaperMargin);

        int rightSampleX = Mathf.Min(
            width - 1, areaRight + cleanPaperMargin);

        int patchWidth = x1 - x0 + 1;
        int patchHeight = y1 - y0 + 1;

        Color32[] patch = new Color32[
            patchWidth * patchHeight
        ];

        for (int y = y0; y <= y1; y++)
        {
            Color leftPaper =
                originalPixels[y * width + leftSampleX];

            Color rightPaper =
                originalPixels[y * width + rightSampleX];

            for (int x = x0; x <= x1; x++)
            {
                int index = y * width + x;

                float dx = x - cx;
                float dy = y - cy;

                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance < radius)
                {
                    float gradient = Mathf.InverseLerp(
                        areaLeft, areaRight, x);

                    Color paperColor = Color.Lerp(
                        leftPaper,
                        rightPaper,
                        gradient
                    );

                    float brush = 1f - distance / radius;
                    brush = brush * brush *
                        (3f - 2f * brush);

                    float strength =
                        brush * strengthPerStroke;

                    Color result = Color.Lerp(
                        pixels[index],
                        paperColor,
                        strength
                    );

                    pixels[index] = (Color32)result;
                }

                patch[(y - y0) * patchWidth + (x - x0)] =
                    pixels[index];
            }
        }

        workingTexture.SetPixels32(
            x0, y0, patchWidth, patchHeight, patch);

        workingTexture.Apply(false);
    }

    private void Cleanup()
    {
        if (book != null && book.bookImage != null &&
            book.bookImage.sprite == workingSprite)
        {
            book.bookImage.sprite = book.openedBookSprite;
        }

        if (workingSprite != null)
            Destroy(workingSprite);

        if (workingTexture != null)
            Destroy(workingTexture);

        workingSprite = null;
        workingTexture = null;
        originalPixels = null;
        pixels = null;
    }

    private void OnDisable()
    {
        dragging = false;
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
