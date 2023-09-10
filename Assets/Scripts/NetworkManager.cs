#pragma warning disable CS4014

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using Fusion.XR.Shared.Rig;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NetworkManager>();
            }

            if (_instance == null)
            {
                _instance = Instantiate(NetworkPrefabsStorage.Instance.networkManagerPrefab);
            }

            return _instance;
        }
    }

    private static NetworkManager _instance;

    [Header("Room configuration")]
    public GameMode mode = GameMode.Server;
    public string lobbyName = "GameLobby";
    public string roomName
    {
        // TODO will eventually be replaced by the host username
        get
        {
            string glyphs= "abcdefghijklmnopqrstuvwxyz0123456789";
            string name = "";
            for (int i = 0; i < 10; i++)
            {
                name += glyphs[Random.Range(0, glyphs.Length)];
            }

            return name;
            
        }
    }
    public bool connectOnStart = false;

    [Header("Fusion settings")]
    [Tooltip("Fusion runner. Automatically created if not set")]
    public NetworkRunner _runner;
    public INetworkSceneManager sceneManager;

    [Header("User Prefab for pre-fusion controls")]
    public GameObject menuUserPrefabController;

    public bool HasStateAuthority =>
#if UNITY_SERVER
        _runner.IsServer;
#else
        !_runner.IsClient;
#endif

    [Header("Event")]
    public UnityEvent onWillConnect = new UnityEvent();

    private bool _initialized = false;
    private Action<List<SessionInfo>> onSessionListUpdated;

    public static string[] RegionList = new string[]
    {
        "us",
        "usw",
        "eu",
        "asia",
        "jp",
        "cn",
        "sa",
        "kr"
    };

    public static string Region = "us";

    public static string LocalPlayerName = "Player";
    
    private void Awake()
    {
        VerifyRunnerExists();
    }

    private void Start()
    {
#if !UNITY_SERVER
        if (connectOnStart)
#endif
            Connect();
    }

    private void OnDestroy()
    {
        _instance = null;
    }

    private void VerifyRunnerExists()
    {
        // Check if a runner exist on the same game object
        if (_runner == null) _runner = GetComponent<NetworkRunner>();

        // Create the Fusion runner and let it know that we will be providing user input
        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
    }

    public async Task EnterLobby(Action<List<SessionInfo>> onLobbyEntered)
    {
        onSessionListUpdated = onLobbyEntered;
        Connect();
        VerifyRunnerExists();
        if (!_runner.SessionInfo.IsValid)
        {
            var appSettings = PhotonAppSettings.Instance.AppSettings.GetCopy();
            appSettings.FixedRegion = Region;
            appSettings.AppVersion = Application.version;
            
            StartGameResult result = await _runner.JoinSessionLobby(
                SessionLobby.Custom, 
                lobbyName, 
                customAppSettings: appSettings
            );

            // touch of delay to let Fusion start properly, since the above doesn't do it
            await UniTask.Delay(250);

            if (!result.Ok)
            {
                // TODO do error things
                Debug.LogError($"Failed to join lobby {lobbyName}: {result}");
            }
        }
    }

    private void Connect()
    {
        // Create the scene manager if it does not exist
        if (sceneManager == null) sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        if (onWillConnect != null) onWillConnect.Invoke();
    }

    public void CreateRoom()
    {
        mode = GameMode.Host;
        EnterRoom();
    }

    public void JoinRoom(string room)
    {
        mode = GameMode.Client;
        EnterRoom(room);
    }

    private async void EnterRoom(string room = null)
    {
        string roomToEnter = (string.IsNullOrEmpty(room)) ? roomName : room;
        // Start or join (depends on gamemode) a session with a specific name
        var args = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomToEnter,
            Scene = SceneManager.GetActiveScene().buildIndex,
            SceneManager = sceneManager
        };
        await _runner.StartGame(args);
        _runner.Spawn(NetworkPrefabsStorage.Instance.roomDataManagerPrefab);
    }

    public async UniTask ExitRoom()
    {
        DespawnManagers();
        
        await _runner.Shutdown(false);
        Destroy(_runner);
        _runner = null;
        _spawnedCharacters.Clear();
        _initialized = false;
        
        await UniTask.Delay(250);
    }

    public void StartGame()
    {
        if (_runner != null && _runner.IsRunning)

        {
#if UNITY_SERVER
            if (!_runner.IsServer)
#else
            if (_runner.IsClient)
#endif
            {
                return;
            }

            GameStateManager.Instance.StartGame();
        }
    }

    #region INetworkRunnerCallbacks

    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        
#if UNITY_SERVER
        if (runner.IsServer)
