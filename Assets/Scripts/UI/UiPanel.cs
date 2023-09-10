using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UiPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelContainer;

    protected virtual void ShowPanel(bool show)
    {
        panelContainer.SetActive(show);
    }
    
    // TODO:
    // These audio methods should eventually be moved to a custom UiButton class
    // This is just a quick and dirty way to get the audio working
    protected virtual void PlayHoverSound()
    {
        GlobalAudioManager.Instance.PlayButtonHoverClip();
    }
    
    protected virtual void PlayClickSound()
    { 
        GlobalAudioManager.Instance.PlayButtonPressClip();
    }
}
