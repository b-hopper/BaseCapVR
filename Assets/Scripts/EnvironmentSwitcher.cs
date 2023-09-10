using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject _environment3d;
    [SerializeField] private GameObject _environmentPlaceholder;

    public void ToggleEnvironment()
    {
        if (_environment3d == null || _environmentPlaceholder == null)
        {
            Debug.LogError("EnvironmentSwitcher is missing a reference to an environment");
            return;
        }
        
        bool enabled = _environment3d.activeSelf;
        _environment3d.SetActive(!enabled);
        _environmentPlaceholder.SetActive(enabled);
    }
}
