using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

public class StarBaseManager : NetworkBehaviour
{
    // Partial singleton, we want only one instance, but that instance to be controlled by Fusion
    public static StarBaseManager Instance => _instance;
    private static StarBaseManager _instance;
    
    // event to tick the local nodes on a change
    [HideInInspector] public UnityEvent tickStarBases = new();

    // TODO consider reworking this once input is better known
    [HideInInspector] public UnityEvent<StarBase> StarBaseHovered = new();
    [HideInInspector] public UnityEvent<StarBase> StarBaseStopHovered = new();
    public (bool useResult, int combatDelta) combatResult = (false, 0);
    private List<StarBase> selectedBases = new();

    [Networked(OnChanged = nameof(OnStarBaseDataArrayChanged)), Capacity(64)]
    private NetworkArray<StarBaseData> StarBaseDataArray { get; } = MakeInitializer(new StarBaseData[64]);

    [Networked] public NetworkBool BaseDataInitialized { get; private set; }
    [Networked] private NetworkBool DataReset { get; set; }

    [Networked] private ref SecondTicker _secondTicker => ref MakeRef(SecondTicker.CreateSecondTicker);

    private bool _gameStarted = false;

    public void HoverStarBase(int id)
    {
        if (!_gameStarted) return;
        if (id < 0 || id >= StarBaseDataArray.Length) StarBaseHovered.Invoke(null);
        else
            StarBaseHovered.Invoke(StarMapData.StarBases[id]);
    }

    public void StopHoverStarBase(int id)
    {
        if (!_gameStarted) return;
        if (id < 0 || id >= StarBaseDataArray.Length) return;
        StarBaseStopHovered.Invoke(StarMapData.StarBases[id]);
    }

    #region Fusion Callbacks

