using Fusion.XR.Shared.Rig;
using UnityEngine;

public class UiCanvasManager : MonoBehaviour
{
    private Transform playerTransform;

    private void Awake()
    {
        UiEvents.ShowUi.AddListener((show) => gameObject.SetActive(show));
        UiEvents.MoveUiToPlayer.AddListener(UpdatePosition);
    }

    private void Start()
    {
        HardwareRig hardwareRig = FindObjectOfType<HardwareRig>(false);
        if (hardwareRig == null)
        {
            Debug.LogError("Missing HardwareRig in the scene");
            return;
        }

        // check if it's the spatial simulator or not
        Transform editorCharacter = hardwareRig.transform.Find("AvatarObjects");

        playerTransform = (editorCharacter != null) ? editorCharacter : hardwareRig.gameObject.transform;
        
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (playerTransform != null)
        {
            Vector3 curPlayerPos = playerTransform.position;
            transform.position = new Vector3(transform.position.x, transform.position.y, curPlayerPos.z);
        }
    }
}