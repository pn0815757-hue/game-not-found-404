
using UnityEngine;
using UnityEngine.UI;

public class BookEraseYou : MonoBehaviour
{
    [Header("References")]
    public SafeBoxBookFlip book;

    [Header("Book Sprites")]
    public Sprite so1;
    public Sprite so2;

    [Header("Settings")]
    public KeyCode eraseKey = KeyCode.E;

    private bool erased = false;

    private void Update()
    {
        if (book == null || !book.IsOpen)
            return;

        if (erased)
            return;

        if (Input.GetKeyDown(eraseKey))
        {
            EraseYou();
        }
    }

    private void EraseYou()
    {
        if (so2 == null || book == null || book.bookImage == null)
            return;

        erased = true;

        // Replace only the sprite.
        // Keep the existing RectTransform, size and alignment.
        book.openedBookSprite = so2;
        book.bookImage.sprite = so2;
        book.bookImage.preserveAspect = true;

        Debug.Log("[Book] You has been erased.");
    }

    public void ResetErase()
    {
        erased = false;

        if (book != null && so1 != null)
        {
            book.openedBookSprite = so1;

            if (book.IsOpen && book.bookImage != null)
                book.bookImage.sprite = so1;
        }
    }
}