    public override async void Spawned()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Runner.Despawn(Object);
            return;
        }
        
        await UniTask.WaitWhile(() => GameStateManager.Instance == null);
        
        GameStateManager.GameOverEvent.AddListener(OnGameOver);
        GameStateManager.OnGameStart.Add(OnGameStart);
    }

    private void OnGameOver()
    {
        _gameStarted = false;
        StarBaseHovered.RemoveAllListeners();
        StarBaseStopHovered.RemoveAllListeners();
    }

    private void OnGameStart()
    {
        _secondTicker.Initialize(Runner);
        _gameStarted = true;
        StarBaseHovered.AddListener(OnHoverStarBase);
        StarBaseStopHovered.AddListener(OnStopHoverStarBase);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_instance == this)
        {
            _gameStarted = false;
            GameStateManager.GameOverEvent.RemoveListener(OnGameOver);
            GameStateManager.OnGameStart.Remove(OnGameStart);
            StarBaseHovered.RemoveAllListeners();
            StarBaseStopHovered.RemoveAllListeners();
            _instance = null;
        }
        
        base.Despawned(runner, hasState);
    }

    public override void FixedUpdateNetwork()
    {
        if (!_gameStarted) return;

        if (!HasStateAuthority) return;
        
        _secondTicker.Tick(Runner);
        if (_secondTicker.secondHasTicked)
        {
            TickStarBases();
        }
    }

    #endregion

    #region StarBaseUpdating

    public static void OnStarBaseDataArrayChanged(Changed<NetworkBehaviour> changed)
    {
        if (StarMapData.StarBases == null) return;
        
        foreach (var starBase in StarMapData.StarBases)
        {
            if (!starBase.isActiveAndEnabled) continue; // If the server regenerates the map, some of the NetworkObjects
                                                        // will be disabled while spawning/despawning, so we need to skip them
            starBase.UpdateBase();
        }
    }
    
    private void TickStarBases()
    {
//        Debug.Log("[StarBaseManager] Ticking Bases");

        for (int i = 0; i < StarBaseDataArray.Length; i++)
        {
            StarBaseData baseData = StarBaseDataArray[i];
            if (i > 0 && baseData.id == 0) break; // reached the end of the existing nodes
            baseData = CheckBaseUpdateProgress(baseData);
            baseData = TickBaseDroneProduction(baseData);
            StarBaseDataArray.Set(i, baseData);
        }

        // tick the bases each tick so they stay accurate to the node numbers
        tickStarBases.Invoke();
    }

    // these have to return the updated data due to being a struct
    private StarBaseData CheckBaseUpdateProgress(StarBaseData baseData)
    {
        if (baseData.upgradeTime != 0)
        {
            baseData.TickUpgradeTime();
            if (baseData.upgradeTime == 0)
            { 
                baseData.upgradeLevel++;
            }

            if (baseData.upgradeTime < 0)
            {
                baseData.upgradeTime = 0;
            }
        }

        if (baseData.captureTime != 0)
        {
            baseData.TickCaptureTime();
            if (baseData.captureTime < 0)
            {
                baseData.captureTime = 0;
            }
        }

        return baseData;
    }

    private StarBaseData TickBaseDroneProduction(StarBaseData baseData)
    {
        if (baseData.upgradeTime != 0) return baseData;

        GameSettingsManager.NetworkedUpgradeLevels curLevelSettings = GameSettingsManager.Instance.upgradeLevels[baseData.upgradeLevel];
        if (baseData.droneCount == curLevelSettings.MaxDrones) return baseData;
        if (baseData.team == -1 || curLevelSettings.DroneBuildTime < 0) return baseData;
        if (_secondTicker.secondsElapsed % curLevelSettings.DroneBuildTime == 0)
        {               
            baseData = StarMapData.StarBases[baseData.id].ProduceDroneTick(baseData);
        }

        return baseData;
    }

    public bool CanUpgradeBase(int baseId)
    {
        StarBaseData baseData = StarBaseDataArray[baseId];

        return (baseData.droneCount >= GameSettingsManager.Instance.upgradeLevels[baseData.upgradeLevel].UpgradeCost &&
                baseData.upgradeLevel < GameSettingsManager.Instance.upgradeLevels.Count() - 1 && 
                baseData.upgradeTime <= 0);
    }

    public void TryStartBaseUpgrade(int baseId)
    {
        if (CanUpgradeBase(baseId))
        {
            RPC_StartBaseUpgrade(baseId);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_StartBaseUpgrade(int baseId)
    {
        if (!NetworkManager.Instance.HasStateAuthority) return;
        if (!CanUpgradeBase(baseId)) return;

        StarBaseData baseData = StarBaseDataArray[baseId];

        var cost = GameSettingsManager.Instance.upgradeLevels[baseData.upgradeLevel].UpgradeCost;
        
        baseData.RemoveDrone(cost);
        TeamAnalyticsManager.Instance.RemoveDronesFromTeam(baseData.team, cost);
        
        baseData.upgradeTime = GameSettingsManager.Instance.upgradeLevels[baseData.upgradeLevel].UpgradeTime;
        
        StarBaseDataArray.Set(baseId, baseData); // StarBaseDataArray's OnChanged callback will update the base on the clients
    }

    public bool CanCaptureBase(int baseId)
    {
        StarBaseData baseData = StarBaseDataArray[baseId];

        return (baseData.droneCount <= 0);
    }
    
    public void TryStartBaseCapture(int baseId)
    {
        if (!CanCaptureBase(baseId)) return;
        
        RPC_StartBaseCapture(baseId);
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_StartBaseCapture(int baseId)
    {
        if (!NetworkManager.Instance.HasStateAuthority) return;
        if (!CanCaptureBase(baseId)) return;

        StarBaseData baseData = StarBaseDataArray[baseId];

        baseData.captureTime = GameSettingsManager.Instance.timeToCapture;
        
        StarBaseDataArray.Set(baseId, baseData); // StarBaseDataArray's OnChanged callback will update the base on the clients
    }
    
    #endregion

    #region Drone Movement Selection

    Dictionary<int /*base id*/, int /*# drones selected*/> _dronesSelected = new Dictionary<int, int>();
    Dictionary<StarBase, List<LinePath>> _highlightedLinePaths = new Dictionary<StarBase, List<LinePath>>();

    public void OnHoverStarBase(StarBase starBase)
    {
        if (starBase == null)
        {
            return;
        }

        if (selectedBases.Count <= 1)
        {
            selectedBases.Add(starBase);
            starBase.EnableOutline();
        }
        
        bool isEnemyBase = starBase.TeamIndex != PlayerTeamAssignmentManager.Instance.GetPlayerTeam(Runner.LocalPlayer);

        if (_dronesSelected.Count > 0 && isEnemyBase)
        {
            ProcessCombatResults(starBase);
        }
        else
        {
            combatResult = (false, 0);
        }
        
        starBase.audioManager.PlayHoverClip(isEnemyBase);
    }
    
    public void OnStopHoverStarBase(StarBase starBase)
    {
        if (!selectedBases.Contains(starBase)) return;

        combatResult = (false, 0);

        if (!_dronesSelected.ContainsKey(starBase.BaseId))
        {
            // No drones from this base selected, turn base highlight off
            starBase.DisableOutline();
        }
        
        selectedBases.Remove(starBase);
        
        if (_highlightedLinePaths.ContainsKey(starBase))
        {
            // Remove the highlighted paths
            foreach (LinePath linePath in _highlightedLinePaths[starBase])
            {
                linePath.UnhighlightLine();
            }
            _highlightedLinePaths.Remove(starBase);
        }
    }
    
    public bool IsBaseSelected(StarBase starBase)
    {
        return selectedBases.Contains(starBase);
    }

    private void ProcessCombatResults(StarBase starBase)
    {
        int droneCount = 0;
        int defenseCount = 0;
        int startTeam;
        
        if (!_highlightedLinePaths.ContainsKey(starBase))
        {
            _highlightedLinePaths.Add(starBase, new List<LinePath>());
        }
        
        foreach (KeyValuePair<int, int> selectedDronesPerBase in _dronesSelected)
        {
            List<int> path = DronePathfinder.GetPath(selectedDronesPerBase.Key, starBase.BaseId);
            droneCount += selectedDronesPerBase.Value;
            startTeam = StarMapData.StarBases[selectedDronesPerBase.Key].TeamIndex;
            for (int i = 0; i < path.Count - 1; i++)
            {
                StarBase startBase = StarMapData.StarBases[path[i]];
                if (starBase.TeamIndex != startTeam) defenseCount += starBase.DroneCount;
                LinePath linePath = StarMapData.StarLines[startBase]
                    .Find(x => x.Item2.CheckIds(path[i], path[i + 1])).Item2;
                if (linePath == null)
                {
                    Debug.LogError($"No path found between StarBases {path[i]} and {path[i + 1]}");
                    continue;
                }
                
                _highlightedLinePaths[starBase].Add(linePath);
                linePath.HighlightLine();
            }
        }

        combatResult = (true, droneCount - defenseCount);
    }

    public void ControllerCommand(StarBase starBase, int numDrones)
    {
        if (starBase.TeamIndex == PlayerTeamAssignmentManager.Instance.GetPlayerTeam(Runner.LocalPlayer))
        {
            AddDronesFromSelectedBase(starBase, numDrones);
        }
        else
        {
            SendDrones(starBase, numDrones);
        }
    }

    public void AddDronesFromSelectedBase(StarBase starBase, int numDrones)
    {
        if (selectedBases == null) return;
        if (!IsLocalTeamBase(starBase)) return;

        var origNumDrones = _dronesSelected.TryGetValue(starBase.BaseId, out var value) ? value : 0;

        numDrones += origNumDrones;

        if (numDrones > starBase.DroneCount) numDrones = starBase.DroneCount;
        if (numDrones <= 0) return;

        _dronesSelected.TryAdd(starBase.BaseId, 0);
        _dronesSelected[starBase.BaseId] = numDrones;
        starBase.OnDronesSelected(numDrones);
        starBase.audioManager.PlaySelectUnitsClip(numDrones - origNumDrones);
        HapticsManager.Instance.PlayHaptics(starBase._interactor, 2);
    }

    private bool IsLocalTeamBase(StarBase starBase)
    {
        return starBase.TeamIndex == PlayerTeamAssignmentManager.Instance.GetPlayerTeam(Runner.LocalPlayer);
    }

    public int GetSelectedDroneCount()
    {
        return _dronesSelected.Sum(kvp => kvp.Value);
    }

    public void SendDrones(StarBase starBase, int numDrones)
    {
        if (_dronesSelected.Count == 0) return;
        
        Dictionary<int, int> tmpDronesSelected = new Dictionary<int, int>();

        foreach (var kvp in _dronesSelected.ToList())
        {
            if (numDrones <= 0) break;
            
            int dronesToSend = kvp.Value;
            if (dronesToSend > numDrones) dronesToSend = numDrones;
            
            tmpDronesSelected.TryAdd(kvp.Key, dronesToSend);
            
            _dronesSelected[kvp.Key] -= dronesToSend;
            numDrones -= dronesToSend;
            
            if (_dronesSelected[kvp.Key] <= 0)
            {
                _dronesSelected.Remove(kvp.Key);
                StarMapData.StarBases[kvp.Key].OnDronesSelected(0);
            }
            
            if (kvp.Key == starBase.BaseId)
            {   // Effectively "deselecting" drones
                starBase.audioManager.PlaySelectUnitsClip(-dronesToSend);
            }
        }

        foreach (var kvp in tmpDronesSelected)
        {
            var sb = StarMapData.StarBases[kvp.Key];
            sb.SendDrones(starBase, kvp.Value);
        }
        
        if (starBase.TeamIndex == PlayerTeamAssignmentManager.Instance.GetPlayerTeam(Runner.LocalPlayer))
        {
            starBase.audioManager.PlayMoveClip(false);
        }
        else
        {
            starBase.audioManager.PlayMoveClip(true);
        }

        if (starBase._interactor != null)
        {
            HapticsManager.Instance.PlayHaptics(starBase._interactor, 2);
        }
    }
    #endregion

    #region dataGettersAndSetters

    public void PopulateStarBaseData(List<StarBaseData> starBaseList)
    {
        if (BaseDataInitialized) return;
        int expectedId = 0;
        starBaseList = starBaseList.OrderBy(x => x.id).ToList();
        foreach (StarBaseData baseData in starBaseList)
        {
            StarBaseData curStarBaseData = StarBaseDataArray.Get(baseData.id);
            if (curStarBaseData.id != 0 && !DataReset)
            {
                Debug.LogError($"[SetStarBaseData] Multiple star bases generated with id {baseData.id}!");
                continue;
            }

            if (baseData.id != expectedId)
            {
                Debug.LogError(
                    $"[SetStarBaseData] Star base does not have expected id. Expected Id: {expectedId} actual Id: {baseData.id}!");
                expectedId++;
                continue;
            }

            expectedId++;
            StarBaseDataArray.Set(baseData.id, baseData);
        }

        BaseDataInitialized = true;
    }

    public void ResetStarBaseData()
    {
        BaseDataInitialized = false;
        DataReset = true;
    }
    
#if UNITY_EDITOR
    public StarBase DEBUG_GetFirstEnemyStarBaseData(int teamIndex)
    {
        foreach (StarBaseData starBaseData in StarBaseDataArray)
        {
            if (starBaseData.team != teamIndex && starBaseData.team != -1) 
                return StarMapData.StarBases[starBaseData.id];
        }

        return null;
    }

    [ContextMenu("DEBUG_AllTeamsSendDronesToFirstEnemy")]
    private void DEBUG_AllTeamsSendDronesToFirstEnemy()
    {
        int teamIdx = 0;

        var bases = StarMapData.StarBases;
        var firstBase = bases.First(x => x.TeamIndex == teamIdx);
        while (firstBase != null)
        {
            firstBase.TEST_AddDrones();
            firstBase.TEST_SendDrones();

            teamIdx++;
            firstBase = bases.FirstOrDefault(x => x.TeamIndex == teamIdx);
        }
    }
#endif

    public StarBaseData GetStarBaseData(int id)
    {
        if (StarBaseDataArray[id].id == id) return StarBaseDataArray.Get(id);

        Debug.LogError(
            $"No Star Base Found or Incorrect ID at index for Star Base. Index provided: {id}. Id of node on network {StarBaseDataArray[id].id}");
        return new StarBaseData();
    }

    public void ChangeTeam(int baseId, int currentTeam, int newTeam)
    {
        if (!NetworkManager.Instance.HasStateAuthority) return;

        StarBaseData data = StarBaseDataArray[baseId];
        data.team = newTeam;
        StarBaseDataArray.Set(baseId, data);
    }

    public void AddDroneToBase(int baseId, int productionAmount)
    {
        if (!NetworkManager.Instance.HasStateAuthority) return;
        
        StarBaseData data = StarBaseDataArray[baseId];
        data.ProduceDrone(productionAmount);
        StarBaseDataArray.Set(baseId, data);
    }

    public void RemoveDroneFromBase(int baseId, int productionAmount)
    {
        if (!NetworkManager.Instance.HasStateAuthority) return;

        StarBaseData data = StarBaseDataArray[baseId];
        data.RemoveDrone(productionAmount);
        StarBaseDataArray.Set(baseId, data);
    }

    #endregion
    
}