using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameBoard : MonoBehaviour
{
    [System.Serializable]
    public struct ColorPrefabPair
    {
        public ItemColor color;
        public GameObject prefab;
    }

    [Header("Board Size")]
    public int width = 8;
    public int height = 8;

    [Header("Item Prefabs")]
    public List<ColorPrefabPair> itemPrefabs;

    [Header("Board Parent")]
    public Transform itemsParent;

    [Header("Background")]
    public SpriteRenderer boardRenderer;

    [Header("Fall Settings")]
    public float fallDuration = 0.3f;

    [Header("Debug / Testing")]
    [Tooltip("If true, candies about to be destroyed will flicker for a few seconds before actually being cleared. Turn off for normal play.")]
    public bool debugShowClearPreview = false;
    public float debugPreviewDuration = 5f;

    public bool IsBusy { get; private set; }

    // Fired once per resolved match: true = special match (created/upgraded
    // a special candy), false = plain 3-match. GameManager listens to this
    // to award score without GameBoard knowing anything about scoring.
    public event System.Action<bool> OnMatchScored;
    public void SetInputLocked(bool locked) => inputLocked = locked;

    private float cellWidth;
    private float cellHeight;
    private Vector3 origin;

    private Cell[,] gameBoard;
    private ItemColor[,] startingColors;
    private int[] columnEmptyCount;
    private bool inputLocked = false;


    private void Start()
    {
        CalculateGridMetrics();
        CreateBoard();
    }

    #region Generate Board, Avoid Involuntary Matches, Preserve atleast 1 possible valid move
    private void CreateBoard()
    {
        gameBoard = new Cell[width, height];
        startingColors = new ItemColor[width, height];
        columnEmptyCount = new int[width];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gameBoard[x, y] = new Cell(true);
            }
        }

        GenerateValidBoard();
    }

    private void GenerateValidBoard()
    {
        const int maxAttempts = 1000;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            GenerateRandomColors();

            if (HasAnyMatch())
                continue;

            if (!HasPossibleMove())
                continue;

            SpawnBoardFromColors();
            return;
        }

        Debug.LogError("Could not generate a valid starting board.");
    }

    private void SpawnBoardFromColors()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SpawnItem(x, y, startingColors[x, y]);
            }
        }
    }

    private void SpawnItem(int x, int y, ItemColor color)
    {
        GameObject prefab = GetPrefabForColor(color);

        GameObject newItem = Instantiate(
            prefab,
            GetWorldPosition(x, y),
            Quaternion.identity,
            itemsParent
        );

        Item item = newItem.GetComponent<Item>();

        item.itemColor = color;
        item.itemType = ItemType.Simple;

        item.SetCoordinates(x, y);

        gameBoard[x, y].item = newItem;
    }

    private GameObject GetPrefabForColor(ItemColor color)
    {
        foreach (var pair in itemPrefabs)
        {
            if (pair.color == color)
                return pair.prefab;
        }

        Debug.LogError($"No prefab assigned for color {color}! Check GameBoard's Item Prefabs list.");
        return null;
    }

    private void GenerateRandomColors()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int randomColor = Random.Range(
                    1,
                    System.Enum.GetValues(typeof(ItemColor)).Length
                );

                startingColors[x, y] = (ItemColor)randomColor;
            }
        }
    }

    private bool HasAnyMatch()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x <= width - 3; x++)
            {
                if (startingColors[x, y] == startingColors[x + 1, y] &&
                    startingColors[x, y] == startingColors[x + 2, y])
                {
                    return true;
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y <= height - 3; y++)
            {
                if (startingColors[x, y] == startingColors[x, y + 1] &&
                    startingColors[x, y] == startingColors[x, y + 2])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasPossibleMove()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < width - 1)
                {
                    SwapColors(x, y, x + 1, y);
                    bool createsMatch = HasAnyMatch();
                    SwapColors(x, y, x + 1, y);
                    if (createsMatch) return true;
                }

                if (y < height - 1)
                {
                    SwapColors(x, y, x, y + 1);
                    bool createsMatch = HasAnyMatch();
                    SwapColors(x, y, x, y + 1);
                    if (createsMatch) return true;
                }
            }
        }

        return false;
    }

    private void SwapColors(int x1, int y1, int x2, int y2)
    {
        ItemColor temp = startingColors[x1, y1];
        startingColors[x1, y1] = startingColors[x2, y2];
        startingColors[x2, y2] = temp;
    }

    private void CalculateGridMetrics()
    {
        Bounds bounds = boardRenderer.bounds;

        cellWidth = bounds.size.x / width;
        cellHeight = bounds.size.y / height;

        origin = new Vector3(bounds.min.x, bounds.min.y, 0f);
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        float xPos = origin.x + (x + 0.5f) * cellWidth;
        float yPos = origin.y + (y + 0.5f) * cellHeight;

        return new Vector3(xPos, yPos, 0f);
    }

    #endregion

    #region Matching Candies

    public void TrySwap(int x1, int y1, int x2, int y2)
    {
        if (IsBusy || inputLocked) return;

        if (!IsInBounds(x1, y1) || !IsInBounds(x2, y2)) return;
        if (!IsAdjacent(x1, y1, x2, y2)) return;

        Item itemA = gameBoard[x1, y1].item.GetComponent<Item>();
        Item itemB = gameBoard[x2, y2].item.GetComponent<Item>();

        if (itemA.isMoving || itemB.isMoving) return;

        IsBusy = true;

        SwapItemsInData(x1, y1, x2, y2);

        AnimateSwap(itemA, itemB, () =>
        {
            if (TryHandleSpecialCombo(itemA, itemB))
                return;

            List<MatchGroup> matches = MatchFinder.FindAllMatches(gameBoard, width, height);

            if (matches.Count > 0)
            {
                ResolveMatches(matches, new Vector2Int(x2, y2));
            }
            else
            {
                SwapItemsInData(x1, y1, x2, y2);
                AnimateSwap(itemA, itemB, () => IsBusy = false);
            }
        });
    }

    private bool IsInBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    private bool IsAdjacent(int x1, int y1, int x2, int y2)
    {
        int dx = Mathf.Abs(x1 - x2);
        int dy = Mathf.Abs(y1 - y2);
        return dx + dy == 1;
    }

    private void SwapItemsInData(int x1, int y1, int x2, int y2)
    {
        GameObject temp = gameBoard[x1, y1].item;
        gameBoard[x1, y1].item = gameBoard[x2, y2].item;
        gameBoard[x2, y2].item = temp;

        gameBoard[x1, y1].item.GetComponent<Item>().SetCoordinates(x1, y1);
        gameBoard[x2, y2].item.GetComponent<Item>().SetCoordinates(x2, y2);
    }

    private void AnimateSwap(Item a, Item b, System.Action onComplete)
    {
        a.isMoving = true;
        b.isMoving = true;

        Vector3 posA = GetWorldPosition(a.xIndex, a.yIndex);
        Vector3 posB = GetWorldPosition(b.xIndex, b.yIndex);

        int remaining = 2;
        void OnOneDone()
        {
            remaining--;
            if (remaining == 0)
            {
                a.isMoving = false;
                b.isMoving = false;
                onComplete?.Invoke();
            }
        }

        a.MoveTo(posA, 0.25f, OnOneDone);
        b.MoveTo(posB, 0.25f, OnOneDone);
    }

    private void ResolveMatches(List<MatchGroup> matches, Vector2Int preferredCell)
    {
        HashSet<Vector2Int> cellsToClear = new HashSet<Vector2Int>();
        List<(Vector2Int cell, ItemType type, ItemColor color)> newSpecials = new List<(Vector2Int, ItemType, ItemColor)>();
        HashSet<Vector2Int> protectedCells = new HashSet<Vector2Int>();

        foreach (var match in matches)
        {
            ItemType resultType = GetResultingType(match);
            Vector2Int specialCell = GetSpecialCellFor(match, preferredCell);
            ItemColor matchColor = GetMatchColor(match); // <-- was reading existingItem.itemColor directly

            OnMatchScored?.Invoke(resultType != ItemType.Simple);

            foreach (var cell in match.cells)
            {
                if (cell == specialCell && resultType != ItemType.Simple)
                {
                    GameObject existingObj = gameBoard[cell.x, cell.y].item;
                    Item existingItem = existingObj != null ? existingObj.GetComponent<Item>() : null;

                    newSpecials.Add((cell, resultType, matchColor)); // <-- use matchColor

                    if (existingItem != null && existingItem.itemType != ItemType.Simple)
                        cellsToClear.Add(cell);
                    else
                        protectedCells.Add(cell);
                }
                else
                {
                    cellsToClear.Add(cell);
                }
            }
        }

        ProcessClearQueue(cellsToClear, protectedCells, () =>
        {
            foreach (var (cell, type, color) in newSpecials)
                SpawnOrUpgradeSpecial(cell, type, color);

            CollapseAndRefill();
        });
    }


    private ItemColor GetMatchColor(MatchGroup match)
    {
        foreach (var cell in match.cells)
        {
            GameObject obj = gameBoard[cell.x, cell.y].item;
            if (obj == null) continue;

            ItemColor color = obj.GetComponent<Item>().itemColor;
            if (color != ItemColor.None)
                return color;
        }

        return ItemColor.Red; // shouldn't happen in practice, safety net only
    }

    private void SpawnOrUpgradeSpecial(Vector2Int cell, ItemType type, ItemColor color)
    {
        GameObject obj = gameBoard[cell.x, cell.y].item;

        if (obj != null)
        {
            obj.GetComponent<Item>().BecomeSpecial(type);
            return;
        }

        GameObject prefab = GetPrefabForColor(color);
        GameObject newItem = Instantiate(prefab, GetWorldPosition(cell.x, cell.y), Quaternion.identity, itemsParent);
        Item item = newItem.GetComponent<Item>();

        item.itemColor = color;
        item.SetCoordinates(cell.x, cell.y);
        item.BecomeSpecial(type);

        gameBoard[cell.x, cell.y].item = newItem;
    }

    private Vector2Int GetSpecialCellFor(MatchGroup match, Vector2Int preferredCell)
    {
        // If the player's swipe directly created this match, the special should
        // spawn where they moved their candy to — regardless of shape.
        if (match.cells.Contains(preferredCell))
            return preferredCell;

        // Cascade-triggered match (no swipe involved) — fall back to the
        // geometric middle for L/T shapes, or just the first cell otherwise.
        if (match.isLOrTShape)
            return match.anchorCell;

        return match.cells[0];
    }

    private ItemType GetResultingType(MatchGroup match)
    {
        if (match.isLOrTShape) return ItemType.Wrapped;
        if (match.Count >= 5) return ItemType.ColorBomb;
        if (match.Count == 4) return match.isHorizontal ? ItemType.HorizontalStriped : ItemType.VerticalStriped;
        return ItemType.Simple;
    }

    #endregion

    #region Special Candy Effects

    // Combines two special candies swapped directly into each other.
    // Returns true if a combo happened (caller should stop, board is already resolving).
    private bool TryHandleSpecialCombo(Item itemA, Item itemB)
    {
        bool aSpecial = itemA.itemType != ItemType.Simple;
        bool bSpecial = itemB.itemType != ItemType.Simple;
        bool involvesColorBomb = itemA.itemType == ItemType.ColorBomb || itemB.itemType == ItemType.ColorBomb;

        if (!involvesColorBomb)
        {
            // Neither is a Color Bomb — combo only if BOTH are special AND
            // the same color (e.g. two Red specials). Different-colored
            // specials no longer combo, matching Candy Crush's rule.
            if (!(aSpecial && bSpecial)) return false;
            if (itemA.itemColor != itemB.itemColor) return false;
        }

        HashSet<Vector2Int> cellsToClear = new HashSet<Vector2Int>();
        bool bothColorBombs = itemA.itemType == ItemType.ColorBomb && itemB.itemType == ItemType.ColorBomb;

        if (bothColorBombs)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    cellsToClear.Add(new Vector2Int(x, y));
        }
        else if (involvesColorBomb)
        {
            Item bomb = itemA.itemType == ItemType.ColorBomb ? itemA : itemB;
            Item other = bomb == itemA ? itemB : itemA;

            cellsToClear.UnionWith(GetCellsOfColor(other.itemColor));

            if (other.itemType != ItemType.Simple)
                cellsToClear.UnionWith(GetAffectedCells(other));

            cellsToClear.Add(new Vector2Int(bomb.xIndex, bomb.yIndex));
        }
        else
        {
            // Two non-bomb specials, same color confirmed above — union both effects.
            cellsToClear.UnionWith(GetAffectedCells(itemA));
            cellsToClear.UnionWith(GetAffectedCells(itemB));
        }

        OnMatchScored?.Invoke(true);

        ProcessClearQueue(cellsToClear, null, () =>
        {
            CollapseAndRefill();
        });

        return true;
    }

    // Returns the set of cells a special candy's effect would clear.
    private HashSet<Vector2Int> GetAffectedCells(Item item)
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();

        switch (item.itemType)
        {
            case ItemType.HorizontalStriped:
                for (int x = 0; x < width; x++)
                    cells.Add(new Vector2Int(x, item.yIndex));
                break;

            case ItemType.VerticalStriped:
                for (int y = 0; y < height; y++)
                    cells.Add(new Vector2Int(item.xIndex, y));
                break;

            case ItemType.Wrapped:
                cells.UnionWith(GetWrappedBlastCells(item.xIndex, item.yIndex));
                break;

            case ItemType.ColorBomb:
                cells.UnionWith(GetCellsOfColor(item.itemColor));
                break;

            default:
                cells.Add(new Vector2Int(item.xIndex, item.yIndex));
                break;
        }

        return cells;
    }


    // Given a wrapped candy's position, returns a full 9-cell (3x3) block,
    // shifting the center inward near board edges so cells never get clipped
    // (e.g. a wrapped candy sitting in column 0 still destroys 9 cells, not 6).
    private HashSet<Vector2Int> GetWrappedBlastCells(int itemX, int itemY)
    {
        int clampedX = Mathf.Clamp(itemX, 1, width - 2);
        int clampedY = Mathf.Clamp(itemY, 1, height - 2);

        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                cells.Add(new Vector2Int(clampedX + dx, clampedY + dy));

        return cells;
    }

    private HashSet<Vector2Int> GetCellsOfColor(ItemColor color)
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject obj = gameBoard[x, y].item;
                if (obj != null && obj.GetComponent<Item>().itemColor == color)
                    cells.Add(new Vector2Int(x, y));
            }
        }

        return cells;
    }

    private void ProcessClearQueue(IEnumerable<Vector2Int> initialCells, HashSet<Vector2Int> protectedCells, System.Action onComplete)
    {
        HashSet<Vector2Int> clearSet = ComputeClearSet(initialCells, protectedCells);

        if (debugShowClearPreview)
        {
            StartCoroutine(ClearWithPreviewRoutine(clearSet, onComplete));
        }
        else
        {
            ExecuteClear(clearSet);
            onComplete?.Invoke();
        }
    }

    private HashSet<Vector2Int> ComputeClearSet(IEnumerable<Vector2Int> initialCells, HashSet<Vector2Int> protectedCells)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>(initialCells);
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        HashSet<Vector2Int> result = new HashSet<Vector2Int>();

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();

            if (visited.Contains(cell)) continue;
            if (!IsInBounds(cell.x, cell.y)) continue;

            visited.Add(cell);

            if (protectedCells != null && protectedCells.Contains(cell))
                continue;

            GameObject obj = gameBoard[cell.x, cell.y].item;
            if (obj == null) continue;

            result.Add(cell);

            Item item = obj.GetComponent<Item>();

            if (item.itemType != ItemType.Simple)
            {
                HashSet<Vector2Int> effectCells = item.itemType == ItemType.ColorBomb
                    ? GetCellsOfColor(item.itemColor)
                    : GetAffectedCells(item);

                foreach (var c in effectCells)
                    if (!visited.Contains(c))
                        queue.Enqueue(c);
            }
        }

        return result;
    }

    private void ExecuteClear(HashSet<Vector2Int> cellsToClear)
    {
        foreach (var cell in cellsToClear)
        {
            GameObject obj = gameBoard[cell.x, cell.y].item;
            if (obj == null) continue;

            Destroy(obj);
            gameBoard[cell.x, cell.y].item = null;
        }
    }

    private IEnumerator ClearWithPreviewRoutine(HashSet<Vector2Int> cellsToClear, System.Action onComplete)
    {
        List<Item> itemsToPulse = new List<Item>();

        foreach (var cell in cellsToClear)
        {
            GameObject obj = gameBoard[cell.x, cell.y].item;
            if (obj == null) continue;

            Item item = obj.GetComponent<Item>();
            item.StartClearPreview();
            itemsToPulse.Add(item);
        }

        yield return new WaitForSeconds(debugPreviewDuration);

        foreach (var item in itemsToPulse)
            item.StopClearPreview();

        ExecuteClear(cellsToClear);
        onComplete?.Invoke();
    }

    #endregion

    #region Gravity, Refill, Cascades

    private void CollapseAndRefill()
    {
        List<Item> itemsToAnimate = new List<Item>();

        for (int x = 0; x < width; x++)
        {
            CollapseColumn(x, itemsToAnimate);
            RefillColumn(x, itemsToAnimate);
        }

        AnimateFall(itemsToAnimate, () =>
        {
            List<MatchGroup> newMatches = MatchFinder.FindAllMatches(gameBoard, width, height);

            if (newMatches.Count > 0)
            {
                ResolveMatches(newMatches, new Vector2Int(-1, -1));
            }
            else
            {
                IsBusy = false;
            }
        });
    }

    private void CollapseColumn(int x, List<Item> itemsToAnimate)
    {
        int emptySlots = 0;

        for (int y = 0; y < height; y++)
        {
            if (gameBoard[x, y].item == null)
            {
                emptySlots++;
                continue;
            }

            if (emptySlots == 0) continue;

            int newY = y - emptySlots;

            GameObject obj = gameBoard[x, y].item;
            gameBoard[x, y].item = null;
            gameBoard[x, newY].item = obj;

            Item item = obj.GetComponent<Item>();
            item.SetCoordinates(x, newY);

            itemsToAnimate.Add(item);
        }

        columnEmptyCount[x] = emptySlots;
    }

    private void RefillColumn(int x, List<Item> itemsToAnimate)
    {
        int emptySlots = columnEmptyCount[x];

        for (int i = 0; i < emptySlots; i++)
        {
            int targetY = height - emptySlots + i;

            ItemColor color = (ItemColor)Random.Range(1, System.Enum.GetValues(typeof(ItemColor)).Length);
            GameObject prefab = GetPrefabForColor(color);

            Vector3 spawnPos = GetWorldPosition(x, height + i);

            GameObject newItem = Instantiate(prefab, spawnPos, Quaternion.identity, itemsParent);
            Item item = newItem.GetComponent<Item>();

            item.itemColor = color;
            item.itemType = ItemType.Simple;
            item.SetCoordinates(x, targetY);

            gameBoard[x, targetY].item = newItem;

            itemsToAnimate.Add(item);
        }
    }

    private void AnimateFall(List<Item> itemsToAnimate, System.Action onComplete)
    {
        if (itemsToAnimate.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        int remaining = itemsToAnimate.Count;

        foreach (Item item in itemsToAnimate)
        {
            item.isMoving = true;
            Vector3 target = GetWorldPosition(item.xIndex, item.yIndex);

            item.MoveTo(target, fallDuration, () =>
            {
                item.isMoving = false;
                remaining--;
                if (remaining == 0)
                    onComplete?.Invoke();
            });
        }
    }

    #endregion
}