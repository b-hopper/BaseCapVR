using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] List<AudioClip> _musicClips = new List<AudioClip>();
    [SerializeField] AudioSource _audioSource;

    private float _startVolume;
    
    private void Start()
    {
        _startVolume = _audioSource.volume;
        GameSettingsManager.OnMusicVolumeChanged += OnMusicVolumeChanged;
        LoopMusicForever().Forget();
    }

    private void OnMusicVolumeChanged(float vol)
    {
        _audioSource.volume = _startVolume * vol;
    }

    private static int _lastPlayed = -1;
    private async UniTask LoopMusicForever()
    {
        while (true)
        {
            _audioSource.clip = GetNextClip();
            _audioSource.Play();
            await UniTask.Delay(TimeSpan.FromSeconds(_audioSource.clip.length));
        }
    }

    private AudioClip GetNextClip()
    {
        var nextClip = _musicClips[UnityEngine.Random.Range(0, _musicClips.Count)];
        if (_lastPlayed == _musicClips.IndexOf(nextClip))
        {
            return GetNextClip();
        }
        _lastPlayed = _musicClips.IndexOf(nextClip);
        return nextClip;
    }
}
