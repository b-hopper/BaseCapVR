using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class QuitGameButton : MonoBehaviour
{
    [ContextMenu("Quit")]
    public void QuitButtonPressed()
    {
        QuitButtonAsync().Forget();
    }
    
    private async UniTaskVoid QuitButtonAsync()
    {
        await NetworkManager.Instance.ExitRoom();
        UiEvents.ShowUi.Invoke(true);
        UiEvents.ShowLobbyPanel.Invoke(true);
    }
}
