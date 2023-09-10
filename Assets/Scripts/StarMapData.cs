using System.Collections.Generic;
using System.Linq;

// holds data and retrieval methods for the star bases and other map data
public static class StarMapData
{
    public static List<StarBase> StarBases { get; private set; }
    public static Dictionary<StarBase, List<StarBase>> StarConnections { get; private set; }
    public static Dictionary<StarBase, List<(StarBase, LinePath)>> StarLines = new();

    #region Getters

    public static List<StarBase> GetStarBasesByTeam(int teamIndex)
    {
        return StarBases.FindAll(x => x.TeamIndex == teamIndex);
    }

    #endregion
    
    #region SetReset

    public static void SetStarBases(List<StarBase> starBases)
    {
        StarBases = starBases;
    }

    public static void SortStarBasesByPosition()
    {
        StarBases = StarBases.OrderBy(x => x.transform.position.z).ToList();
    }

    public static void ResetStarBases()
    {
        StarBases = new List<StarBase>();
    }

    public static void ResetStarConnections()
    {
        StarConnections = new Dictionary<StarBase, List<StarBase>>();
    }

    public static void ResetStarLines()
    {
        StarLines = new Dictionary<StarBase, List<(StarBase, LinePath)>>();
    }

    public static LinePath GetLinePathBetween(StarBase a, StarBase b)
    {
        if (StarLines.TryGetValue(a, out var lines))
        {
            var line = lines.Find(x => x.Item1.BaseId == b.BaseId);
            if (line != default)
            {
                return line.Item2;
            }
        }
        
        return null;
    }
    
    public static LinePath GetLinePathBetween(int aId, int bId)
    {
        var a = StarBases.Find(x => x.BaseId == aId);
        var b = StarBases.Find(x => x.BaseId == bId);
        return GetLinePathBetween(a, b);
    }

    #endregion
}
