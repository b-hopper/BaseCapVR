using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Zinnia.Rule;

public class IsLocalPlayerRule : MonoBehaviour, IRule
{
    public bool Accepts(object target)
    {
        var nwo = (target as GameObject)?.transform.GetComponentInParent<NetworkObject>();
        if (nwo == null)
        {
            return false;
        }

        if (nwo.InputAuthority != NetworkManager.Instance._runner.LocalPlayer)
        {
            return false;
        }
        
        return true;
    }
}