#else
        if (!runner.IsClient)
#endif
        {
            EnsureManagersReady(runner, () =>
            {
                if (!player.IsValid) return; // in case of dedicated 
                int idx = _spawnedCharacters.Count;
                var pos = StarmapManager.GetPlayerPosition(idx);
                // Look at the center of the map, but only on the Y axis
                var lookPos = new Vector3(pos.x, 0, pos.z);
                var rot = Quaternion.LookRotation(-lookPos);
                
                //rot = Quaternion.Euler(0, rot.eulerAngles.y, 0);

                // Spawn the user prefab for the local user
                NetworkObject networkPlayerObject = runner.Spawn(
                    prefab: NetworkPrefabsStorage.Instance.userPrefab,
                    position: pos,
                    rotation: rot,
                    inputAuthority: player,
                    onBeforeSpawned: (runner, obj) => { });

                // move the with the local player
                //if (networkPlayerObject.InputAuthority == runner.LocalPlayer)
                //    UiEvents.MoveUiToPlayer.Invoke();

                _spawnedCharacters.Add(player, networkPlayerObject);

                if (PlayerTeamAssignmentManager.Instance.AddPlayerToNextAvailableTeam(player))
                    Debug.Log($"Spawned player: {player} (idx {idx}) at pos: {pos}", networkPlayerObject);
                else
                {
                    runner.Disconnect(player);
                }

                // if the original user prefab for menus still exists, destroy it so there's only one local player
                if (menuUserPrefabController != null && menuUserPrefabController.activeSelf)
                {
                    Destroy(menuUserPrefabController);
                }
                
                UiEvents.PlayerJoinedRoom.Invoke(player.PlayerId);

            });
        }
    } 
    private void EnsureManagersReady(NetworkRunner runner, Action onReady = null)
    {
        if (_initialized)
        {
            StartCoroutine(EnsureManagersReadyCoroutine(onReady));
            return;
        }

        _initialized = true;
        if (PlayerTeamAssignmentManager.Instance == null) runner.Spawn(NetworkPrefabsStorage.Instance.playerTeamAssignmentPrefab);
        if (GameStateManager.Instance == null) runner.Spawn(NetworkPrefabsStorage.Instance.gameStateManagerPrefab);
        if (StarBaseManager.Instance == null) runner.Spawn(NetworkPrefabsStorage.Instance.starBaseManagerPrefab);
        if (StarmapManager.Instance == null) runner.Spawn(NetworkPrefabsStorage.Instance.starMapManagerPrefab);
        if (TeamAnalyticsManager.Instance == null) runner.Spawn(NetworkPrefabsStorage.Instance.teamManagerPrefab);
        if (GameSettingsManager.Instance == null) runner.Spawn(NetworkPrefabsStorage.Instance.gameSettingsManagerPrefab);

        StartCoroutine(EnsureManagersReadyCoroutine(onReady));
    }
    
    private void DespawnManagers()
    {
        if (!HasStateAuthority) return;
        
        if (PlayerTeamAssignmentManager.Instance != null) _runner.Despawn(PlayerTeamAssignmentManager.Instance.Object);
        if (GameStateManager.Instance != null) _runner.Despawn(GameStateManager.Instance.Object);
        if (StarBaseManager.Instance != null) _runner.Despawn(StarBaseManager.Instance.Object);
        if (StarmapManager.Instance != null) _runner.Despawn(StarmapManager.Instance.Object);
        if (TeamAnalyticsManager.Instance != null) _runner.Despawn(TeamAnalyticsManager.Instance.Object);
        if (GameSettingsManager.Instance != null) _runner.Despawn(GameSettingsManager.Instance.Object);
        
        var ps = GetComponent<NetworkPhysicsSimulation3D>();
        if (ps != null) Destroy(ps);
        var pm = GetComponent<HitboxManager>();
        if (pm != null) Destroy(pm);
    }

    private IEnumerator EnsureManagersReadyCoroutine(Action onReady = null)
    {
        while (GameStateManager.Instance == null
               || StarBaseManager.Instance == null
               || StarmapManager.Instance == null // || !StarmapManager.Instance.initialized // Map no longer generates before game starts
               || TeamAnalyticsManager.Instance == null)
        {
            yield return null;
        }

        onReady?.Invoke();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Find and remove the players avatar
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }

        UiEvents.PlayerLeftRoom.Invoke(player.PlayerId);
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // TODO pass currrent room map size
        onSessionListUpdated?.Invoke(sessionList);
    }


    #region Unused callbacks

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    #endregion

    #endregion
}
#pragma warning restore CS4014