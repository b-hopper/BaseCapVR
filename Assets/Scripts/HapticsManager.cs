using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Tilia.Interactions.Interactables.Interactors;
using Tilia.Output.InteractorHaptics;
using UnityEngine;

public class HapticsManager : MonoBehaviour
{
    public static HapticsManager Instance;
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("HapticsManager already exists");
            Destroy(this);
            return;
        }
        Instance = this;
    }
    
    public InteractorHapticsFacade _interactorHapticsFacade;
    
    public void PlayHaptics(bool isLeft, int profile = -1 )
    {
        if (_interactorHapticsFacade == null)
        {
            Debug.LogError("InteractorHapticsFacade is null");
            return;
        }
        var interactor = isLeft ? _interactorHapticsFacade.LeftInteractor : _interactorHapticsFacade.RightInteractor;
        if (interactor == null)
        {
            Debug.LogError($"Interactor {(isLeft ? "Left" : "Right")} is null");
            return;
        }
        
        PlayHaptics(interactor, profile);
    }
    
    public void PlayHaptics(InteractorFacade interactor, int profile = -1 )
    {
        if (_interactorHapticsFacade == null)
        {
            Debug.LogError("InteractorHapticsFacade is null");
            return;
        }
        if (interactor == null)
        {
            Debug.LogError($"Interactor is null");
            return;
        }

        if (_interactorHapticsFacade.TrackedAlias != null) _interactorHapticsFacade.CancelHaptics(interactor);
        
        if (profile != -1)
        {
            _interactorHapticsFacade.Profile = profile;
            _interactorHapticsFacade.PerformProfileHaptics(interactor);
            
        }
        else
        {
            _interactorHapticsFacade.PerformDefaultHaptics(interactor);
        }
    }
}
