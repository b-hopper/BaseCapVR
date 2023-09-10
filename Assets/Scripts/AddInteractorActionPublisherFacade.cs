using System;
using System.Collections;
using System.Collections.Generic;
using Tilia.Interactions.Interactables.Interactables;
using Tilia.Interactions.Interactables.Interactors;
using UnityEngine;

public class AddInteractorActionPublisherFacade : MonoBehaviour
{
    [SerializeField] private string[] _interactorGONames;
    private InteractableActionReceiverFacade _interactableActionReceiverFacade;

    private void Awake()
    {
        _interactableActionReceiverFacade = GetComponent<InteractableActionReceiverFacade>();

        foreach (var interactorGoName in _interactorGONames)
        {
            var obj = GameObject.Find(interactorGoName);
                
            if (obj == null)
            { // Allows for the Interactor to be optional, in case of inactive VR rigs
                continue;
            }
            
            var interactor = obj.GetComponent<InteractorActionPublisherFacade>();
            if (interactor == null)
            {
                Debug.LogError($"Interactor {interactorGoName} does not have an InteractorActionPublisherFacade component");
                continue;
            }
            _interactableActionReceiverFacade.SourcePublishers.Add(interactor);
        }
        
        Destroy(this);
    }
}
