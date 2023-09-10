using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class GameStateManager : NetworkBehaviour
{
    public static GameStateManager Instance => _instance;
    private static GameStateManager _instance;

    [Networked] public NetworkBool IsGameRunning { get; private set; } = false;
    [HideInInspector] public static UnityEvent GameOverEvent = new UnityEvent();
    private bool isGameOver = false;
    private int winnerTeamIdx = -1;

    public static List<Action> OnGameStart = new List<Action>();
    public static List<Action> BeforeGameStart = new List<Action>();

    [Networked] public int Seed { get; set; }

    public override void Spawned()
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

        if (HasStateAuthority) SetSeed();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        
        if (_instance == this)
        {
            IsGameRunning = false;
            
            _instance = null;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        CheckGameState();
    }


    private void CheckGameState()
    {
        if (isGameOver || !IsGameRunning)
        {
            return;
        }

        int aliveTeams = 0;
        int winner = -1;

        var teamsCount = TeamAnalyticsManager.Instance.teamSettings.teams.Count;
        for (int i = 0; i < teamsCount; i++)
        {
            if (StarMapData.StarBases.FindAll(x => x.TeamIndex == i).Count > 0)
            {
                aliveTeams++;
                winner = i;
            }
        }
        
        if (aliveTeams == 1 && winner != -1)
        {
            isGameOver = true;
            IsGameRunning = false;
            winnerTeamIdx = winner;
            OnGameOver();
        }
    }

    [ContextMenu("Start Game")]
    public void StartGame()
    {
        if (!Object.HasStateAuthority) return;

        RPC_StartGame();
    }

    [Rpc(sources: RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartGame()
    {
        foreach (var action in BeforeGameStart)
        {
            action.Invoke();
        }
        
        IsGameRunning = true;
        Debug.Log("[RPC] Starting game!");
        foreach (var action in OnGameStart)
        {
            action.Invoke();
        }

        UiEvents.StartGame.Invoke();
    }

    [ContextMenu("Game Over")]
    private void OnGameOver()
    {
        RPC_OnGameOver(winnerTeamIdx);
        if (winnerTeamIdx == -1) return;
        Debug.Log(
            "Game Over! The winner is " + TeamAnalyticsManager.Instance.teamSettings.teams[winnerTeamIdx].teamName);
    }

    [Rpc(sources: RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnGameOver(int winTeamIndex)
    {
        if (winTeamIndex != -1)
        {
            Debug.Log("[RPC] Game Over! The winner is " +
                      TeamAnalyticsManager.Instance.teamSettings.teams[winTeamIndex].teamName);
        }
        UiEvents.ShowUi.Invoke(true);
        //UiEvents.MoveUiToPlayer.Invoke();
        UiEvents.ShowEndGamePanel.Invoke(true,
            PlayerTeamAssignmentManager.Instance.GetPlayerTeam(Runner.LocalPlayer) == winTeamIndex);
        StarmapManager.Instance.ClearStarmap();
        GameOverEvent.Invoke();
        
    }

    public void SetSeed()
    {
        var newSeed = Seed;
        if (newSeed == 0)
        {
            newSeed = Random.Range(0, int.MaxValue);
        }

        RPC_SetSeed(newSeed);
    }

    [Rpc(sources: RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetSeed(int seed)
    {
        Seed = seed;
    }
}