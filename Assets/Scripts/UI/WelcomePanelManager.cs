using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WelcomePanelManager : UiPanel
{
    [Header("Registration")]
    [SerializeField] private TMP_InputField registerEmail;
    [SerializeField] private TMP_InputField registerPassword;

    [Header("Login")]
    [SerializeField] private TMP_InputField loginEmail;
    [SerializeField] private TMP_InputField loginPassword;
    [SerializeField] private Toggle rememberToggle;
    
    [Header("Region")]
    [SerializeField] private TMP_Dropdown regionDropdown;
    
    private void Start()
    {
        UiEvents.ShowWelcomePanel.AddListener(ShowPanel);
        ShowPanel(true);
        InitRegionDropdown();
    }

    public void OnRegisterButton()
    {
        // TODO register player

        registerEmail.text = "";
        registerPassword.text = "";
        ShowPanel(false);
        UiEvents.ShowLobbyPanel.Invoke(true);
        PlayClickSound();
    }


    public void OnLoginButton()
    {
        // TODO login player
        if (rememberToggle.isOn)
        {
            PlayerPrefs.SetString("logged-user", registerEmail.text);
            // TODO create a means of password/token storage
        }

        //loginEmail.text = "";
        loginPassword.text = "";
        ShowPanel(false);
        UiEvents.ShowLobbyPanel.Invoke(true);
        PlayClickSound();
    }
    
    private void InitRegionDropdown()
    {
        regionDropdown.ClearOptions();
        foreach (string region in NetworkManager.RegionList)
        {
            regionDropdown.options.Add(new TMP_Dropdown.OptionData(region));
        }

        regionDropdown.value = 0; // TODO set this to the last selected region
    }
    
    public void OnRegionChanged(int value)
    {
        regionDropdown.value = value;
        NetworkManager.Region = regionDropdown.options[value].text;
        PlayClickSound();
    }

    public void OnForgotPassword()
    {
        // TODO create password recovery system
        PlayClickSound();
    }

    public void SetPlayerName(string name)
    {
        NetworkManager.LocalPlayerName = name;
    }
}