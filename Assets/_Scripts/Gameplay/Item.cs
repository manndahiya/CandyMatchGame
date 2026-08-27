using UnityEngine;
using DG.Tweening;

public class Item : MonoBehaviour
{
    public ItemColor itemColor;
    public ItemType itemType = ItemType.Simple;

    [Header("Special Sprites (for this candy's color)")]
    public Sprite horizontalStripedSprite;
    public Sprite verticalStripedSprite;
    public Sprite wrappedSprite;
    public Sprite colorBombSprite;

    public int xIndex;
    public int yIndex;

    public bool isMatched;
    public bool isMoving;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetCoordinates(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }

    public void MoveTo(Vector3 targetWorldPos, float duration, System.Action onComplete = null)
    {
        transform.DOMove(targetWorldPos, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void BecomeSpecial(ItemType newType)
    {
        itemType = newType;

        switch (newType)
        {
            case ItemType.HorizontalStriped:
                spriteRenderer.sprite = horizontalStripedSprite;
                break;

            case ItemType.VerticalStriped:
                spriteRenderer.sprite = verticalStripedSprite;
                break;

            case ItemType.Wrapped:
                spriteRenderer.sprite = wrappedSprite;
                break;

            case ItemType.ColorBomb:
                itemColor = ItemColor.None;
                spriteRenderer.sprite = colorBombSprite;
                break;
        }
    }

    // --- Debug/testing only: visualizes "this candy is about to be destroyed" ---

    public void StartClearPreview()
    {
        spriteRenderer.DOKill(); // stop any previous fade tween on this renderer

        Color c = spriteRenderer.color;
        c.a = 0.1f;
        spriteRenderer.color = c;

        // Flicker between 0.1 and 0.5 alpha, forever, until StopClearPreview cancels it.
        spriteRenderer.DOFade(0.5f, 0.4f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void StopClearPreview()
    {
        spriteRenderer.DOKill();

        Color c = spriteRenderer.color;
        c.a = 1f;
        spriteRenderer.color = c;
    }
}

public enum ItemColor { None, Red, Blue, Green, Purple, Yellow }
public enum ItemType { Simple, HorizontalStriped, VerticalStriped, Wrapped, ColorBomb }