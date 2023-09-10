using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

public class LobbyPanelManager : UiPanel
{
    [SerializeField] private Transform roomListLayout;
    [SerializeField] private RoomListItem roomListItemPrefab;
    private ElementPool<RoomListItem> listItemPool;
    private List<RoomListItem> activeItems = new List<RoomListItem>();

    private void Start()
    {
        UiEvents.ShowLobbyPanel.AddListener(ShowPanel);
        listItemPool = new ElementPool<RoomListItem>(roomListItemPrefab, roomListLayout);
        ShowPanel(false);
    }

    protected override async void ShowPanel(bool show)
    {
        if (show)
        {
            await NetworkManager.Instance.EnterLobby(OnLobbyEntered);
        }

        base.ShowPanel(show);
    }

    private void OnLobbyEntered(List<SessionInfo> sessionInfos)
    {
        foreach (RoomListItem item in activeItems)
        {
            listItemPool.ReturnElement(item);
        }
        activeItems.Clear();
        
        foreach (SessionInfo session in sessionInfos)
        {
            RoomListItem listItem = listItemPool.GetElement();
            listItem.Populate(session.Name, "Unknown Map", session.PlayerCount, session.MaxPlayers, ShowPanel);
            activeItems.Add(listItem);
        }
    }

    public void OnPlayTutorial()
    {
        // TODO add a tutorial
        PlayClickSound();
    }

    public async void OnLogOut()
    {
        ShowPanel(false);
        await NetworkManager.Instance.ExitRoom();
        UiEvents.ShowWelcomePanel.Invoke(true);
        PlayClickSound();
    }

    public void OnCreateRoom()
    {
        ShowPanel(false);
        NetworkManager.Instance.CreateRoom();
        // Don't show the Room panel until it's been initialized 
        //UiEvents.ShowRoomPanel.Invoke(true);
        PlayClickSound();
    }

    public async void OnRefreshLobby()
    {
        await NetworkManager.Instance.EnterLobby(OnLobbyEntered);
        PlayClickSound();
    }
}