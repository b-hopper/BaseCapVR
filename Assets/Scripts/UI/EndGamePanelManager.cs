using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EndGamePanelManager : UiPanel
{
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text timeText;

    private void Start()
    {
        ShowPanel(false);
        UiEvents.ShowEndGamePanel.AddListener(ShowPanel);
    }

    protected void ShowPanel(bool show, bool playerWon)
    {
        base.ShowPanel(show);
        Debug.Log($"EndGamePanelManager.ShowPanel({show}, {playerWon})");
        resultText.text = playerWon ? "Victory" : "Defeat";
        UiEvents.ShowRoomPanel.Invoke(false);
    }
    
    public async void OnReturnToLobby()
    {
        ShowPanel(false);
        await NetworkManager.Instance.ExitRoom();
        UiEvents.ShowLobbyPanel.Invoke(true);
        PlayClickSound();
    }
}
