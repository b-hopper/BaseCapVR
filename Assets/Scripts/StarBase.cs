using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Drones;
using Fusion;
using Tilia.Interactions.Interactables.Interactors;
using TMPro;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class StarBase : DroneQueue
{
    public string StarName { get; private set; }
    public int DroneCount;
    public int defense;
     [Networked] public int BaseId { get; private set; }
    [SerializeField, Range(-1, 3), ReadOnly] private int baseLevel = 0;

    [Header("Rendering")] public Renderer baseRenderer;
    public Material material;
    public Outline outline;
    public MeshFilter[] meshFilters;
    public Mesh[] baseMeshes;
    public Mesh[] neutralMeshes;
    [Header("World Text")] public GameObject uiContainer;
    //public Image droneCountBorder;
    public TMP_Text droneSelectedText;
    public TMP_Text droneCountText;
    public TMP_Text baseIdText;
    public Image pausedIndicator;
    public Image overPopulationIndicator;
    public GameObject upgradeIndicator;
    public Image timerProgressBar;
    private bool initialized = false;
    private bool idSet;
    private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
    public StarbaseAudioManager audioManager;
    
    [Networked, Capacity(3)] NetworkArray<float> _position => default;

    [Networked, Capacity(16)]
    public NetworkArray<int> ConnectedBaseIds { get; } = MakeInitializer(new int[16]);
            //{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, });
            // For some reason this MakeInitializer is causing strange behavior
            // So, we'll just set it in SyncWithNetwork() instead
            
    public Vector3 SpawnPosition => new Vector3(_position[0], _position[1], _position[2]);

    #region Built-in

    private void Start()
    {
        if (droneCountText != null)
        {
            droneCountText.fontMaterial = Instantiate(droneCountText.fontMaterial);
            droneCountText.fontMaterial.SetColor(OutlineColor, Color.black);
            droneCountText.fontMaterial.SetKeyword(new LocalKeyword(droneCountText.fontMaterial.shader, "OUTLINE_ON"),
                true);
            droneCountText.outlineWidth = 0.15f;
        }
        
        if (audioManager == null)
        {
            audioManager = GetComponentInChildren<StarbaseAudioManager>();
        }
        
        if (baseRenderer != null)
        {
            baseRenderer.material =
                Instantiate(material); // Make a copy of the material so we don't change the original
            initialized = true;
        }
        StartCoroutine(SyncWithNetwork());
    }

    void Awake()
    {
        StarName = "Unnamed Star";
    }

    private void Update()
    {
        HandleInput();
    }

    #endregion

    #region setup

    public StarBaseData Populate(int id, MapNode nodeData)
    {
        StarBaseData baseData;
        if (StarBaseManager.Instance.BaseDataInitialized)
        {
            baseData = StarBaseManager.Instance.GetStarBaseData(BaseId);
            TeamIndex = baseData.team;
        }
        else
        {
            baseData = nodeData.ToStarBaseData();
            BaseId = id;
            baseData.id = BaseId;
            TeamIndex = baseData.team;
            DroneCount = nodeData.startingDroneCount;
        }

        PopulateOffData(baseData);
        return baseData;
    }

    public StarBaseData Populate(int id, int teamIdx, int startingDrones = 0)
    {
        StarBaseData baseData;
        if (StarBaseManager.Instance.BaseDataInitialized)
        {
            baseData = StarBaseManager.Instance.GetStarBaseData(BaseId);
            TeamIndex = baseData.team;
        }
        else
        {
            BaseId = id;
            baseData = new StarBaseData
            {
                id = id,
                team = teamIdx,
                droneCount = startingDrones,
                upgradeLevel = teamIdx == -1 ? 0 : 1,
            };
            
            DroneCount = startingDrones;
            TeamIndex = teamIdx;
        }

        PopulateOffData(baseData);
        return baseData;
    }

    private void PopulateOffData(StarBaseData baseData)
    {
        baseLevel = baseData.upgradeLevel;
        if (TeamIndex != -1)
        {
            TeamAnalyticsManager.Instance.AddStarBaseToTeam(TeamIndex);
            TeamAnalyticsManager.Instance.AddDronesToTeam(baseData.team, baseData.droneCount);
        }

        if (baseData.team == -1) ChangeBaseShape(-1);
        else ChangeBaseShape(baseLevel);
        ChangeColor(TeamAnalyticsManager.Instance.teamSettings.GetTeamData(TeamIndex));
        TeamAnalyticsManager.Instance.teamSettings.onColorChanged.AddListener((team, teamData) =>
        {
            if(team == baseData.team) ChangeColor(teamData);
        });
        SetupBaseUI(baseData);
    }

    public void PopulateConnectionData(List<StarBase> connections)
    {
        connectedBases = connections;
        
        // Check to see if Ids have been set already  
        if (ConnectedBaseIds[0] != -1) return; 
        
        // If they haven't, set them (this will run on server only)
        int i = 0;
        foreach (var connectedBase in connectedBases)
        {
            ConnectedBaseIds.Set(i++, connectedBase.BaseId);
        }
    }
    
    private void SetupBaseUI(StarBaseData baseData)
    {
        droneCountText.text = baseData.droneCount.ToString();
        baseIdText.text = baseData.id.ToString();
    }

    private bool _wasAtMaxDrones = false, _wasAboveMaxDrones = false;
    
    private void UpdateUI()
    {
        StarBaseData data = StarBaseManager.Instance.GetStarBaseData(BaseId);
        droneCountText.text = data.droneCount.ToString();
        baseIdText.text = data.id.ToString();

        if (data.team == -1) return;
        int maxDrones = GameSettingsManager.Instance.upgradeLevels[data.upgradeLevel].MaxDrones;
        if (data.droneCount == maxDrones)
        {
            pausedIndicator.gameObject.SetActive(true);
            overPopulationIndicator.gameObject.SetActive(false);
            if (!_wasAtMaxDrones) // Playing the sound every tick is annoying
            {
                _wasAtMaxDrones = true;
                audioManager.PlayUnitLimitReachedClip(true);
            }
        }
        else if (data.droneCount > maxDrones)
        {
            overPopulationIndicator.gameObject.SetActive(true);
            pausedIndicator.gameObject.SetActive(false);
            if (!_wasAboveMaxDrones)
            {
                _wasAtMaxDrones = true; // This is here so we don't play the sound when we go from overpopulated to max drones
                _wasAboveMaxDrones = true;
                audioManager.PlayUnitLimitReachedClip(false);
            }
        }
        else
        {
            _wasAtMaxDrones = false;
            _wasAboveMaxDrones = false;
            overPopulationIndicator.gameObject.SetActive(false);
            pausedIndicator.gameObject.SetActive(false);
        }

        if (data.upgradeTime != 0)
        {
            if (!_isUpgrading)
            {
                _isUpgrading = true;
                StartTimer(data.upgradeTime);
            }
        }
        else if (_isUpgrading)
        {
            _isUpgrading = false;
        }
    }

    #endregion

    #region networkUpdates

    private IEnumerator SyncWithNetwork()
    {
        while (StarBaseManager.Instance == null)
        {
            yield return null;
        }

        if (Object.HasStateAuthority)
        { // Since these bases never move, we don't need a NetworkTransform. We can just send the position once
          // This also allows players to pan around the map without the bases moving for everyone else
            var pos = transform.localPosition;
            _position.Set(0, pos.x);
            _position.Set(1, pos.y);
            _position.Set(2, pos.z);

            for (var i = 0; i < ConnectedBaseIds.Length; i++)
            {
                ConnectedBaseIds.Set(i, -1);
            }
        }
        else
        {
            while (transform.parent == null)
            {
                yield return null;
            }
            transform.localPosition = new Vector3(_position[0], _position[1], _position[2]);
        }

        transform.localScale = Vector3.one;
        
        //Debug.Log($"[StarBase {BaseId}] listening for updates");
        //StarBaseManager.Instance.tickStarBases.AddListener(UpdateBase);
        // ** Replaced with StarBaseManager.OnStarBaseDataArrayChanged(Changed<StarBase>) ** 
    }

    public void UpdateBase()
    {
        StarBaseData curBaseData = StarBaseManager.Instance.GetStarBaseData(BaseId);
        if (TeamIndex != curBaseData.team)
        {
            // only update the base change when the data reflects it
            TeamAnalyticsManager.Instance.ChangeStarBaseTeam(TeamIndex, curBaseData.team);
            TeamIndex = curBaseData.team;
            if (TeamIndex == -1) ChangeBaseShape(-1);
            else ChangeBaseShape(curBaseData.upgradeLevel);
            ChangeColor(TeamAnalyticsManager.Instance.teamSettings.GetTeamData(TeamIndex));
        }

        if (DroneCount != curBaseData.droneCount)
        {
            SetDroneCount(curBaseData);
        }

        if (baseLevel != curBaseData.upgradeLevel)
        {
            baseLevel = curBaseData.upgradeLevel;
            ChangeBaseShape((TeamIndex == -1) ? -1 : baseLevel);
            audioManager.PlayBaseUpgradedClip(TeamIndex == PlayerTeamAssignmentManager.Instance.GetLocalPlayerTeam());
        }
        
        UpdateUI();
    }

    private void SetDroneCount(StarBaseData curBaseData)
    {
        DroneCount = curBaseData.droneCount;
        droneCountText.text = DroneCount.ToString();
    }

    #endregion

    #region Teams

    private void CheckTeamChange()
    {
        if (_capturingTeamIndex == -1) return;
        
        if (StarBaseManager.Instance.GetStarBaseData(BaseId).droneCount <= 0 &&
            _isCapturing &&
            TeamIndex != _capturingTeamIndex)
        {
            var oldTeam = TeamIndex;
            
            StarBaseManager.Instance.ChangeTeam(BaseId, TeamIndex, _capturingTeamIndex);
            RPC_TeamChange(_capturingTeamIndex, oldTeam);
            AddDrones(_capturingDroneCount);
            RemoveAllOrbitingDrones();
        }
    }

    private void ChangeTeamNeutral()
    {
        Debug.Log($"[StarBase {BaseId}] changing to neutral");
        
        var oldTeam = TeamIndex;

        StarBaseManager.Instance.ChangeTeam(BaseId, TeamIndex, -1);
        RPC_TeamChange(-1, oldTeam);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void RPC_TeamChange(int newTeam, int oldTeam)
    {
        TeamChangeInternal(newTeam, oldTeam);
    }

    private void TeamChangeInternal(int newTeam, int oldTeam)
    {
        ChangeColor(TeamAnalyticsManager.Instance.teamSettings.GetTeamData(newTeam));
        
        var localPlayerTeam = PlayerTeamAssignmentManager.Instance.GetLocalPlayerTeam();
        
        audioManager.PlayBaseCapturedClip(
            isFriendly: localPlayerTeam == newTeam,
            capturedWasFriendly: localPlayerTeam == oldTeam);
    }

    public void SetStarName(string name)
    {
        StarName = name;
    }

    #endregion

    #region Input

    private float _lastInputTime;
    public static float _timeBetweenInputs = 0.25f; // TODO add to settings manager
    private void HandleInput()
    {
        if (Time.time - _lastInputTime < _timeBetweenInputs) return;
        
        if (!_hasInput) return;

        if (_inputTracker.isHoldingTrigger)
        {
            StarBaseManager.Instance.ControllerCommand(this, int.MaxValue);
        }
        else if (_inputTracker.isHoldingUp)
        {
            StarBaseManager.Instance.SendDrones(this, 10);
        }
        else if (_inputTracker.isHoldingLeft)
        {
            StarBaseManager.Instance.AddDronesFromSelectedBase(this, 1);
        }
        else if (_inputTracker.isHoldingRight)
        {
            StarBaseManager.Instance.SendDrones(this, 1);
        }
        else if (_inputTracker.isHoldingDown)
        {
            StarBaseManager.Instance.AddDronesFromSelectedBase(this, 10);
        }
        
        _lastInputTime = Time.time;
    }
    
    
    public void TriggerPress()
    {
    }

    private bool _hasInput => _inputTracker != null && 
                                  (_inputTracker.isHoldingTrigger || 
                                   _inputTracker.isHoldingUp || 
                                   _inputTracker.isHoldingLeft || 
                                   _inputTracker.isHoldingRight || 
                                   _inputTracker.isHoldingDown) &&
                              StarBaseManager.Instance.IsBaseSelected(this);

    public void FirstTouched(InteractorFacade interactor)
    {
        StarBaseManager.Instance.HoverStarBase(BaseId);
    }
    
    [HideInInspector]
    public InteractorFacade _interactor;
    InputTracker _inputTracker;
    public void Touched(InteractorFacade interactor)
    {
        _interactor = interactor;
        _inputTracker = interactor.GetComponent<InputTracker>();

        if (PlayerTeamAssignmentManager.Instance.GetLocalPlayerTeam() != TeamIndex)
        {
            HapticsManager.Instance.PlayHaptics(interactor, 0);
        }
        else
        {
            HapticsManager.Instance.PlayHaptics(interactor, 1);
        }
    }

    public void Untouched(InteractorFacade interactor)
    {
    }
    
    public void LastUntouched(InteractorFacade interactor)
    {
        StarBaseManager.Instance.StopHoverStarBase(BaseId);
        _inputTracker = null;
        _interactor = null;
        
        //HapticsManager.Instance.ClearHaptics(interactor);
    }

    public void TryUpgradeBase()
    {
        StarBaseManager.Instance.TryStartBaseUpgrade(BaseId);
        HandUiController.Instance.ResetUpgradeToolPosition();
    }

    #endregion

    #region Visuals

    public void OnDronesSelected(int amount)
    {
        if (amount == 0)
        {
            droneSelectedText.gameObject.SetActive(false);
            DisableOutline();
            return;
        }
        
        droneSelectedText.gameObject.SetActive(true);
        droneSelectedText.text = $"{amount}/";
        
        EnableOutline();
    }
    
    public void EnableOutline()
    {
        outline.OutlineColor = TeamAnalyticsManager.Instance.teamSettings.GetTeamData(TeamIndex).highlightColor;
        outline.enabled = true;
    }

    public void EnableOutlineWithColor(Color c)
    {
        outline.OutlineColor = c;
        outline.enabled = true;
    }

    public void DisableOutline()
    {
        outline.enabled = false;
    }

    // need to override due to listeners not playing nice with the default values
    internal void ChangeColor(TeamData teamData)
    {
        StartCoroutine(ChangeColorCoroutine(teamData.teamColor, teamData.teamColorSecondary, 0.0f));
    }

    private void ChangeColor(TeamData teamData, float duration)
    {
        StartCoroutine(ChangeColorCoroutine(teamData.teamColor, teamData.teamColorSecondary, duration));
    }

    private IEnumerator ChangeColorCoroutine(Color color, Color secondaryColor, float duration)
    {
#if UNITY_EDITOR
        if (EditorApplication.isPlaying == false)
        {
            baseRenderer.sharedMaterial.color = color;
            droneCountText.fontMaterial.SetColor(OutlineColor, color);
            droneCountText.color = secondaryColor;
            //droneCountBorder.color = secondaryColor;
            
            yield break;
        }
#endif

        
        while (!initialized) yield return new WaitForFixedUpdate();

        Color startColor = baseRenderer.material.color;
        float time = 0f;
        while (time < duration)
        {
            var newColor = Color.Lerp(startColor, color, time / duration);
            var newSecondaryColor = Color.Lerp(startColor, secondaryColor, time / duration);
            baseRenderer.material.color = newColor;
            droneCountText.fontMaterial.SetColor(OutlineColor, newColor);
            //droneCountText.color = newSecondaryColor;
            //droneCountBorder.color = newSecondaryColor;

            time += Time.deltaTime;
            yield return null;
        }

        baseRenderer.material.color = color;
        droneCountText.fontMaterial.SetColor(OutlineColor, color);
        //droneCountText.color = secondaryColor;
        //droneCountBorder.color = secondaryColor;

        timerProgressBar.color = secondaryColor;
    }

    private void ChangeBaseShape(int level)
    {
        var idx = Random.Range(0, neutralMeshes.Length);
        
        if (level >= 0 && level < baseMeshes.Length && baseMeshes[level] == null)
        {
            // This means there is no mesh for this upgrade level, so do not change the mesh
            // This can happen if the player captured an asteroid, but has not yet upgraded it to a base.
            // This is intended behavior.
            return;
        }
        
        
        
        foreach (var meshFilter in meshFilters)
        {
            meshFilter.mesh = level < 0 ? 
                neutralMeshes[idx] : 
                baseMeshes[level];
        }
        
        bool isAsteroid = level < 0;
        
        Quaternion rotation = isAsteroid ? 
            Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)) : 
            Quaternion.identity;
        
        transform.localRotation = rotation;
        uiContainer.transform.rotation = Quaternion.identity;
    }

    #endregion


    #region attacks

    private void TakeAttack(int damage)
    {
    }

    #endregion


    #region DroneControl
    public void SendDrones(StarBase destination, int dronesToSend)
    {
        if (destination == null || destination == this) return;
        RPC_SendDrones(destination, dronesToSend);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SendDrones(StarBase destination, int dronesToSend)
    {
        AuthSendDrones(destination, dronesToSend);
    }

    private void AuthSendDrones(StarBase destination, int dronesToSend)
    {
        if (!NetworkManager.Instance.HasStateAuthority) return;
        
        // Just send all drones if we don't have enough
        // This causes a problem if we have 0 drones,
        // which can happen if a base has a waypoint and is sending drones to it
        // So: if we have 0 drones, send 1 drone
        // Checking for a waypoint is not feasible, because that is client side
        // TODO: This might be exploitable, look into it
        if (StarBaseManager.Instance.GetStarBaseData(BaseId).droneCount < dronesToSend)
        {
            dronesToSend = StarBaseManager.Instance.GetStarBaseData(BaseId).droneCount;
            dronesToSend = dronesToSend == 0 ? 1 : dronesToSend;
        } 
        
        List<int> path = DronePathfinder.GetPath(BaseId, destination.BaseId);
        string pathString = "";
        foreach (int node in path)
        {
            pathString += $"{node} ";
        }
        
        for (int i = 0; i < dronesToSend; i++)
        {
            CreateDrone(path.ToArray());
        }
        
        RPC_SendDroneLocal(dronesToSend);
    }

    public override void DroneArrived(Drone drone)
    {
        if (!NetworkManager.Instance.HasStateAuthority) return;
        
        base.DroneArrived(drone);
        
        if (drone.teamIndex == TeamIndex)
        {
            DoFriendlyArrive(drone);
        }
        else
        {
            DoCombat(drone);
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AddOrbitingDrone(Drone drone)
    {
        AddOrbitingDroneInternal(drone);
    }

    private float orbitDistance = 1.5f;
    private void AddOrbitingDroneInternal(Drone drone)
    {
        _capturingDroneCount++;
        Vector3 randomOffset = (Random.insideUnitSphere * 0.5f).normalized;
        _attackingDrones.Add((drone, randomOffset));
        
        // Put the drone in a new parent so it can rotate around the base
        var newParent = new GameObject("DroneParent").transform;
        newParent.SetParent(transform);
        newParent.localPosition = Vector3.zero;
        newParent.localScale = Vector3.one;
        drone.Object.transform.SetParent(newParent);
        var normPos = drone.Object.transform.localPosition.normalized;
        drone.Object.transform.localPosition = normPos * orbitDistance;
    }

    private void FixedUpdate()
    {
        if (_attackingDrones.Count > 0)
        {
            RotateOrbitingDrones();
        }
    }

    private bool _timerRunning = false;

    private void StartTimer(float time)
    {
        RunTimerAsync(time).Forget();
    }

    private void StopTimer()
    {
        _timerRunning = false;
        timerProgressBar.fillAmount = 0f;
    }
    
    private async UniTask RunTimerAsync(float time)
    {
        if (_timerRunning) return;
        
        timerProgressBar.fillAmount = 0f;
        _timerRunning = true;

        var timer = 0f;
        
        while (time > timer && _timerRunning)
        {
            await UniTask.WaitForFixedUpdate();
            timer += Time.fixedDeltaTime;
            timerProgressBar.fillAmount = timer / time;
        }
        
        StopTimer();
    }

    private void RotateOrbitingDrones()
    {
        if (_attackingDrones.Count > 0)
        {
            foreach (var drone in _attackingDrones)
            {
                var newRot = drone.Item2;
                
             //   var newRot = Quaternion.AngleAxis(1f, Vector3.up) * drone.Item2;
                drone.Item1.Object.transform.parent.localEulerAngles += newRot;
            }
        }
    }
    
    private void RemoveAllOrbitingDrones(bool destroyed = false)
    {
        RPC_RemoveOrbitingDrone(_capturingDroneCount, destroyed);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RemoveOrbitingDrone(int amount, bool destroyed = false)
    {
        RemoveOrbitingDroneInternal(amount, destroyed);
    }
    
    private void RemoveOrbitingDroneInternal(int amount, bool destroyed = false)
    {
        _capturingDroneCount -= amount;
        if (_capturingDroneCount <= 0)
        {
            StopCaptureTimer();
        }

        if (NetworkManager.Instance.HasStateAuthority)
        {
            for (int i = 0; i < amount; i++)
            {
                var drone = _attackingDrones[0];
                _attackingDrones.RemoveAt(0);
                if (drone.Item1 == null)
                {
                    continue;
                }

                var toDestroy = drone.Item1.Object.transform.parent.gameObject;

                    if (destroyed)
                    {
                        drone.Item1.KillDrone();
                    }
                    else
                    {
                        Runner.Despawn(drone.Item1.Object);
                    }

                    Destroy(toDestroy); // Destroy the temporary parent once the drone is no longer orbiting
            }
        }

        else 
        {
            _attackingDrones.RemoveRange(0, amount);
            if (_attackingDrones.Count == 0)
            { // Server handles destroying the drones, so we just need to destroy the parents locally
                List<GameObject> toDestroy = new List<GameObject>();
                for (int i = 0; i < transform.childCount; i++)
                {
                    // Find all the drone parents 
                    if (transform.GetChild(i).name == "DroneParent")
                    {
                        toDestroy.Add(transform.GetChild(i).gameObject);
                    }
                }
                
                foreach (var p in toDestroy)
                {
                    Destroy(p);
                }
            }
        }
    }

    private void DoFriendlyArrive(Drone drone)
    {
        if (_isCapturing) // enemy is capturing, decrement 
        {
            Debug.Log($"Friendly drone arrived at {BaseId} while enemy is capturing");
            RPC_RemoveOrbitingDrone(1, true);
        }
        else // friendly drone arrived
        {
            Debug.Log($"Friendly drone arrived at {BaseId}");
            Runner.Despawn(drone.Object);
            AddDrone();
        }
        
    }
    
    private void DoCombat(Drone drone)
    {
        if (drone.teamIndex == TeamIndex) return;
        
        if (StarBaseManager.Instance.GetStarBaseData(BaseId).droneCount <= 0 && TeamIndex != drone.teamIndex)
        {
            if (!_isCapturing)
            {
                StarBaseManager.Instance.TryStartBaseCapture(BaseId);
                StartCaptureTimer();
                _capturingTeamIndex = drone.teamIndex;
            }
            RPC_AddOrbitingDrone(drone);
        }
        else
        {
            TeamAnalyticsManager.Instance.RemoveDronesFromTeam(drone.teamIndex, 1);
            drone.KillDrone();
            RemoveDrones(drone.damage - defense);
            
            if (StarBaseManager.Instance.GetStarBaseData(BaseId).droneCount <= 0)
            {
                ChangeTeamNeutral();
            }
        }
        //RemoveDrones(drone.damage - defense);
    }

    private bool _isCapturing = false;
    private bool _isUpgrading = false;
    private bool _droneProdPaused => _isCapturing || TeamIndex == -1;
    private int _capturingTeamIndex = -1;
    public static int timeTillCaptured => RuntimeGameSettings.captureTime;
    private int _capturingDroneCount = 0;
    private List<(Drone, /*orbit direction normalized*/ Vector3)> _attackingDrones = new ();
    private void StartCaptureTimer()
    { // TODO: Rework this to use the universal timer above
        if (_isCapturing)
        {
            return;
        }
        _isCapturing = true;
        
        DoCaptureTimerAsync().Forget();
    }
    
    private void StopCaptureTimer()
    {
        if (!_isCapturing)
        {
            return;
        }
        _isCapturing = false;
        _capturingDroneCount = 0;
        _capturingTeamIndex = -1;
    }

    private async UniTask DoCaptureTimerAsync()
    {
        while (_isCapturing)
        {
            var timeLeft = StarBaseManager.Instance.GetStarBaseData(BaseId).captureTime;
            var ratio = (float)timeLeft / GameSettingsManager.Instance.timeToCapture;
            timerProgressBar.fillAmount = ratio;
            
            if (timeLeft <= 0)
            {
                CheckTeamChange();
                StopCaptureTimer();
            }
            
            await UniTask.Delay(100);
        }
        
        timerProgressBar.fillAmount = 0;
    }

    public StarBaseData ProduceDroneTick(StarBaseData baseData)
    {
        if (_isCapturing || _droneProdPaused) return baseData; // If we are being captured, don't produce drones
        
        if (_waypointBase != null)
        {   // If we have a waypoint base, send drones to it
            SendDrones((StarBase)_waypointBase, 1);
        }
        else
        {   // Otherwise, produce drones
            GameSettingsManager.NetworkedUpgradeLevels curLevelSettings = GameSettingsManager.Instance.upgradeLevels[baseData.upgradeLevel];
            
            if (baseData.droneCount < curLevelSettings.MaxDrones)
            { // If we have less than the max drones, produce one
                baseData.ProduceDrone(1);
                TeamAnalyticsManager.Instance.AddDronesToTeam(baseData.team, 1);

            }
            else if (baseData.droneCount > curLevelSettings.MaxDrones)
            { // If we have more than the max drones, remove one
                baseData.RemoveDrone(1);
                TeamAnalyticsManager.Instance.RemoveDronesFromTeam(baseData.team, 1);
                
            }
            else // (baseData.droneCount == curLevelSettings.MaxDrones)
            {
                // Do nothing
            }
        }
        
        return baseData;
    }
    
    public override void AddDrones(int amount)
    {
        base.AddDrones(amount);
        StarBaseManager.Instance.AddDroneToBase(BaseId, amount);
    }

    public override void AddDrone()
    {
        base.AddDrone();
        StarBaseManager.Instance.AddDroneToBase(BaseId, 1);
    }

    public override void RemoveDrones(int amount)
    {
        base.RemoveDrones(amount);
        StarBaseManager.Instance.RemoveDroneFromBase(BaseId, amount);
        
    }

    public void EnableUpgradeIndicator(bool enable)
    {
        upgradeIndicator.SetActive(enable);
    }
    
#if UNITY_EDITOR
    [ContextMenu("Add a lot of drones")]
    public void TEST_AddDrones()
    {
        StarBaseManager.Instance.AddDroneToBase(BaseId, 400);
    }

    private StarBase _debugEnemyBase;
    [ContextMenu("Send all drones to first enemy base found")]
    public void TEST_SendDrones()
    {
        if (_debugEnemyBase == null)
        {
            _debugEnemyBase = StarBaseManager.Instance.DEBUG_GetFirstEnemyStarBaseData(TeamIndex);
        }

        AuthSendDrones(_debugEnemyBase, 200);
    }

    private int _lastLevel;
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
        if (_lastLevel != baseLevel)
        {
            if (baseLevel == 0 && _lastLevel != -1) ChangeBaseShape(-1); // Little bit wonky but this is just for editor visuals so NBD
            else ChangeBaseShape(baseLevel);
            _lastLevel = baseLevel;
        }
    }
#endif

    #endregion
}