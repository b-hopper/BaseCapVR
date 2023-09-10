using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TeamSettingsManager : MonoBehaviour
{

    [Header("Team Settings")] 
    public TeamData neutralTeam;
    public List<TeamData> teams;

    [Header("Team Color Options")]
    public SelectableColorList colorOptions;

    [HideInInspector] public UnityEvent<int, TeamData> onColorChanged = new();

    public TeamData GetTeamData(int teamIdx)
    {
        var idx = Mathf.Clamp(teamIdx, -1, teams.Count - 1);
        if (idx == -1)
        {
            return neutralTeam;
        }

        return teams[idx];
    }

    public void ChangeTeamColor(int team, int colorIndex)
    {
        teams[team].teamColor = colorOptions.selectableColors[colorIndex].color;
        teams[team].teamColorSecondary = colorOptions.selectableColors[colorIndex].secondaryColor;
        teams[team].highlightColor = colorOptions.selectableColors[colorIndex].highlightColor;
        
        onColorChanged.Invoke(team, teams[team]);
    }
}