using UnityEngine;

public class InputManager : MonoBehaviour
{
    public GameBoard gameBoard;
    public LayerMask itemLayer;

    private Item selectedItem;
    private Vector2 pointerDownWorldPos;
    private bool isDragging;

    private void Update()
    {
        if (gameBoard.IsBusy) return; // board is animating/resolving — ignore all input

        if (Input.GetMouseButtonDown(0)) OnPointerDown(Input.mousePosition);
        if (Input.GetMouseButtonUp(0)) OnPointerUp(Input.mousePosition);

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) OnPointerDown(touch.position);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                OnPointerUp(touch.position);
        }
    }

    private void OnPointerDown(Vector2 screenPos)
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, 0f, itemLayer);

        if (hit.collider == null) return;

        selectedItem = hit.collider.GetComponent<Item>();
        pointerDownWorldPos = worldPos;
        isDragging = true;
    }

    private void OnPointerUp(Vector2 screenPos)
    {
        if (!isDragging || selectedItem == null) return;
        isDragging = false;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        Vector2 delta = worldPos - pointerDownWorldPos;

        // Too small a movement = treat as a tap, not a swipe. Ignore it.
        if (delta.magnitude < 0.2f)
        {
            selectedItem = null;
            return;
        }

        Vector2Int dir = GetSwipeDirection(delta);
        int targetX = selectedItem.xIndex + dir.x;
        int targetY = selectedItem.yIndex + dir.y;

        gameBoard.TrySwap(selectedItem.xIndex, selectedItem.yIndex, targetX, targetY);

        selectedItem = null;
    }

    // Whichever axis moved further decides the direction (up/down/left/right)
    private Vector2Int GetSwipeDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
    }
}
