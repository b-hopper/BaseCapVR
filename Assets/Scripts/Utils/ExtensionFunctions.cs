using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionFunctions
{
    public static StarBaseData ToStarBaseData(this MapNode node)
    {
        return new StarBaseData
        {
            droneCount = node.startingDroneCount,
            team = (int)node.StartingPlayer,
            upgradeLevel = node.StartingPlayer == -1 ? 0 : 1,
        };
    }
}
