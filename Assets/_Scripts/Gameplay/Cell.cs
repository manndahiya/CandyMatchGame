using UnityEngine;

public class Cell
{
    public bool isUsable;
    public GameObject item;

    public Cell(bool usable)
    {
        isUsable = usable;
        item = null;
    }
}
