using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text username;
    [SerializeField] private Image playerIcon;
    [SerializeField] private TMP_Dropdown colorDropdown;
    public Toggle readyToggle;
    private RoomDataManager _dataManager;
    private Action checkAllReadyCallback;
    public int PlayerIndex { get; private set; }

    private int curColor = -1;

    public void Populate(string playerName, int playerId, int selectedColor, RoomDataManager dataManager,
        Action allReadyCallback)
    {
        username.text = playerName;
        PlayerIndex = playerId;
        _dataManager = dataManager;
        colorDropdown.value = selectedColor;
        curColor = selectedColor;

        checkAllReadyCallback = allReadyCallback;

        if (dataManager.Runner.LocalPlayer.PlayerId != playerId)
        {
            colorDropdown.interactable = false;
            readyToggle.interactable = false;
        }

        SetupInteractableListeners();
        LoadColorOptions();
    }

    public void ChangeName(string n)
    {
        username.text = n;
    }

    public void Depopulate()
    {
        RemoveInteractableListeners();
    }

    private void SetupInteractableListeners()
    {
        readyToggle.onValueChanged.AddListener(OnPlayerReady);
        colorDropdown.onValueChanged.AddListener(OnColorChange);
        _dataManager.onPlayerReady.AddListener(OnPlayerReadyReceived);
        _dataManager.onColorChanged.AddListener(OnColorChangeReceived);
    }

    private void RemoveInteractableListeners()
    {
        readyToggle.onValueChanged.RemoveListener(OnPlayerReady);
        colorDropdown.onValueChanged.RemoveListener(OnColorChange);
        _dataManager.onPlayerReady.RemoveListener(OnPlayerReadyReceived);
        _dataManager.onColorChanged.RemoveListener(OnColorChangeReceived);
    }

    private void LoadColorOptions()
    {
        colorDropdown.options.Clear();
        foreach (SelectableColor color in TeamAnalyticsManager.Instance.teamSettings.colorOptions.selectableColors)
        {
            TMP_Dropdown.OptionData optionData = new TMP_Dropdown.OptionData
            {
                text = color.name,
                image = color.colorSwatch
            };

            colorDropdown.options.Add(optionData);
        }
    }

    private void OnPlayerReady(bool isReady)
    {
        if (_dataManager != null && _dataManager.Runner.LocalPlayer.PlayerId == PlayerIndex)
            _dataManager.RPC_PlayerIsReady(PlayerIndex, isReady);
    }

    private void OnPlayerReadyReceived(PlayerRef player, bool isReady)
    {
        if (player.PlayerId == PlayerIndex)
        {
            readyToggle.isOn = isReady;
            checkAllReadyCallback.Invoke();
        }
    }

    // tell the server that the player has changed their color
    private void OnColorChange(int colorIndex)
    {
        // if a different player has the color, don't change colors
        if (_dataManager.GetActivePlayers()[PlayerIndex] == colorIndex ||
            _dataManager.GetActivePlayers().ContainsValue(colorIndex))
        {
            colorDropdown.value = _dataManager.GetActivePlayers()[PlayerIndex];
            return;
        }

        _dataManager.RPC_ChangePlayerColor(PlayerIndex, colorIndex);
    }

    // callback from the server to ensure everyone shows the same color
    private void OnColorChangeReceived()
    {
      
        // if the color is already set, don't change it. This prevents an infinite loop
        if (colorDropdown.value == _dataManager.GetActivePlayers()[PlayerIndex])
        {
            return;
        }
        
        colorDropdown.value = _dataManager.GetActivePlayers()[PlayerIndex];
    }

    public int GetSelectedColor()
    {
        return colorDropdown.value;
    }
}