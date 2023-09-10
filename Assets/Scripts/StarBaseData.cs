using Fusion;
using UnityEngine.Serialization;

// StarBaseData is managed by Fusion
public struct StarBaseData : INetworkStruct
{
    /// <summary>
    /// the unique id of the base
    /// </summary>
    public int id;

    /// <summary>
    /// the player that owns the base, -1 for a neutral base
    /// </summary>
    public int team;

    /// <summary>
    /// the current population of drones at the base
    /// </summary>
    public int droneCount;

    /// <summary>
    /// the current upgrade level of the base
    /// </summary>
    public int upgradeLevel;

    /// <summary>
    /// amount of time remaining to upgrade the base
    /// </summary>
    public int upgradeTime;

    /// <summary>
    /// amount of time remaining to capture the base
    /// </summary>
    public int captureTime;

    public void ProduceDrone(int amount)
    {
        droneCount += amount;
        //Debug.Log($"base {id} adding a drone. Current count {droneCount}");
    }

    public void RemoveDrone(int amount)
    {
        droneCount -= amount;
        if (droneCount < 0) droneCount = 0;
    }

    public void TickUpgradeTime()
    {
        upgradeTime--;
    }
    
    public void TickCaptureTime()
    {
        captureTime--;
    }
}