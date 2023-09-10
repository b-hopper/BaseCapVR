using System;
using UnityEngine;

public class GameSettingsPanelManager : UiPanel
{
    [SerializeField] private DroneSettingsButtons droneSettingsButtonChangers;
    [SerializeField] private BaseLevelSettingsButtons[] baseLevelSettingsButtonChangers;

    private void Start()
    {
        ShowPanel(false);
        UiEvents.ShowGameSettingsPanel.AddListener(ShowPanel);
    }

    public void Populate()
    {
        droneSettingsButtonChangers.speedSetting.Populate((value) =>
        {
            GameSettingsManager.Instance.droneSpeed = value;
            RuntimeGameSettings.droneSpeed = value;
        }, GameSettingsManager.Instance.droneSpeed);
        droneSettingsButtonChangers.sendIntervalSetting.Populate((value) =>
        {
            GameSettingsManager.Instance.droneSendInterval = value;
            RuntimeGameSettings.droneSendInterval = value;
        }, GameSettingsManager.Instance.droneSendInterval);
        droneSettingsButtonChangers.destroyTimeSetting.Populate((value) =>
        {
            GameSettingsManager.Instance.droneDestroyTime = value;
            RuntimeGameSettings.droneDestroyTime = value;
        }, GameSettingsManager.Instance.droneDestroyTime);
        droneSettingsButtonChangers.launchSpeedSetting.Populate((value) =>
        {
            GameSettingsManager.Instance.droneLaunchSpeed = value;
            RuntimeGameSettings.droneLaunchSpeed = value;
        }, GameSettingsManager.Instance.droneLaunchSpeed);
        droneSettingsButtonChangers.captureTimeSetting.Populate((value) =>
        {
            GameSettingsManager.Instance.timeToCapture = value;
            RuntimeGameSettings.captureTime = value;
        }, GameSettingsManager.Instance.timeToCapture);

        for (int i = 0; i < baseLevelSettingsButtonChangers.Length; i++)
        {
            int curIndex = i;
            baseLevelSettingsButtonChangers[i].maxDronesSetting.Populate(
                (value) =>
                {
                    GameSettingsManager.Instance.SetMaxDrones(curIndex, value);
                    RuntimeGameSettings.upgradeLevels[curIndex].maxDrones = value;
                },
                GameSettingsManager.Instance.upgradeLevels[i].MaxDrones);

            baseLevelSettingsButtonChangers[i].droneBuildTimeSetting.Populate(
                (value) =>
                {
                    GameSettingsManager.Instance.SetDroneBuildTime(curIndex, value);
                    RuntimeGameSettings.upgradeLevels[curIndex].droneBuildTime = value;
                },
                GameSettingsManager.Instance.upgradeLevels[i].DroneBuildTime);

            baseLevelSettingsButtonChangers[i].upgradeCostSetting.Populate(
                (value) =>
                {
                    GameSettingsManager.Instance.SetUpgradeCost(curIndex, value);
                    RuntimeGameSettings.upgradeLevels[curIndex].upgradeCost = value;
                },
                GameSettingsManager.Instance.upgradeLevels[i].UpgradeCost);

            baseLevelSettingsButtonChangers[i].upgradeTimeSetting.Populate(
                (value) =>
                {
                    GameSettingsManager.Instance.SetUpgradeTime(curIndex, value);
                    RuntimeGameSettings.upgradeLevels[curIndex].upgradeTime = value;
                },
                GameSettingsManager.Instance.upgradeLevels[i].UpgradeTime);
        }
    }

    public void OnExportSettings()
    {
        GameSettingsManager.Instance.ExportSettings();
    }

    [Serializable]
    private struct DroneSettingsButtons
    {
        public FloatSettingsButtonChanger speedSetting;
        public FloatSettingsButtonChanger sendIntervalSetting;
        public FloatSettingsButtonChanger destroyTimeSetting;
        public FloatSettingsButtonChanger launchSpeedSetting;
        public IntSettingsButtonChanger captureTimeSetting;
    }

    [Serializable]
    private struct BaseLevelSettingsButtons
    {
        public IntSettingsButtonChanger maxDronesSetting;
        public IntSettingsButtonChanger droneBuildTimeSetting;
        public IntSettingsButtonChanger upgradeCostSetting;
        public IntSettingsButtonChanger upgradeTimeSetting;
    }
}