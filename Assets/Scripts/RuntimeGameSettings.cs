using System.Collections.Generic;

public static class RuntimeGameSettings
{
    public static float droneSpeed;
    public static float droneSendInterval;
    public static float droneDestroyTime;
    public static float droneLaunchSpeed;
    public static int captureTime;
    
    public static List<GameSettings.BaseUpgradeLevelSettings> upgradeLevels = new();
}