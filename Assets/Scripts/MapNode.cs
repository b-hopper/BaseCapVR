using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct MapNode
{
    public Vector3 StarBasePosition;
    public List<int> ConnectedStarBaseIndices;
    public int StartingPlayer;
    public int startingDroneCount;
}