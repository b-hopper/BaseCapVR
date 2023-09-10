using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeControlSettingsMenu : MonoBehaviour
{
    public Slider MasterVolumeSlider;
    public Slider MusicVolumeSlider;
    public Slider SFXVolumeSlider;
    
    private void Start()
    {
        MasterVolumeSlider.value = GameSettingsManager.MasterVolume;
        MusicVolumeSlider.value = GameSettingsManager.MusicVolume;
        SFXVolumeSlider.value = GameSettingsManager.SFXVolume;
        
        MasterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        MusicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        SFXVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
    }
    
    public void SetMasterVolume(float value)
    {
        GameSettingsManager.MasterVolume = value;
    }
    
    public void SetMusicVolume(float value)
    {
        GameSettingsManager.MusicVolume = value;
    }
    
    public void SetSFXVolume(float value)
    {
        GameSettingsManager.SFXVolume = value;
    }
    
    
}
