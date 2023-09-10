using System;
using Fusion;
using UnityEngine;
using UnityEngine.Windows;
using File = System.IO.File;

public class GameSettingsManager : NetworkBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    [SerializeField] private GameSettings defaultGameSettings;

    // these are instance based to account for changes made locally
    [Networked(OnChanged = nameof(OnDroneSpeedUpdated))]
    public float droneSpeed { get; set; }
    [Networked] public float droneSendInterval { get; set; }
    [Networked] public float droneDestroyTime { get; set; }
    [Networked] public float droneLaunchSpeed { get; set; }
    [Networked] public int timeToCapture { get; set; }

    [Networked, Capacity(4)] public NetworkArray<NetworkedUpgradeLevels> upgradeLevels { get; } =
        MakeInitializer(new NetworkedUpgradeLevels[4]);

#region Game Settings
    public static void OnDroneSpeedUpdated(Changed<GameSettingsManager> changed)
    {
//        Debug.Log($"Drone Speed Updated: {changed.Behaviour.droneSpeed}");
    }

    public override void Spawned()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Runner.Despawn(GetComponent<NetworkObject>());
        }

        droneSpeed = (RuntimeGameSettings.droneSpeed == default)
            ? defaultGameSettings.speed
            : RuntimeGameSettings.droneSpeed;
        droneSendInterval = (RuntimeGameSettings.droneSendInterval == default)
            ? defaultGameSettings.sendInterval
            : RuntimeGameSettings.droneSendInterval;
        droneDestroyTime = (RuntimeGameSettings.droneDestroyTime == default)
            ? defaultGameSettings.destroyTime
            : RuntimeGameSettings.droneDestroyTime;
        droneLaunchSpeed = (RuntimeGameSettings.droneLaunchSpeed == default)
            ? defaultGameSettings.launchSpeed
            : RuntimeGameSettings.droneLaunchSpeed;
        timeToCapture = (RuntimeGameSettings.captureTime == default)
            ? defaultGameSettings.captureTime
            : RuntimeGameSettings.captureTime;


        for (int i = 0; i < upgradeLevels.Length; i++)
        {
            if (RuntimeGameSettings.upgradeLevels.Count < upgradeLevels.Length)
            {
                upgradeLevels.Set(i, defaultGameSettings.upgradeLevels[i].ConvertUpgradeLevelsToNetworked());
                // duplicate to avoid modification of the scriptable object
                RuntimeGameSettings.upgradeLevels.Add(
                    GameSettings.BaseUpgradeLevelSettings.Duplicate(defaultGameSettings.upgradeLevels[i]));
            }
            else
            {
                upgradeLevels.Set(i, RuntimeGameSettings.upgradeLevels[i].ConvertUpgradeLevelsToNetworked());
            }
        }


        GameSettingsPanelManager panelManager = FindObjectOfType<GameSettingsPanelManager>();
        panelManager.Populate();
        if (HasStateAuthority) UiEvents.ShowGameSettingsPanel.Invoke(true);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        UiEvents.ShowGameSettingsPanel.Invoke(false);
    }

    public void SetMaxDrones(int index, int newValue)
    {
        NetworkedUpgradeLevels upgradeLevel = upgradeLevels.Get(index);
        upgradeLevel.MaxDrones = newValue;
        upgradeLevels.Set(index, upgradeLevel);
    }

    public void SetDroneBuildTime(int index, int newValue)
    {
        NetworkedUpgradeLevels upgradeLevel = upgradeLevels.Get(index);
        upgradeLevel.DroneBuildTime = newValue;
        upgradeLevels.Set(index, upgradeLevel);
    }

    public void SetUpgradeCost(int index, int newValue)
    {
        NetworkedUpgradeLevels upgradeLevel = upgradeLevels.Get(index);
        upgradeLevel.UpgradeCost = newValue;
        upgradeLevels.Set(index, upgradeLevel);
    }

    public void SetUpgradeTime(int index, int newValue)
    {
        NetworkedUpgradeLevels upgradeLevel = upgradeLevels.Get(index);
        upgradeLevel.UpgradeTime = newValue;
        upgradeLevels.Set(index, upgradeLevel);
    }

    public void ExportSettings()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        string jsonSettings =
            $"droneSpeed: {droneSpeed} droneSendInterval: {droneSendInterval} droneDestroyTime: {droneDestroyTime} droneLaunchSpeed: {droneLaunchSpeed}";
        int levelNum = 0;
        foreach (var level in upgradeLevels)
        {
            jsonSettings +=
                $"\nBase Level {levelNum}: maxDrones: {level.MaxDrones} droneBuildTime: {level.DroneBuildTime} upgradeCost: {level.UpgradeCost} upgradeTime: {level.UpgradeTime}";
            levelNum++;
        }

        string directory = Application.persistentDataPath + "/XYZsettings/";
        string settingsFilePath = "settings.txt";
        try
        { 
            File.WriteAllText(directory + settingsFilePath, jsonSettings);
        }
        catch (Exception e)
        {
            Debug.LogError($"File write Error: {e}");
        }
#endif
    }

    [Serializable]
    public struct NetworkedUpgradeLevels : INetworkStruct
    {
        public int MaxDrones { get; set; }
        public int DroneBuildTime { get; set; }
        public int UpgradeCost { get; set; }
        public int UpgradeTime { get; set; }
    }
#endregion

#region Audio Settings

    public static float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = value;
            AudioListener.volume = _masterVolume;
            OnMasterVolumeChanged?.Invoke(_masterVolume);
        }
    }

    private static float _masterVolume = 1f;
    public static float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = value;
            OnMusicVolumeChanged?.Invoke(_musicVolume);
        }
    }

    private static float _musicVolume = 1f;
    public static float SFXVolume
    {
        get => _sfxVolume;
        set
        {
            _sfxVolume = value;
            OnSFXVolumeChanged?.Invoke(_sfxVolume);
        }
    }

    private static float _sfxVolume = 1f;
    
    public static event Action<float> OnMasterVolumeChanged;
    public static event Action<float> OnMusicVolumeChanged;
    public static event Action<float> OnSFXVolumeChanged;
    

#endregion
}

public static class NetworkExtensions
{
    public static GameSettingsManager.NetworkedUpgradeLevels ConvertUpgradeLevelsToNetworked(
        this GameSettings.BaseUpgradeLevelSettings baseSettings)
    {
        return new GameSettingsManager.NetworkedUpgradeLevels
        {
            MaxDrones = baseSettings.maxDrones,
            DroneBuildTime = baseSettings.droneBuildTime,
            UpgradeCost = baseSettings.upgradeCost,
            UpgradeTime = baseSettings.upgradeTime
        };
    }
}