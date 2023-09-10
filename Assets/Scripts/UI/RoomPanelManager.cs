using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomPanelManager : UiPanel
{
    [SerializeField] private Button[] mapButtons;
    [SerializeField] private TMP_Dropdown premadeMapDropdown;
    [SerializeField] private Button startGameButton;

    [Header("Player List")]
    [SerializeField] private Transform playerLayout;
    [SerializeField] private PlayerListItem listItemPrefab;

    private ElementPool<PlayerListItem> listItemPool;
    private List<PlayerListItem> activePlayerItems = new();
    private RoomDataManager _dataManager;
    private bool isHost;

    public void Start()
    {
        ShowPanel(false);
        listItemPool = new ElementPool<PlayerListItem>(listItemPrefab, playerLayout);
        UiEvents.ShowRoomPanel.AddListener(ShowPanel);
        UiEvents.StartGame.AddListener(OnStartGameReceived);
    }

    public async void Populate(RoomDataManager roomDataManager, bool isRoomHost = false)
    {
        UiEvents.PlayerJoinedRoom.AddListener(OnPlayerJoined);
        UiEvents.PlayerLeftRoom.AddListener(OnPlayerLeft);
        _dataManager = roomDataManager;

        // make sure the data manager is fully initialized before continuing
        if (!_dataManager.IsSpawned) await UniTask.WaitUntil(() => _dataManager.IsSpawned);
        isHost = isRoomHost;
        foreach (PlayerListItem activePlayer in activePlayerItems)
        {
            ReturnPlayerItem(activePlayer);
        }
        activePlayerItems.Clear();
        
        foreach (KeyValuePair<int, int> activePlayer in _dataManager.GetActivePlayers())
        {
            AddPlayerItem(activePlayer.Key, activePlayer.Value);
        }

        AddPlayerItem(_dataManager.Runner.LocalPlayer.PlayerId, GetNextAvailableColor());
        _dataManager.PlayerJoined(_dataManager.Runner.LocalPlayer.PlayerId, GetNextAvailableColor());

        PopulateMapSizeButtons();
        PopulatePredefinedMapDropdown();
        UpdateStartGameStatus();
        ShowPanel(true);
    }

    private int GetNextAvailableColor()
    {
        int availableColor = 0;

        while (_dataManager.GetActivePlayers().ContainsValue(availableColor))
        {
            availableColor++;
        }

        return availableColor;
    }

    private void PopulateMapSizeButtons()
    {
        // set the listeners for the map size buttons here to make it cleaner
        for (int i = 0; i < mapButtons.Length; i++)
        {
            if (isHost)
            {
                StarmapManager.MapSizes size = (StarmapManager.MapSizes)i;
                mapButtons[i].onClick.AddListener(() =>
                {
                    OnMapSizeSelect(size);
                    premadeMapDropdown.value = 0;
                    PlayClickSound();
                });
                mapButtons[i].interactable = true;
            }
            else
            {
                mapButtons[i].interactable = false;
            }
        }
    }

    private void PopulatePredefinedMapDropdown()
    {
        if (!isHost)
        {
            premadeMapDropdown.interactable = false;
            return;
        }

        premadeMapDropdown.options.Clear();
        premadeMapDropdown.options.Add(new TMP_Dropdown.OptionData { text = "None" });
        foreach (PredefinedStarmap map in StarmapManager.Instance.predefinedStarmapList)
        {
            premadeMapDropdown.options.Add(new TMP_Dropdown.OptionData { text = map.name });
        }

        premadeMapDropdown.onValueChanged.AddListener(OnPremadeMapSelect);
        premadeMapDropdown.interactable = true;
    }

    private void UpdateStartGameStatus()
    {
        bool isReady = true;
        foreach (PlayerListItem player in activePlayerItems)
        {
            if (!player.readyToggle.isOn) isReady = false;
        }

        startGameButton.interactable = isHost && isReady;
    }

    private async void OnPlayerJoined(int newPlayer)
    {
        if (!_dataManager.IsSpawned) await UniTask.WaitUntil(() => _dataManager.IsSpawned);
        int color = GetNextAvailableColor();
        _dataManager.PlayerJoined(newPlayer, color);
        AddPlayerItem(newPlayer, color);
    }

    private void AddPlayerItem(int newPlayerId, int selectedColor)
    {
        // make sure there's only one listing for each player
        if (activePlayerItems.Exists(x => x.PlayerIndex == newPlayerId)) return;
        if (!_dataManager.PlayerNames.TryGet(newPlayerId, out string playerName))
        {
            string s = "";
            foreach (KeyValuePair<PlayerRef, string> player in _dataManager.PlayerNames)
            {
                s += $"{player.Key}: {player.Value}\n";
            }
            Debug.LogError($"Player {newPlayerId} not found in player names dictionary\n{s}");
            playerName = "Player";
        }

        PlayerListItem item = listItemPool.GetElement();
        item.Populate(playerName, newPlayerId, selectedColor, _dataManager, UpdateStartGameStatus);
        activePlayerItems.Add(item);
    }

    public void ChangeName(int playerId, string name)
    {
        var playerItem = activePlayerItems.Find(x => x.PlayerIndex == playerId);
        if (playerItem != null) playerItem.ChangeName(name);
    }

    private void OnPlayerLeft(int leavingPlayer)
    {
        PlayerListItem playerItem = activePlayerItems.Find(x => x.PlayerIndex == leavingPlayer);
        ReturnPlayerItem(playerItem);
        activePlayerItems.Remove(playerItem);

        _dataManager.PlayerLeft(leavingPlayer);
    }

    private void ReturnPlayerItem(PlayerListItem playerItem)
    {
        playerItem.Depopulate();
        listItemPool.ReturnElement(playerItem);
    }

    private void OnMapSizeSelect(StarmapManager.MapSizes size)
    {
        StarmapManager.Instance.SetRandomMapSize(size);
        StarmapManager.Instance.SetPremadeMapIndex(-1);
        
        PlayClickSound();
        //StarmapManager.Instance.ReloadMap(size);
    }

    private void OnPremadeMapSelect(int premadeMapIndex)
    {
        if (premadeMapIndex == 0) return;
        StarmapManager.Instance.SetPremadeMapIndex(premadeMapIndex);
        PlayClickSound();
    }

    // only called by host
    public void OnStartGame()
    {
        NetworkManager.Instance.StartGame();
        PlayClickSound();
    }

    // received by all players once host starts game
    public void OnStartGameReceived()
    {
        ShowPanel(false);
        UiEvents.ShowUi.Invoke(false);
        PlayClickSound();
    }

    public async void OnReturnToLobby()
    {
        ShowPanel(false);
        await NetworkManager.Instance.ExitRoom();
        UiEvents.ShowLobbyPanel.Invoke(true);
        PlayClickSound();
    }
}