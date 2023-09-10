using Fusion;
using UnityEngine;

public class PlayerTeamAssignmentManager : NetworkBehaviour
{
    public static PlayerTeamAssignmentManager Instance => _instance;
    private static PlayerTeamAssignmentManager _instance;

    [Networked, Capacity(12)] 
    private NetworkDictionary<int /*playerRef.id*/, int /*team index*/> _playerRefs { get; }

    public override void Spawned()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Runner.Despawn(Object);
        }
    }

    #region Player

    public bool AddPlayerToNextAvailableTeam(PlayerRef player)
    {
        for (int i = 0; i < TeamAnalyticsManager.Instance.teamSettings.teams.Count; i++)
        {
            if (!_playerRefs.ContainsValue(i))
            {
                SetPlayerTeam(player, i);
                return true;
            }
        }

        Debug.LogError($"Player {player} unable to join - no available team found");
        return false;
    }

    public int GetPlayerTeam(PlayerRef player)
    {
        return GetPlayerTeam(player.PlayerId);
    }

    public int GetPlayerTeam(int playerId)
    {
        if (_playerRefs.TryGet(playerId, out var teamIdx))
        {
            return teamIdx;
        }
        return -1;
    }

    public int GetLocalPlayerTeam()
    {
        return GetPlayerTeam(Runner.LocalPlayer);
    }

    public void SetPlayerTeam(PlayerRef player, int teamIdx)
    {
        if (Object.HasStateAuthority)
        {
            RPC_SetPlayerTeam(player, teamIdx);
        }

        if (!GameStateManager.Instance.IsGameRunning)
            SetPlayerTeamInternal(player, teamIdx);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetPlayerTeam(PlayerRef player, int teamIdx)
    {
        SetPlayerTeamInternal(player, teamIdx);
    }

    private void SetPlayerTeamInternal(PlayerRef player, int teamIdx)
    {
        if (teamIdx == -1)
        {
            _playerRefs.Remove(player.PlayerId);
            return;
        }

        _playerRefs.Set(player.PlayerId, teamIdx);
    }

    #endregion
}