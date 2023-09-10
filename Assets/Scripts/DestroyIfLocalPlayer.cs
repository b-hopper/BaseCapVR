using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class DestroyIfLocalPlayer : MonoBehaviour
{
    [SerializeField] private bool destroyIfLocalPlayer = true;
    
    void Start()
    {
        var netObj = GetComponentInParent<NetworkObject>();
        bool isLocalPlayer = netObj != null && netObj.HasInputAuthority;
        
        if (destroyIfLocalPlayer == isLocalPlayer)
        {
            Destroy(gameObject);
        }
        else 
        {
            Destroy(this); // remove this script
        }
    }
}
