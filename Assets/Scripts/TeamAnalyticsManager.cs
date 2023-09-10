using Fusion;
using UnityEngine;

public class TeamAnalyticsManager : NetworkBehaviour
{
    public static TeamAnalyticsManager Instance => _instance;
    private static TeamAnalyticsManager _instance;

    public TeamSettingsManager teamSettings;

    [Networked, Capacity(12)]
    private NetworkArray<TeamDataHolder> teamData { get; } = MakeInitializer(new TeamDataHolder[12]);

    private bool _hasStateAuthority => NetworkManager.Instance.HasStateAuthority;

    public override void Spawned()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Runner.Despawn(Object);
        }
    }

    [ContextMenu("TEST_KillOneTeam")]
    private void TEST_KillOneTeam()
    {
        var randomTeam = Random.Range(0, teamSettings.teams.Count);
        var listToRemove = StarMapData.GetStarBasesByTeam(randomTeam);
        teamData.Set(randomTeam, new TeamDataHolder { bases = 0, drones = 0 });
        foreach (var b in listToRemove)
        {
            // TODO ChangeStarBaseTeam(b, -1);
        }
    }

    #region getters

    public int GetStarBaseCountForTeam(int teamIdx) => teamData[teamIdx].bases;
    public int GetTeamCount() => teamSettings.teams.Count;
    public int GetDroneCountForTeam(int teamIdx) => teamData[teamIdx].drones;


    public int GetTotalStarBaseCount()
    {
        int count = 0;
        foreach (TeamDataHolder data in teamData)
        {
            count += data.bases;
        }

        return count;
    }

    public int GetTotalDroneCount()
    {
        var count = 0;
        foreach (TeamDataHolder data in teamData)
        {
            count += data.drones;
        }

        return count;
    }

    public float GetDroneProductionForTeam(int teamIdx)
    {
        float count = 0;
        foreach (var b in StarMapData.GetStarBasesByTeam(teamIdx))
        {
            var lvl = StarBaseManager.Instance.GetStarBaseData(b.BaseId).upgradeLevel;
            var buildTime = (float)GameSettingsManager.Instance.upgradeLevels[lvl].DroneBuildTime;
            if (buildTime <= 0) continue; // don't divide by 0, also negative build times don't build drones
            float droneBuildRate = 1f / buildTime;
            count += droneBuildRate;
        }

        return count;
    }

    #endregion

    #region StarBaseCounting

    public void AddStarBaseToTeam(int teamIdx)
    {
        if (!_hasStateAuthority) return; // only the server should be able to do this
        
        TeamDataHolder data = teamData[teamIdx];
        data.bases++;
        teamData.Set(teamIdx, data);
    }

    public void ChangeStarBaseTeam(int currentTeamIdx, int newTeamIdx)
    {
        if (!_hasStateAuthority) return; // only the server should be able to do this
        
        if (currentTeamIdx != -1)
        {
            TeamDataHolder currentTeamData = teamData[currentTeamIdx];
            currentTeamData.bases--;
            teamData.Set(currentTeamIdx, currentTeamData);
        }

        if (newTeamIdx != -1)
        {
            TeamDataHolder newTeamData = teamData[newTeamIdx];
            newTeamData.bases++;
            teamData.Set(newTeamIdx, newTeamData);
        }
    }

    #endregion

    #region DroneCounting

    public void AddDronesToTeam(int teamIndex, int newDrones)
    {
        if (!_hasStateAuthority) return; // only the server should be able to do this
        
        if (teamIndex == -1) return;
        TeamDataHolder data = teamData[teamIndex];
        data.drones += newDrones;
        teamData.Set(teamIndex, data);
    }

    public void RemoveDronesFromTeam(int teamIndex, int deletedDrones)
    {
        if (!_hasStateAuthority) return;

        if (teamIndex == -1) return;
        TeamDataHolder data = teamData[teamIndex];
        data.drones -= deletedDrones;
        teamData.Set(teamIndex, data);
    }

    #endregion


    private struct TeamDataHolder : INetworkStruct
    {
        public int bases;
        public int drones;
    }
}