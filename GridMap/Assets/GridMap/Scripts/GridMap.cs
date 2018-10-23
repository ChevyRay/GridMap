using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GridMap", menuName = "Grid Map", order = 101)]
public class GridMap : ScriptableObject
{
    public int size = 9;
    public Rect roomBounds = new Rect(-5, -5, 10, 10);
    public Vector3 roomBoundsAxis = Vector3.up;
}
