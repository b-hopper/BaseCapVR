using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon.StructWrapping;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

public class RoomDataManager : NetworkBehaviour
{
    [HideInInspector] public UnityEvent<PlayerRef, bool> onPlayerReady = new();
    [HideInInspector] public UnityEvent onColorChanged = new();
    public bool IsSpawned { get; private set; }
    //[Networked, Capacity(10)] NetworkArray<int> PlayerColorIndexes =>
    //    default; // NOTE: the size of this array needs to always equal the max player limit for a room
    [Networked(OnChanged = nameof(OnColorChanged)), Capacity(10)] NetworkDictionary<int, int> ActivePlayers =>
        default; // NOTE: the size of this array needs to always equal the max player limit for a room
    
    [Networked(OnChanged = nameof(OnNameChanged)), Capacity(10)] public NetworkDictionary<PlayerRef, string> PlayerNames => default;
    
    private RoomPanelManager _panelManager;

    public override void Spawned()
    {
        var dataManagers = FindObjectsOfType<RoomDataManager>();
        if (dataManagers.Length > 1)
        {
            foreach (var rdm in dataManagers)
            {
                if (rdm != this) Destroy(rdm.gameObject);
            }
        }
        _panelManager = FindObjectOfType<RoomPanelManager>();
        // sending the player as an int to make it easier to support more than two players in the future
//        Debug.Log("Local Player: " + Runner.LocalPlayer.PlayerId);
//        Debug.Log(
//            $"Is local player server host: {(Runner.LocalPlayer.PlayerId == (Runner.SessionInfo.MaxPlayers - 1))}");
        _panelManager.Populate(this, HasStateAuthority);
        if (HasStateAuthority) ActivePlayers.Add(Runner.LocalPlayer.PlayerId, 1);
        RPC_ChangePlayerName(Runner.LocalPlayer, NetworkManager.LocalPlayerName);
        IsSpawned = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        IsSpawned = false;
    }

    public void PlayerJoined(int playerId, int selectedColor)
    {
        if (ActivePlayers.ContainsKey(playerId)) return;
        if (HasStateAuthority)
        {
            ActivePlayers.Add(playerId, selectedColor);
            PlayerNames.Add(playerId, "");
        }
    }

    public void PlayerLeft(int playerId)
    {
        if (HasStateAuthority)
        {
            ActivePlayers.Remove(playerId);
            PlayerNames.Remove(playerId);
        }
    }

    public NetworkDictionary<int, int> GetActivePlayers()
    {
        return ActivePlayers;
    }
    
    private static void OnColorChanged(Changed<RoomDataManager> t)
    {
        t.Behaviour.onColorChanged.Invoke();
        
        foreach (var p in t.Behaviour.ActivePlayers)
        {
            var team = PlayerTeamAssignmentManager.Instance.GetPlayerTeam(p.Key);
            if(team != -1) TeamAnalyticsManager.Instance.teamSettings.ChangeTeamColor(team, p.Value);
        }
    }

    private static void OnNameChanged(Changed<RoomDataManager> t)
    {
        foreach (var p in t.Behaviour.PlayerNames)
        {
            t.Behaviour._panelManager.ChangeName(p.Key, p.Value);
        }
    }

    [Rpc]
    public void RPC_PlayerIsReady(PlayerRef readyPlayer, bool isReady)
    {
        onPlayerReady.Invoke(readyPlayer, isReady);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ChangePlayerColor(int playerIndex, int colorIndex)
    {
        Debug.Log($"Setting Player {playerIndex} color to {colorIndex}");
        ActivePlayers.Set(playerIndex, colorIndex);
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ChangePlayerName(PlayerRef player, string name)
    {
        PlayerNames.Set(player, name);
    }
}