using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomListItem : MonoBehaviour
{
   [SerializeField] private TMP_Text roomNameText;
   [SerializeField] private TMP_Text mapSizeText;
   [SerializeField] private TMP_Text playerCountText;
   private Action<bool> hideLobby;
   private string roomName;

   public void Populate(string roomName, string mapSize, int curPlayerCount, int maxPlayerCount, Action<bool> showLobby)
   {
      this.roomName = roomName;
      roomNameText.text = $"Room of {roomName}";
      mapSizeText.text = mapSize;
      playerCountText.text = $"{curPlayerCount}/{maxPlayerCount}";
      hideLobby = showLobby;
   }

   public void OnRoomSelected()
   {
      NetworkManager.Instance.JoinRoom(roomName);
      hideLobby.Invoke(false);
      UiEvents.ShowRoomPanel.Invoke(true);
      GlobalAudioManager.Instance.PlayButtonPressClip();
   }
}
