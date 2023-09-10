using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;

public class UpgradeTool : MonoBehaviour
{
    List<StarBase> _highlightedBases = new List<StarBase>();
    public void HighlightEligibleBases()
    {
        if (PlayerTeamAssignmentManager.Instance == null)
        { 
            return;
        }
        int teamIdx = PlayerTeamAssignmentManager.Instance.GetLocalPlayerTeam();

        _highlightedBases = StarMapData.StarBases.Where
            (x => x.TeamIndex == teamIdx && StarBaseManager.Instance.CanUpgradeBase(x.BaseId)).ToList();

        foreach (var starBase in _highlightedBases)
        {
            starBase.EnableUpgradeIndicator(true);
        }
    }
    
    public void UnhighlightEligibleBases()
    {
        foreach (var starBase in _highlightedBases)
        {
            starBase.EnableUpgradeIndicator(false);

        }
    }
}
