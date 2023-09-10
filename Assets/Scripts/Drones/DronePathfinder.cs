using System.Collections.Generic;
using UnityEngine;

public static class DronePathfinder
{
    private static float[,] distances;
    private static int[,] nextBase;

    // TODO probably put this in a setting somewhere, for easy toggle
    private static bool SHOW_DEBUG = false;

    public static void StartDronePathfinder()
    {
        Initialize();
        PrintData();
        GeneratePaths();
        PrintData();
    }

    public static void Initialize()
    {
        distances = new float[StarMapData.StarBases.Count, StarMapData.StarBases.Count];
        nextBase = new int[StarMapData.StarBases.Count, StarMapData.StarBases.Count];


        // map the distance between each pair of nodes
        foreach (StarBase starBase in StarMapData.StarBases)
        {
             List<(StarBase, LinePath)> connectedBases = StarMapData.StarLines[starBase];
            for (int currentBaseId = 0; currentBaseId < StarMapData.StarBases.Count; currentBaseId++)
            {
                if (connectedBases.Exists(x => x.Item2.CheckIds(starBase.BaseId, currentBaseId)))
                {
                    // if there's a connection, map the distance
                    (StarBase, LinePath) path = connectedBases.Find(x => x.Item2.CheckIds(starBase.BaseId, currentBaseId));
                    distances[starBase.BaseId, currentBaseId] = path.Item2.LineDistance;
                    nextBase[starBase.BaseId, currentBaseId] = currentBaseId;
                }
                else
                {
                    // if there's no link, distance is max, path is -1
                    distances[starBase.BaseId, currentBaseId] = float.MaxValue;
                    nextBase[starBase.BaseId, currentBaseId] = -1;
                }
            }
        }
    }

    public static void GeneratePaths()
    {
        foreach (StarBase k in StarMapData.StarBases)
        {
            foreach (StarBase i in StarMapData.StarBases)
            {
                foreach (StarBase j in StarMapData.StarBases)
                {
                    if (distances[i.BaseId, k.BaseId] == float.MaxValue || distances[k.BaseId, j.BaseId] == float.MaxValue)
                        continue;

                    if (distances[i.BaseId, j.BaseId] >
                        distances[i.BaseId, k.BaseId] + distances[k.BaseId, j.BaseId])
                    {
                        distances[i.BaseId, j.BaseId] = distances[i.BaseId, k.BaseId] + distances[k.BaseId, j.BaseId];
                        nextBase[i.BaseId, j.BaseId] = nextBase[i.BaseId, k.BaseId];
                    }
                }
            }
        }
    }

    private static void PrintData()
    {
        if (!SHOW_DEBUG) return;
        string distanceData = "";
        string nextBaseData = "";
        for (int i = 0; i < StarMapData.StarBases.Count; i++)
        {
            distanceData += $"{i}: ";
            nextBaseData += $"{i}: ";
            for (int j = 0; j < StarMapData.StarBases.Count; j++)
            {
                distanceData += $"{distances[i, j]}, ";
                nextBaseData += $"{nextBase[i, j]}, ";
            }
            distanceData += "\n";
            nextBaseData += "\n";
        }

        Debug.Log($"distance data: {distanceData}");
        Debug.Log($"nextBase data: {nextBaseData}");
    }

    public static List<int> GetPath(int startBaseId, int endBaseId)
    {
        if (nextBase[startBaseId, endBaseId] == -1) return null;

        List<int> path = new List<int>();
        path.Add(startBaseId);

        while (startBaseId != endBaseId)
        {
            startBaseId = nextBase[startBaseId, endBaseId];
            path.Add(startBaseId);
        }

        string pathString = "[ ";
        foreach (int node in path)
        {
            pathString += $"{node} ";
        }

        pathString += "]";
        // Debug.Log($"Discovered path from {startBaseId} to {endBaseId}: {pathString}");
        return path;
    }
}