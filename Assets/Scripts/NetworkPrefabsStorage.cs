using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

public class NetworkPrefabsStorage : MonoBehaviour
{
    // used for storing the network prefabs 
    public static NetworkPrefabsStorage Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NetworkPrefabsStorage>();
            }

            return _instance;
        }
    }

    private static NetworkPrefabsStorage _instance;

    [Header("Network object prefabs")]
    public NetworkManager networkManagerPrefab;
    [Space]
    public NetworkObject userPrefab;
    public NetworkObject starBaseManagerPrefab;
    public NetworkObject starMapManagerPrefab;
    public NetworkObject gameStateManagerPrefab;
    public NetworkObject teamManagerPrefab;
    public NetworkObject playerTeamAssignmentPrefab;
    public NetworkObject roomDataManagerPrefab;
    public NetworkObject gameSettingsManagerPrefab;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
}