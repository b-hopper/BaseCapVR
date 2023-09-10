using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "ScriptableObjects/GameSettings")]
public class GameSettings : ScriptableObject
{[Header("Drone Settings")]
    public float speed;
    [Tooltip("The interval at which drones are sent from a base")]public float sendInterval;
    [Tooltip("The amount of time to destroy the drone after collision")]public float destroyTime;
    [Tooltip("The speed of the launch upon colliding with another drone")]public float launchSpeed;
    [Tooltip("Time in seconds it takes to capture an unoccupied starbase")]public int captureTime;

    [Header("Base Settings")]
    public List<BaseUpgradeLevelSettings> upgradeLevels;

    [Serializable]
    public class BaseUpgradeLevelSettings
    {
        public int maxDrones;
        public int droneBuildTime;
        public int upgradeCost;
        public int upgradeTime;

        public static BaseUpgradeLevelSettings Duplicate(BaseUpgradeLevelSettings old)
        {
            return new BaseUpgradeLevelSettings
            {
                maxDrones = old.maxDrones,
                droneBuildTime = old.droneBuildTime,
                upgradeCost = old.upgradeCost,
                upgradeTime = old.upgradeTime
            };
        }
    }
}
