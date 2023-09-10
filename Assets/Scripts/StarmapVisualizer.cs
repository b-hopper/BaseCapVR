using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

[SelectionBase]
public class StarmapVisualizer : MonoBehaviour
{
    [SerializeField] StarmapManager starmapManagerPrefab;
    [SerializeField] StarBase starbasePrefab;
    
    [SerializeField] float _targetSize = 1f; 
    
    [Header("Debug Team Colors")] public static Color neutralColor = Color.gray;
    public static Color p1color = Color.blue;
    public static Color p2color = Color.red;
    
    [SerializeField] internal StarmapManager.MapSizes mapSize;
    [SerializeField] internal PredefinedStarmap mapToVisualize;

    private void Start()
    {
        Destroy(this.gameObject);
    }

#if UNITY_EDITOR
    List<(/*Team*/ int, /*Level*/ int, /*Position*/ Vector3)> starPositions = new List<(int, int, Vector3)>();
    List<List<int>> connections = new List<List<int>>();
    
    private void OnDrawGizmos()
    {
        if (starmapManagerPrefab == null && mapToVisualize) return;
        
        if (mapToVisualize == null) // show bounds of randomly generated starfield
        {
            DrawMapBounds();
        }

        DrawStarmapVisualization();
        DrawConnectionsVisualization();
        DrawPlayerPositions();
    }

    public bool _regenerateMap;
    private StarmapManager.MapSizes _lastMapSize;
    private PredefinedStarmap _lastMap;
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
        transform.localScale = Vector3.one * CalculateScale(_targetSize);
        
        if (mapToVisualize != _lastMap)
        {
            _lastMap = mapToVisualize;
            _regenerateMap = true;
        } 
        
        if (mapSize != _lastMapSize)
        {
            _lastMapSize = mapSize;
            _regenerateMap = true;
        }
        
