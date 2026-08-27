using System.Collections.Generic;
using UnityEngine;

public class MatchGroup
{
    public List<Vector2Int> cells = new List<Vector2Int>();
    public bool isHorizontal;
    public bool isLOrTShape;

    // Only meaningful when isLOrTShape is true — the geometric "center" cell
    // of the combined shape, used as the anchor for the Wrapped candy's
    // spawn position (and therefore its later 3x3 blast center).
    public Vector2Int anchorCell;

    public int Count => cells.Count;
}

public static class MatchFinder
{
    public static List<MatchGroup> FindAllMatches(Cell[,] board, int width, int height)
    {
        List<MatchGroup> horizontal = FindLineMatches(board, width, height, true);
        List<MatchGroup> vertical = FindLineMatches(board, width, height, false);

        return CombineIntersecting(horizontal, vertical);
    }

    private static List<MatchGroup> FindLineMatches(Cell[,] board, int width, int height, bool horizontal)
    {
        var matches = new List<MatchGroup>();
        int outerCount = horizontal ? height : width;
        int innerCount = horizontal ? width : height;

        for (int outer = 0; outer < outerCount; outer++)
        {
            int runLength = 1;

            for (int inner = 1; inner <= innerCount; inner++)
            {
                bool sameAsPrevious = false;

                if (inner < innerCount)
                {
                    Vector2Int current = horizontal ? new Vector2Int(inner, outer) : new Vector2Int(outer, inner);
                    Vector2Int previous = horizontal ? new Vector2Int(inner - 1, outer) : new Vector2Int(outer, inner - 1);
                    sameAsPrevious = GetColor(board, current) == GetColor(board, previous);
                }

                if (sameAsPrevious)
                {
                    runLength++;
                }
                else
                {
                    if (runLength >= 3)
                        matches.Add(BuildGroup(inner - runLength, inner, outer, horizontal));

                    runLength = 1;
                }
            }
        }

        return matches;
    }

    private static MatchGroup BuildGroup(int start, int end, int outer, bool horizontal)
    {
        var group = new MatchGroup { isHorizontal = horizontal };
        for (int i = start; i < end; i++)
            group.cells.Add(horizontal ? new Vector2Int(i, outer) : new Vector2Int(outer, i));
        return group;
    }

    private static ItemColor GetColor(Cell[,] board, Vector2Int pos)
    {
        return board[pos.x, pos.y].item.GetComponent<Item>().itemColor;
    }

    private static List<MatchGroup> CombineIntersecting(List<MatchGroup> horizontal, List<MatchGroup> vertical)
    {
        var result = new List<MatchGroup>();
        var usedH = new bool[horizontal.Count];
        var usedV = new bool[vertical.Count];

        for (int i = 0; i < horizontal.Count; i++)
        {
            for (int j = 0; j < vertical.Count; j++)
            {
                if (usedV[j]) continue;

                // Find the exact cell shared by both matches (there's only ever one,
                // since two straight lines can only cross at a single point).
                Vector2Int? sharedCell = null;
                foreach (var c in horizontal[i].cells)
                {
                    if (vertical[j].cells.Contains(c))
                    {
                        sharedCell = c;
                        break;
                    }
                }

                if (sharedCell == null) continue; // no overlap, not an L/T

                var merged = new MatchGroup { isLOrTShape = true };
                merged.cells.AddRange(horizontal[i].cells);
                foreach (var c in vertical[j].cells)
                    if (!merged.cells.Contains(c)) merged.cells.Add(c);

                // The shared cell is only guaranteed to be the true "middle" of the
                // combined shape for a T (where the junction sits centrally). For an
                // L, the shared cell is a bounding-box corner — e.g. a horizontal run
                // ending at (2,0) joined to a vertical run starting at (2,0) puts the
                // "anchor" at the far corner of the shape instead of its center.
                // Use the cell closest to the shape's centroid instead, falling back
                // to the shared cell only when it's genuinely tied for the middle.
                merged.anchorCell = GetMiddleMostCell(merged.cells, sharedCell.Value);

                result.Add(merged);
                usedH[i] = true;
                usedV[j] = true;
                break;
            }
        }

        for (int i = 0; i < horizontal.Count; i++) if (!usedH[i]) result.Add(horizontal[i]);
        for (int j = 0; j < vertical.Count; j++) if (!usedV[j]) result.Add(vertical[j]);

        return result;
    }

    // Returns the cell in `cells` closest to their combined centroid — i.e. the
    // most "middle" actual cell of the shape. `preferredTie` is used to break
    // ties (or near-ties) deterministically, favoring the natural junction point
    // when multiple cells are equally central.
    private static Vector2Int GetMiddleMostCell(List<Vector2Int> cells, Vector2Int preferredTie)
    {
        const float epsilon = 0.0001f;

        float avgX = 0f, avgY = 0f;
        foreach (var c in cells)
        {
            avgX += c.x;
            avgY += c.y;
        }
        avgX /= cells.Count;
        avgY /= cells.Count;

        Vector2Int best = cells[0];
        float bestDist = float.MaxValue;

        foreach (var c in cells)
        {
            float dx = c.x - avgX;
            float dy = c.y - avgY;
            float dist = dx * dx + dy * dy;

            if (dist < bestDist - epsilon)
            {
                bestDist = dist;
                best = c;
            }
            else if (Mathf.Abs(dist - bestDist) <= epsilon && c == preferredTie)
            {
                // Near-tie: prefer the shared junction cell for a more natural feel.
                best = c;
            }
        }

        return best;
    }
}