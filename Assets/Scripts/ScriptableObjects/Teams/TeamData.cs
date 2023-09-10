using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TeamData", menuName = "ScriptableObjects/Team/TeamData")]
public class TeamData : ScriptableObject
{
    public string teamName = "Unnamed Team";
    public Color teamColor = Color.white;
    public Color teamColorSecondary = Color.white;
    public Color highlightColor = Color.white;
}