        if ((starPositions.Count == 0  && !_generationFailed) || _regenerateMap)
        {
            _regenerateMap = false;
            GenerateStarPositions();
        }
    }

    private void GeneratePredefMap()
    {
        foreach (var n in mapToVisualize.nodes)
        {
            starPositions.Add((n.StartingPlayer, n.StartingPlayer == -1 ? -1 : 1, n.StarBasePosition));
            var conn = n.ConnectedStarBaseIndices.Where(x => x < mapToVisualize.nodes.Count).ToList();
            connections.Add(conn);
        }
    }

    private void DrawMapBounds()
    {
        Vector3 pos = transform.position;

        RandomizedStarmap mapSettings = starmapManagerPrefab.randomizedStarmapSettings[(int)mapSize];
        Color c = Color.cyan;
        c.a = 0.25f;
        Gizmos.color = c;
        Vector3 size = new Vector3(mapSettings.starfieldSize.x * 2, mapSettings.starfieldSize.y * 2,
            mapSettings.starfieldSize.z * 2);
        size.Scale(transform.localScale);
        Gizmos.DrawWireCube(pos, size);
        c.a = 0.05f;
        Gizmos.color = c;
        Gizmos.DrawCube(pos, size);
        c = Color.magenta;
        Gizmos.color = c;

        float layerDistance = size.z / (mapSettings.numberOfLayers);

        for (int i = 1; i < mapSettings.numberOfLayers; i++)
        {
            var box = new Vector3(0, 0, -size.z * 0.5f + i * layerDistance);
            // Draw a line at each layer
            c.a = 0.25f;
            Gizmos.color = c;
            Gizmos.DrawWireCube(pos + box,
                new Vector3(size.x, size.y, 0f));
            c.a = 0.05f;
            Gizmos.color = c;
            Gizmos.DrawCube(pos + box,
                new Vector3(size.x, size.y, 0f));
        }
    }

    private bool _generationFailed = false;
    [ContextMenu("Generate Star Positions")]
    public void GenerateStarPositions()
    {
        starPositions.Clear();
        connections.Clear();
        
        _generationFailed = false;
        int attempts = 5;
        
        if (mapToVisualize != null)
        {
            GeneratePredefMap();
            return;
        }
        
        while (attempts > 0)
        {
            attempts--;
            if (StarmapManager.GenerateRandomPositions(starmapManagerPrefab.randomizedStarmapSettings[(int)mapSize],
                out var positions))
            {
                starPositions = positions.Select(p => (
                positions.IndexOf(p) == 0 ? 0 : 
                    positions.IndexOf(p) == positions.Count - 1 ? 1 : -1, // team
                
                positions.IndexOf(p) == 0 ? 1 : 
                    positions.IndexOf(p) == positions.Count - 1 ? 1 : -1, // level
                p  // position 
                )).ToList();
                connections = StarmapManager.GenerateStarConnections(starmapManagerPrefab.randomizedStarmapSettings[(int)mapSize].maxConnectionDistance);
                
                return;
            }
        }
        
        Debug.LogError("Failed to generate star positions after 5 attempts.");
        _generationFailed = true;
    }
    
    private void DrawStarmapVisualization()
    {
        if (starPositions.Count > 0)
        {
            for (var i = 0; i < starPositions.Count; i++)
            {
                var starPos = starPositions[i];
             
                Color c = starPos.Item1 switch
                {
                    0 => p1color,
                    1 => p2color,
                    _ => neutralColor
                };
                Gizmos.color = c;
                
                starPos.Item3.Scale(transform.localScale);               
                starPos.Item3 += transform.position;

                Mesh mesh;
                if (starPos.Item2 > 0) // 0 also uses neutral mesh
                {
                    mesh = starbasePrefab.baseMeshes[starPos.Item2];
                }
                else
                {
                    mesh = starbasePrefab.neutralMeshes[i % starbasePrefab.neutralMeshes.Length];
                }

                Gizmos.DrawMesh(mesh, starPos.Item3,
                    Quaternion.Euler(i * 10f, i * 50f, i * 20f),
                    transform.localScale);
            }
        }
    }

    private void DrawConnectionsVisualization()
    {
        foreach (var connection in connections)
        {
            var base1 = starPositions[connections.IndexOf(connection)].Item3;
            base1.Scale(transform.localScale);
            base1 += transform.position;
            foreach (var c in connection)
            {
                var base2 = starPositions[c].Item3;
                base2.Scale(transform.localScale);
                base2 += transform.position;
                Gizmos.color = Color.white * 0.6f + new Color(0, 0, 0, -0.4f);
                
                Gizmos.DrawLine(base1, base2);
            }
        }
    }
    
    private void DrawPlayerPositions()
    {
        List<Vector3> playerPositions = new List<Vector3>();
        var p1Pos = StarmapManager.GetPlayerPosition(0) + transform.position;
        var p2Pos = StarmapManager.GetPlayerPosition(1) + transform.position;
        playerPositions.Add(p1Pos);
        playerPositions.Add(p2Pos);

        foreach (var pos in playerPositions)
        {
            var pos2 = pos + Vector3.up * -1.5f;
            Gizmos.color = playerPositions.IndexOf(pos) + 1 == 1 ? p1color : p2color;
            Gizmos.DrawWireSphere(pos2 + Vector3.up * 1.6f, 0.2f);

            Gizmos.color = Color.gray;
            Gizmos.DrawLine(pos2 + Vector3.left * 0.1f, Vector3.up * 1.5f + pos2 + Vector3.left * 0.15f);
            Gizmos.DrawLine(pos2 + Vector3.right * 0.1f, Vector3.up * 1.5f + pos2 + Vector3.right * 0.15f);
            Gizmos.DrawLine(pos2 + Vector3.forward * 0.1f, Vector3.up * 1.5f + pos2 + Vector3.forward * 0.15f);
            Gizmos.DrawLine(pos2 + Vector3.back * 0.1f, Vector3.up * 1.5f + pos2 + Vector3.back * 0.15f);
            Handles.Label(pos2 + Vector3.up * 1.35f,
                "Player " + (playerPositions.IndexOf(pos) + 1) + " Position", // Hack, but it works for debugging
                new GUIStyle
                {
                    normal = new GUIStyleState()
                        { textColor = playerPositions.IndexOf(pos) + 1 == 1 ? p1color : p2color },
                    alignment = TextAnchor.MiddleCenter
                });
        }
    }


    private float CalculateScale(float targetSize)
    {
        if (mapToVisualize != null)
        {
            if (mapToVisualize.TotalSize() == 0f) return 0.25f;
            return targetSize / (mapToVisualize.TotalSize() * 2f);
        }

        return targetSize / (starmapManagerPrefab.randomizedStarmapSettings[(int)mapSize].starfieldSize.z * 2f);
    }
#endif
}
