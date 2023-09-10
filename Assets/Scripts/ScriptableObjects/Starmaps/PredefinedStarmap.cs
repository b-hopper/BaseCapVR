using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PredefinedStarmap", menuName = "ScriptableObjects/PredefinedStarmap", order = 1)]
public class PredefinedStarmap : ScriptableObject
{
    public List<MapNode> nodes;

    public float TotalSize()
    {
        float minZ = 0f, maxZ = 0f;
        foreach (var node in nodes)
        {
            minZ = Mathf.Min(minZ, node.StarBasePosition.z);
            maxZ = Mathf.Max(maxZ, node.StarBasePosition.z);
        }
        
        return maxZ - minZ;
    }

#if UNITY_EDITOR
    private static StarmapVisualizer visualizer;
    private void OnValidate()
    {
        if (visualizer == null)
        {
            visualizer = FindObjectOfType<StarmapVisualizer>();
        }
        if (visualizer != null && visualizer.mapToVisualize == this)
        { // if this is the map that's currently being visualized, regenerate it to reflect changes
            visualizer.GenerateStarPositions();
        }
    }
#endif
}