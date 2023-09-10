using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Fusion;
using Tilia.Interactions.Interactables.Interactables;
using Tilia.Interactions.Interactables.Interactors.ComponentTags;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class StarmapManager : NetworkBehaviour
{
    public static StarmapManager Instance => _instance;
    private static StarmapManager _instance;
    
    [SerializeField] private Transform starfieldParent;
    [SerializeField]
    [Tooltip("The object that will be detached from the starmap network object when the game starts.\n " +
             "This is to allow the starmap to be moved around by the player without moving it for everyone else.")]
    private Transform objectToDetach;

    public StarBase starBasePrefab;

    [SerializeField] private LinePath lineRendererPrefab;

    [SerializeField] internal MapSizes mapSize;
    
    [SerializeField] private int startingDroneCount = 10;

    public PredefinedStarmap[] predefinedStarmapList;
    public RandomizedStarmap[] randomizedStarmapSettings;
    internal PredefinedStarmap predefinedStarmap;
    
    private static float mapGenTimeoutTime = 5f;

    private IEnumerator<StarBase> starBaseEnumerator; // for iterating over the existing bases on the client

    private List<StarBaseData> _starBaseDatas = new(); // for populating the network object


    private bool _linesInitialized = false;

    private Transform _starfieldScaleTransform;
    public static float CurrentStarfieldScale => _instance._starfieldScaleTransform.lossyScale.x;

    public override async void Spawned()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Runner.Despawn(Object);
        }

        VerifyMapSettingsExist();
        _starfieldScaleTransform = starfieldParent.transform;

        await Initialize();
        if (randomizedStarmapSettings[(int)mapSize].numberOfLayers % 2 == 0)
            randomizedStarmapSettings[(int)mapSize].numberOfLayers++; // Make sure the number of layers is odd.
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_instance == this)
        {
            _instance = null;
            initialized = false;
            ClearStarmap();
            Destroy(objectToDetach.gameObject);
            GameStateManager.BeforeGameStart.Remove(PrepareStarmap);
        }
        
        base.Despawned(runner, hasState);
    }

    private void VerifyMapSettingsExist()
    {
        if (randomizedStarmapSettings == null)
        {
            Debug.LogError("Error! No randomized map settings found");
        }
        else if (randomizedStarmapSettings.Length != Enum.GetValues(typeof(MapSizes)).Length)
        {
            Debug.LogError("Error! Missing settings for some random map sizes!");
        }
    }


    public static Vector3 GetPlayerPosition(int team)
    {
        Vector3 pos = Vector3.zero;
        float offset = 1.5f;
        
        switch (team)
        {
            case 0:
                pos = new Vector3(0, 0.25f, -offset);
                break;
            case 1:
                pos = new Vector3(0, 0.25f, offset);
                break;
            default:
                pos = Vector3.zero;
                break;
        }
        
        return pos;
    }

    #region Initialization

    [HideInInspector] public bool initialized = false;
    
    private async UniTask Initialize()
    {
        if (initialized) return;
        await UniTask.WaitUntil(() => GameStateManager.Instance != null);

        objectToDetach.parent = null;
        
        Random.InitState(GameStateManager.Instance.Seed);

        StarMapData.SetStarBases(new List<StarBase>());
        
        GameStateManager.BeforeGameStart.Add(PrepareStarmap);
        
        objectToDetach.gameObject.SetActive(true);
    }

    private float CalculateScale(float targetSize)
    {
        if (predefinedStarmap != null)
        {
            if (predefinedStarmap.TotalSize() == 0f) return 0.25f;
            return targetSize / (predefinedStarmap.TotalSize() * 2f);
        }

        return targetSize / (randomizedStarmapSettings[(int)mapSize].starfieldSize.z * 2f);
    }

    [Networked] private int _nodeCount { get; set; }

    private void PrepareStarmap() => PrepareStarmap(false);
    
    private void PrepareStarmap(bool isRemake)
    {
        PrepareStarmapAsync(isRemake).Forget();
    }
    private async UniTask PrepareStarmapAsync(bool isRemake = false)
    { 
        await UniTask.WaitUntil(() => TeamAnalyticsManager.Instance != null);
        objectToDetach.position = Vector3.zero;
        
        starfieldParent.localScale = Vector3.one * CalculateScale(1f);
        
        if (NetworkManager.Instance.HasStateAuthority)
        {
            await ServerPrepareStarmap(isRemake);
        }
        else
        {            
            await ClientPrepareStarmap();
        }
    }

    private async UniTask ServerPrepareStarmap(bool isRemake)
    {
        StarBase[] bases = FindObjectsOfType<StarBase>();

        if (bases?.Length > 0 && !isRemake)
        {
            if (!bases[0].Object.IsInSimulation) Debug.LogError($"Star base has been despawned");
            List<StarBase> baseList = bases.ToList();
            StarMapData.SetStarBases(baseList.OrderBy(x => x.BaseId).ToList());
            starBaseEnumerator = StarMapData.StarBases.GetEnumerator();
            starBaseEnumerator.MoveNext(); // move the pointer to the first base in the list
        }

        if (predefinedStarmap == null)
        {
            GenerateRandomStarmap();
            await UniTask.WaitUntil(() =>
            {
                return (StarMapData.StarBases.Count > 0 && 
                    StarMapData.StarBases.All(x => x.transform.localPosition != Vector3.zero));
            });
            // On the client, the starbases are spawned in the center of the map,
            // so we wait until they have moved to their correct positions.

            InitializeRandomizedStarConnections(randomizedStarmapSettings[(int)mapSize].maxConnectionDistance);
        }
        else
        {
            _nodeCount = predefinedStarmap.nodes.Count;
            LoadStarmapFromScriptableObject();
        }

        SendMapDataToServer();
        await UniTask.WaitUntil(() => StarMapData.StarBases.Count == _nodeCount);
        DrawLines();
        await UniTask.WaitUntil(() => _linesInitialized);
        DronePathfinder.StartDronePathfinder();
        initialized = true;

    }

    private async UniTask ClientPrepareStarmap()
    {
        StarBase[] bases = FindObjectsOfType<StarBase>();
        while (bases?.Length != _nodeCount)
        {
            bases = FindObjectsOfType<StarBase>();
            await UniTask.WaitForFixedUpdate();
        }

        if (!bases[0].Object.IsInSimulation) Debug.LogError($"Star base has been despawned");
        List<StarBase> baseList = bases.ToList();
        StarMapData.SetStarBases(baseList.OrderBy(x => x.BaseId).ToList());
        starBaseEnumerator = StarMapData.StarBases.GetEnumerator();
        starBaseEnumerator.MoveNext(); // move the pointer to the first base in the list

        while (starBaseEnumerator.Current != null)
        {
            ClientSpawnStarBase();
        }

        PopulateStarbases();

        // starbase positions don't get set until they've all been spawned and their parents changed, so we need to wait for that,
        // or else the lines will be drawn at the origin
        await UniTask.WaitUntil(() => StarMapData.StarBases.All(x => x.transform.parent != null));

        //if (predefinedStarmap == null) InitializeRandomizedStarConnections(randomizedStarmapSettings[(int)mapSize].maxConnectionDistance);
        ClientInitializeStarConnections();
        
        DrawLines();
        await UniTask.WaitUntil(() => _linesInitialized);
        DronePathfinder.StartDronePathfinder();
        initialized = true;
    }

    private void ServerSpawnStarBase(Vector3 pos, int id, MapNode mapNode)
    {
        if (pos == Vector3.zero)
        {
            // For some reason, if the position is exactly (0,0,0), interactions with the starbase will not work.
            // So we offset it by a tiny amount, which seems to fix it.
            // TODO: Figure out why?
            
            pos = new Vector3(0.0001f, 0.0001f, 0.0001f);
        }
        pos *= CurrentStarfieldScale;
        StarBase starBase;
        starBase = Runner.Spawn(starBasePrefab, pos, Quaternion.identity, onBeforeSpawned: (_, obj) =>
        {
            obj.transform.parent = starfieldParent;
        });

        _starBaseDatas.Add(starBase.Populate(id, mapNode));
        StarMapData.StarBases.Add(starBase);
        
    }

    private void ServerSpawnStarBase(Vector3 pos)
    {
        pos *= CurrentStarfieldScale;
        var starBase = Runner.Spawn(starBasePrefab, pos, Quaternion.identity);
        StarMapData.StarBases.Add(starBase);

        starBase.transform.parent = starfieldParent;
        starBase.transform.localScale = Vector3.one; // Since we parented it to the starfield (which may have strange scaling),
                                                     // we need to reset the scale.
    }

    private void ClientSpawnStarBase()
    {
        var starBase = starBaseEnumerator.Current;
        if (starBase == null)
        {
            Debug.LogError("Starbase is null");
            return;
        }

        starBase.transform.parent = starfieldParent;
        starBase.transform.localScale = Vector3.one;  
        starBaseEnumerator.MoveNext();
    }

    private void PopulateRandomBase(StarBase starBase, int id, int teamIdx)
    {
        _starBaseDatas.Add(starBase.Populate(id, teamIdx, teamIdx >= 0 ? startingDroneCount : 0));
    }

    public void HighlightMultipleStarBases(Predicate<StarBase> p)
    {
        ClearAllHighlights();
        var bases = StarMapData.StarBases.Where(p.Invoke).ToList();
        foreach (var base1 in bases)
        {
            base1.EnableOutline();
        }
    }

    public void ClearAllHighlights()
    {
        foreach (var s in StarMapData.StarBases)
        {
            s.DisableOutline();
        }
    }

    public void HighlightMultipleStarBases(List<int> ids)
    {
        ClearAllHighlights();
        foreach (var id in ids)
        {
            StarMapData.StarBases[id].EnableOutline();
        }
    }
    
    [ContextMenu("Regenerate Random Starmap")]
    private void TEST_ReInit()
    {
        ClearStarmap();

        Initialize().Forget();
    }

    public void ClearStarmap()
    {
        initialized = false;
        _linesInitialized = false;
        foreach (var line in StarMapData.StarLines)
        {
            foreach (var l in line.Value)
            {
                Destroy(l.Item2.gameObject);
            }
        }

        if (HasStateAuthority)
        {
            foreach (var base1 in StarMapData.StarBases)
            {
                Runner.Despawn(base1.GetComponent<NetworkObject>());
            }
        }

        _starBaseDatas = new List<StarBaseData>();
        StarMapData.ResetStarLines();
        StarMapData.ResetStarBases();
        if (StarBaseManager.Instance != null) StarBaseManager.Instance.ResetStarBaseData();
    }

    private void SendMapDataToServer()
    {
        StartCoroutine(WaitForClientToConnect());
    }

    private IEnumerator WaitForClientToConnect()
    {
        if (!HasStateAuthority) yield break;
        while (StarBaseManager.Instance == null)
        {
            yield return null;
        }


        StarBaseManager.Instance.PopulateStarBaseData(_starBaseDatas);
    }

    #region Predefined Starmap Generation

    private void LoadStarmapFromScriptableObject()
    {
        InitializePredefinedStars();
        InitializePredefinedStarConnections();
    }

    private void InitializePredefinedStars()
    {
        int id = 0;
        foreach (MapNode node in predefinedStarmap.nodes)
        {
            ServerSpawnStarBase(node.StarBasePosition, id, node);
            id++;
        }
    }

    private void InitializePredefinedStarConnections()
    {
        StarMapData.ResetStarConnections();
        for (var i = 0; i < predefinedStarmap.nodes.Count; i++)
        {
            var node = predefinedStarmap.nodes[i];
            StarMapData.StarConnections.Add(StarMapData.StarBases[i], new List<StarBase>());
            foreach (var connection in node.ConnectedStarBaseIndices)
            {
                StarMapData.StarConnections[StarMapData.StarBases[i]].Add(StarMapData.StarBases[connection]);
            }

            // populate star with connection data to help with drone pathfinding
            StarMapData.StarBases[i].PopulateConnectionData(StarMapData.StarConnections[StarMapData.StarBases[i]]);
        }
    }

    #endregion

    #region Starmap Management

    [Serializable]
    public struct SerializableStarConnection
    {
        public Vector3 StarBasePosition;
        public List<int> ConnectedStarBaseIndices;
        public int StartingPlayer;
    }

    [Serializable]
    public class SerializableStarmap
    {
        public List<MapNode> Connections;
    }

    [ContextMenu("Save Starmap as JSON")]
    public void SaveStarmapAsJson()
    {
        var serializableStarmap = new SerializableStarmap { Connections = new List<MapNode>() };

        foreach (var pair in StarMapData.StarConnections)
        {
            var connectedStarBaseIndices = pair.Value.Select(base1 => StarMapData.StarBases.IndexOf(base1)).ToList();
            serializableStarmap.Connections.Add(new MapNode
            {
                StarBasePosition = pair.Key.transform.localPosition, ConnectedStarBaseIndices = connectedStarBaseIndices,
                StartingPlayer = pair.Key.TeamIndex
            });
        }

        var json = JsonUtility.ToJson(serializableStarmap, true);
        string filePath = Path.Combine(Application.dataPath, "Starmap.json");
        File.WriteAllText(filePath, json);
    }

    [ContextMenu("Load Starmap from JSON")]
    public void LoadStarmapFromJson()
    {
        ClearStarmap();
        string filePath = Path.Combine(Application.dataPath, "Starmap.json");
        var json = File.ReadAllText(filePath);

        var serializableStarmap = JsonUtility.FromJson<SerializableStarmap>(json);

        StarMapData.ResetStarConnections();
        int id = 0;
        foreach (var connection in serializableStarmap.Connections)
        {
            ServerSpawnStarBase(connection.StarBasePosition, id, connection);
            id++;
        }

        for (var i = 0; i < serializableStarmap.Connections.Count; i++)
        {
            var connection = serializableStarmap.Connections[i];
            StarMapData.StarConnections.Add(StarMapData.StarBases[i], new List<StarBase>());
            foreach (var connectedStarBaseIndex in connection.ConnectedStarBaseIndices)
            {
                StarMapData.StarConnections[StarMapData.StarBases[i]]
                    .Add(StarMapData.StarBases[connectedStarBaseIndex]);
            }
        }

        DrawLines();
    }

    #endregion

    #region Random Starmap Generation

    private void GenerateRandomStarmap()
    {
        InitializeRandomStars(randomizedStarmapSettings[(int)mapSize]);
    }
    
    private int PredictStarBaseCount(int numLayers)
    {
        int starBaseCount = 2;
        int midPoint = numLayers / 2;
        for (int i = 1; i <= midPoint; i++)
        {
            int numBasesInLayer = 2 * (i + 1); 
            if (i == midPoint && numLayers % 2 == 1)
            {
                numBasesInLayer = i + 1; 
            }
            starBaseCount += numBasesInLayer;
        }
        return starBaseCount;
    }

    private void InitializeRandomStars(RandomizedStarmap mapSettings)
    {
        _nodeCount = PredictStarBaseCount(mapSettings.numberOfLayers);

        int attempts = 5;
        
        while (attempts > 0)
        {
            attempts--;
            if (GenerateRandomPositions(mapSettings, out var positions))
            {

                for (int i = 0; i < positions.Count; i++)
                {
                    ServerSpawnStarBase(positions[i]);
                }

                PopulateStarbases();
                return;
            }
        }

        // TODO: handle case where we fail to generate a valid starmap in a more player-friendly way
        Debug.LogError("Failed to generate a valid starmap after 5 attempts.");
    }

    public static bool GenerateRandomPositions(RandomizedStarmap mapSettings, out List<Vector3> positions)
    {
        float layerDistance = (2 * mapSettings.starfieldSize.z) / (mapSettings.numberOfLayers - 1);
        
        positions = new List<Vector3>();
        var pos = new Vector3(0, 0, -mapSettings.starfieldSize.z);
        positions.Add(pos);
        _starbasePositionsTmp.Add(pos);
        
        //ServerSpawnStarBase(new Vector3(0, 0, -mapSettings.starfieldSize.z));
        for (int i = 1; i <= mapSettings.numberOfLayers / 2; i++)
        {
            int numBasesInLayer = i + 1;

            for (int j = 0; j < numBasesInLayer; j++)
            {
                int k = 0;
                Vector3 position;
                do
                {
                    // Generate a random position within the layer.
                    position = new Vector3(
                        Random.Range(-mapSettings.starfieldSize.x, mapSettings.starfieldSize.x),
                        Random.Range(-mapSettings.starfieldSize.y, mapSettings.starfieldSize.y),
                        -mapSettings.starfieldSize.z + i * layerDistance +
                        Random.Range(-layerDistance / 3, layerDistance / 3)
                    );
                    k++;
                    if (k > 200000) 
                    {
                        Debug.LogWarning(
                            $"Could not find a valid position for a star base. Ensure that the starfield size is large enough, " +
                            "or that the number of layers is small enough, " +
                            "or that the minimum node distance is small enough.");
                        _starbasePositionsTmp.Clear();
                        return false;
                    }
                } while (!IsWithinRangeOfTwoBases(position, mapSettings.minNodeDistance, mapSettings.maxNodeDistance));

                if (i == mapSettings.numberOfLayers / 2)
                {
                    // Ensure symmetry at the midpoint.
                    position = new Vector3(position.x, position.y, 0);
                }

                // Create a star base at the generated position.
                positions.Add(position);
                _starbasePositionsTmp.Add(position);
                //ServerSpawnStarBase(position);

                // Create a symmetric star base on the other side of the map, unless we're at the midpoint.
                if (i != mapSettings.numberOfLayers / 2)
                {
                    positions.Add(new Vector3(position.x, position.y, -position.z));
                    _starbasePositionsTmp.Add(new Vector3(position.x, position.y, -position.z));
                    //ServerSpawnStarBase(new Vector3(position.x, position.y, -position.z));
                }
            }
        }
        
        positions.Add(new Vector3(0, 0, mapSettings.starfieldSize.z));
        _starbasePositionsTmp.Add(new Vector3(0, 0, mapSettings.starfieldSize.z));
        //ServerSpawnStarBase(new Vector3(0, 0, mapSettings.starfieldSize.z));

        positions = positions.OrderBy(a => a.z).ToList();
        _starbasePositionsTmp = _starbasePositionsTmp.OrderBy(a => a.z).ToList();
        
        return true;
    }
    
    private static List<Vector3> _starbasePositionsTmp = new List<Vector3>();
    private static bool IsWithinRangeOfTwoBases(Vector3 position, float minNodeDistance, float maxNodeDistance)
    {
        int withinRangeCount = 0;
        
        if (_starbasePositionsTmp.Count <= 1)
        {
            return true;
        }
        
        foreach (var b in _starbasePositionsTmp)
        {
            var dist = Vector3.Distance(position, b);
            if (dist <= maxNodeDistance)
            {
                bool foundPossiblePosition = true;
                foreach (var c in _starbasePositionsTmp)
                {
                    var nearDist = Vector3.Distance(position, c);
                    if (nearDist < minNodeDistance)
                    {
                        foundPossiblePosition = false;
                        break;
                    }
                }

                if (foundPossiblePosition)
                {
                    withinRangeCount++;
                }
            }
        }
        
        if (_starbasePositionsTmp.Count == 2)
        {
            return withinRangeCount >= 1;
        }

        return withinRangeCount >= 2;
    }

    private void PopulateStarbases()
    {
        // sort the list so the nodes get id's in a better order for the pathfinding algorithm
        // if the enumerator exists they're already going to be correct order
        if (starBaseEnumerator == null)
            StarMapData.SortStarBasesByPosition();
        PopulateRandomBase(StarMapData.StarBases[0], 0, 0);
        PopulateRandomBase(StarMapData.StarBases[^1], StarMapData.StarBases.Count - 1, 1);
        for (int i = 1; i < StarMapData.StarBases.Count - 1; i++)
        {
            PopulateRandomBase(StarMapData.StarBases[i], i, -1);
        }
    }
    
    private void ClientInitializeStarConnections()
    {
        StarMapData.ResetStarConnections();
        StarMapData.ResetStarLines();

        if (predefinedStarmap != null)
        {
            InitializePredefinedStarConnections();
            return;
        }
        foreach (var star in StarMapData.StarBases)
        {
            StarMapData.StarConnections[star] = new List<StarBase>();
            
            foreach (var connection in star.ConnectedBaseIds)
            {
                if (connection == -1) continue;
                StarMapData.StarConnections[star].Add(StarMapData.StarBases[connection]);
            }
            
            star.PopulateConnectionData(StarMapData.StarConnections[star]);
        }
    }

    public static List<List<int>> GenerateStarConnections(float maxConnectionDistance)
    {
        List<List<int>> starConnections = new List<List<int>>();
        for (int i = 0; i < _starbasePositionsTmp.Count; i++)
        {
            List<int> currentStarConnections = new List<int>();
            //_starbasePositionsTmp[i]
            
            float closestDistance = float.MaxValue;
            float secondClosestDistance = float.MaxValue;
            int closestIndex = -1;
            int secondClosestIndex = -1;
            
            for (int j = 0; j < _starbasePositionsTmp.Count; j++)
            {
                if (i == j) continue;

                float distance = Vector3.Distance(_starbasePositionsTmp[i], _starbasePositionsTmp[j]);

                if (closestDistance > distance)
                {
                    secondClosestDistance = closestDistance;
                    secondClosestIndex = closestIndex;
                    closestDistance = distance;
                    closestIndex = j;
                }
                else if (secondClosestDistance > distance)
                {
                    secondClosestDistance = distance;
                    secondClosestIndex = j;
                }

                if (distance <= maxConnectionDistance)
                {
                    currentStarConnections.Add(j);
                }
            }
            
            if (currentStarConnections.Count == 0)
            {
                if (closestIndex != -1) currentStarConnections.Add(closestIndex);
                if (secondClosestIndex != -1) currentStarConnections.Add(secondClosestIndex);
            }
            else if (currentStarConnections.Count == 1)
            {
                if (secondClosestIndex != -1) currentStarConnections.Add(secondClosestIndex);
            } 
            
            starConnections.Add(currentStarConnections);
        }

        foreach (var cnn in starConnections)
        {
            // Ensure that all connections are two-way
            foreach (var cnn2 in cnn)
            {
                if (starConnections.Count <= cnn2) continue;
                
                if (!starConnections[cnn2].Contains(starConnections.IndexOf(cnn)))
                { // make sure the connection is two-way
                    starConnections[cnn2].Add(starConnections.IndexOf(cnn));
                }
            }
        }

        _starbasePositionsTmp.Clear();
        
        return starConnections;
    }

    private void InitializeRandomizedStarConnections(float maxConnectionDistance)
    {
        StarMapData.ResetStarConnections();
        StarMapData.ResetStarLines();
        
        if (starBaseEnumerator == null)
            StarMapData.SortStarBasesByPosition();

        List<List<int>> starConnections = GenerateStarConnections(maxConnectionDistance);

        for (int i = 0; i < StarMapData.StarBases.Count; i++)
        {
            StarMapData.StarConnections.Add(StarMapData.StarBases[i], new List<StarBase>());
            foreach (var connection in starConnections[i])
            {
                StarBase star = StarMapData.StarBases[i];
                StarMapData.StarConnections[star].Add(StarMapData.StarBases[connection]);
            }

            // populate star with connection data to help with drone pathfinding
            StarMapData.StarBases[i].PopulateConnectionData(StarMapData.StarConnections[StarMapData.StarBases[i]]);
        }

        EnsureHomeBasesAreConnected();
    }


    private void AddConnectionsToDisconnectedStar(StarBase star)
    {
        Vector3 curPosition = star.transform.localPosition;
        StarBase closest = StarMapData.StarBases[0];
        StarBase secondClosest = StarMapData.StarBases[0];

        float closestDistance =
            Math.Abs(Vector3.Distance(curPosition, closest.gameObject.transform.localPosition));
        float secondClosestDistance =
            Math.Abs(Vector3.Distance(curPosition, secondClosest.gameObject.transform.localPosition));
        foreach (StarBase otherBase in StarMapData.StarBases)
        {
            if (Math.Abs(Vector3.Distance(curPosition, otherBase.gameObject.transform.localPosition)) <
                closestDistance)
            {
                closest = otherBase;
                closestDistance =
                    Math.Abs(Vector3.Distance(curPosition, otherBase.gameObject.transform.localPosition));
            }

            else if (Math.Abs(Vector3.Distance(curPosition, otherBase.gameObject.transform.localPosition)) <
                     secondClosestDistance)
            {
                secondClosest = otherBase;
                secondClosestDistance =
                    Math.Abs(Vector3.Distance(curPosition, otherBase.gameObject.transform.localPosition));
            }
        }

        // if there's only one connection, only add the closest connections that don't already exist
        if (!StarMapData.StarConnections[star].Contains(closest)) StarMapData.StarConnections[star].Add(closest);
        if (!StarMapData.StarConnections[star].Contains(secondClosest))
            StarMapData.StarConnections[star].Add(secondClosest);
    }

    private void EnsureHomeBasesAreConnected()
    {
        StarBase homeBase1 = StarMapData.StarBases[0];
        StarBase homeBase2 = StarMapData.StarBases[^1];

        var nearestToBase1 = new[] { StarMapData.StarBases[1], StarMapData.StarBases[2] };
        var nearestToBase2 = new[] { StarMapData.StarBases[^2], StarMapData.StarBases[^3] };

        float avgX = 0f;
        foreach (var starBase in nearestToBase1)
        {
            AddConnectionIfNeeded(homeBase1, starBase);
            avgX += starBase.transform.localPosition.x;
        }

        avgX *= 0.5f;
        homeBase1.transform.localPosition = new Vector3(avgX, 0, homeBase1.transform.localPosition.z);

        avgX = 0f;
        foreach (var starBase in nearestToBase2)
        {
            AddConnectionIfNeeded(homeBase2, starBase);
            avgX += starBase.transform.localPosition.x;
        }

        avgX *= 0.5f;
        homeBase2.transform.localPosition = new Vector3(avgX, 0, homeBase2.transform.localPosition.z);
    }

    private void AddConnectionIfNeeded(StarBase star1, StarBase star2)
    {
        if (StarMapData.StarConnections.ContainsKey(star1) && StarMapData.StarConnections[star1].Contains(star2))
            return;

        if (!StarMapData.StarConnections.ContainsKey(star1))
            StarMapData.StarConnections[star1] = new List<StarBase>();

        StarMapData.StarConnections[star1].Add(star2);
    }

    #endregion

    #endregion

    #region Line Drawing

    public void DrawLines()
    {
        foreach (var starbase in StarMapData.StarBases)
        {
            DrawConnectingLines(starbase);
        }

        _linesInitialized = true;
    }

    private void DrawConnectingLines(StarBase startBase)
    {
        if (StarMapData.StarLines.TryGetValue(startBase, out var starLine))
        {
            foreach (var l in starLine)
            {
                var tuple = l;
                Destroy(tuple.Item2);
                tuple.Item2 = null;
            }
            starLine.Clear();
        }

        if (!StarMapData.StarConnections.ContainsKey(startBase))
        {
            Debug.LogError($"Starbase {startBase.BaseId} not found in StarConnections", startBase);
            return;
        }

        foreach (var star in StarMapData.StarConnections[startBase])
        {
            bool hasLine = false;

            if (StarMapData.StarLines.TryGetValue(star, out var starLine2))
            {
                foreach (var connection in starLine2)
                {
                    if (connection.Item1 == startBase)
                    {
                        if (connection.Item2 != null)
                        {
                            hasLine = true;
                            // still register the line to the base for pathfinding purposes
                            if (!StarMapData.StarLines.ContainsKey(startBase))
                            {
                                StarMapData.StarLines[startBase] = new List<(StarBase, LinePath)>();
                            }

                            StarMapData.StarLines[startBase].Add((star, connection.Item2));

                            break;
                        }
                    }
                }
            }

            if (hasLine) continue;

            var line = DrawLine(startBase, star);

            if (!StarMapData.StarLines.ContainsKey(startBase))
            {
                StarMapData.StarLines[startBase] = new List<(StarBase, LinePath)>();
            }

            StarMapData.StarLines[startBase].Add((star, line));
        }
    }

    private LinePath DrawLine(StarBase startBase, StarBase endBase)
    {
        LinePath line = Instantiate(lineRendererPrefab, Vector3.zero, Quaternion.identity, starfieldParent);
        line.DrawLine(startBase, endBase);
        return line;
    }

    #endregion

    public void SetPremadeMapIndex(int index)
    {
        if (index == -1)
        {
            predefinedStarmap = null;
        }
        else
        {
            predefinedStarmap = predefinedStarmapList[index-1];
        }
    }

    public void SetRandomMapSize(MapSizes newSize)
    {
        mapSize = newSize;
    }
    
    public async void ReloadMap(MapSizes newSize)
    {
        ClearStarmap();
        SetRandomMapSize(newSize);
        
        StarMapData.SetStarBases(new List<StarBase>());

        await PrepareStarmapAsync(true);
        RPC_ReloadMap(newSize);
    }
    public async void ReloadMap(int premadeMapIndex)
    {
        ClearStarmap();
        SetPremadeMapIndex(premadeMapIndex);
        
        StarMapData.SetStarBases(new List<StarBase>());

        await PrepareStarmapAsync(true);
        RPC_ReloadMap(premadeMapIndex);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_ReloadMap(MapSizes newMapSize)
    {
        predefinedStarmap = null;
        ClearStarmap();
        mapSize = newMapSize;
        StarMapData.SetStarBases(new List<StarBase>());
        PrepareStarmap();
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_ReloadMap(int premadeMapIndex)
    {
        ClearStarmap();
        predefinedStarmap = predefinedStarmapList[premadeMapIndex];
        StarMapData.SetStarBases(new List<StarBase>());
        PrepareStarmap();
    }


    public enum MapSizes
    {
        Small,
        Medium,
        Large,
        Massive
    }

}