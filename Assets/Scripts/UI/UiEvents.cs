using UnityEngine;
using UnityEngine.Events;

public static class UiEvents
{
    // Global UI Canvas events
    public static UnityEvent<bool> ShowUi = new();
    public static UnityEvent StartGame = new();
    public static UnityEvent MoveUiToPlayer = new();
    
    // UI Panel events
    public static UnityEvent<bool> ShowWelcomePanel = new();
    public static UnityEvent<bool> ShowLobbyPanel = new();
    public static UnityEvent<bool> ShowRoomPanel = new();
    public static UnityEvent<bool> ShowGameSettingsPanel = new();
    public static UnityEvent<bool, bool> ShowEndGamePanel = new();
    public static UnityEvent<int> PlayerJoinedRoom = new();
    public static UnityEvent<int> PlayerLeftRoom = new();
}