using System.Linq;
using Cysharp.Threading.Tasks;
using Tilia.CameraRigs.TrackedAlias;
using Tilia.Output.InteractorHaptics;
using UnityEngine;
using Zinnia.Data.Collection.List;
using Zinnia.Data.Type;
using Zinnia.Tracking.CameraRig;
using Zinnia.Tracking.CameraRig.Collection;


namespace Fusion.XR.Shared.Rig
{
    /**
     * 
     * Networked VR user
     * 
     * Handle the synchronisation of the various rig parts: headset, left hand, right hand, and playarea (represented here by the NetworkRig)
     * Use the local HardwareRig rig parts position info when this network rig is associated with the local user 
     * 
     * Note: this class (and all network classes in this sample) is heavily focused on shared mode, hence not using InputAuthority, and cannot be used in Host or Server topologies 
     * 
     **/

    [RequireComponent(typeof(NetworkTransform))]
    // We ensure to run after the NetworkTransform or NetworkRigidbody, to be able to override the interpolation target behavior in Render()
    [OrderAfter(typeof(NetworkTransform), typeof(NetworkRigidbody))]
    public class NetworkRig : NetworkBehaviour
    {
        public HardwareRig hardwareRig;
        public NetworkHand leftHand;
        public NetworkHand rightHand;
        public NetworkHeadset headset;
        
        private LinkedAliasAssociationCollectionObservableList linkedAliasAssociationCollectionObservableList;

        [HideInInspector]
        public NetworkTransform networkTransform;

        protected virtual void Awake()
        {
            networkTransform = GetComponent<NetworkTransform>();
        }

        public virtual bool IsLocalNetworkRig => Object.HasInputAuthority;

        public override void Spawned()
        {
            base.Spawned();
            if (IsLocalNetworkRig)
            {
                InitHardwareRig();
            }
        }

        private void InitHardwareRig()
        {
            hardwareRig = FindObjectOfType<HardwareRig>(false);
            if (hardwareRig == null)
            {
                Debug.LogError("Missing HardwareRig in the scene");
                return;
            }
            
            linkedAliasAssociationCollectionObservableList = GetComponentInChildren<LinkedAliasAssociationCollectionObservableList>();
            if (linkedAliasAssociationCollectionObservableList != null)
            {
                linkedAliasAssociationCollectionObservableList.Clear();
                var linkedAliasAssociation = FindObjectOfType<LinkedAliasAssociationCollection>();
                if (linkedAliasAssociation != null)
                {
                    linkedAliasAssociationCollectionObservableList.Add(linkedAliasAssociation);
                }
            }
            
            var uiController = hardwareRig.GetComponentInChildren<HandUiController>(false);
            
            if (uiController != null)
            {
                uiController.AssignPlayer(Object.InputAuthority);
            }
            else
            {
                Debug.LogError("Missing UIController on HardwareRig");
            }
            
            SetPositionAndRotation();

            SetupHapticsManager();
        }

        private void SetupHapticsManager()
        {
            var hapticsFacade = FindObjectOfType<InteractorHapticsFacade>();
            if (hapticsFacade != null)
            {
                hapticsFacade.TrackedAlias = GetComponent<TrackedAliasFacade>();
            }
            else
            {
                Debug.LogError("Missing HapticsManager in the scene");
            }
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            // update the rig at each network tick
            if (GetInput<RigInput>(out var input))
            {
                ApplyInputToRigParts(input);

                ApplyInputToHandPoses(input);

            }
        }

        protected virtual void ApplyInputToRigParts(RigInput input)
        {
            transform.position = input.playAreaPosition;
            transform.rotation = input.playAreaRotation;
            leftHand.transform.position = input.leftHandPosition;
            leftHand.transform.rotation = input.leftHandRotation;
            rightHand.transform.position = input.rightHandPosition;
            rightHand.transform.rotation = input.rightHandRotation;
            headset.transform.position = input.headsetPosition;
            headset.transform.rotation = input.headsetRotation;
        }
        protected virtual void ApplyInputToHandPoses(RigInput input)
        {
            // we update the hand pose info. It will trigger on network hands OnHandCommandChange on all clients, and update the hand representation accordingly
            leftHand.HandCommand = input.leftHandCommand;
            rightHand.HandCommand = input.rightHandCommand;
        }

        public override void Render()
        {
            base.Render();
            if (IsLocalNetworkRig)
            {
                // Extrapolate for local user :
                // we want to have the visual at the good position as soon as possible, so we force the visuals to follow the most fresh hardware positions
                // To update the visual object, and not the actual networked position, we move the interpolation targets
                networkTransform.InterpolationTarget.position = hardwareRig.transform.position;
                networkTransform.InterpolationTarget.rotation = hardwareRig.transform.rotation;
                leftHand.networkTransform.InterpolationTarget.position = hardwareRig.leftHand.transform.position;
                leftHand.networkTransform.InterpolationTarget.rotation = hardwareRig.leftHand.transform.rotation;
                rightHand.networkTransform.InterpolationTarget.position = hardwareRig.rightHand.transform.position;
                rightHand.networkTransform.InterpolationTarget.rotation = hardwareRig.rightHand.transform.rotation;
                headset.networkTransform.InterpolationTarget.position = hardwareRig.headset.transform.position;
                headset.networkTransform.InterpolationTarget.rotation = hardwareRig.headset.transform.rotation;
            }
        }
        
        private void SetPositionAndRotation()
        {
            if (!IsLocalNetworkRig)
            {
                return;
            }
            RPC_RequestSetPositionAndRotation();
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private async void RPC_RequestSetPositionAndRotation()
        {
            await UniTask.WaitUntil(() => StarmapManager.Instance != null);// && StarmapManager.Instance.initialized);
            
            var player = Object.InputAuthority;
            var pos = StarmapManager.GetPlayerPosition(PlayerTeamAssignmentManager.Instance.GetPlayerTeam(player));

            RPC_SetPosition(pos);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        private void RPC_SetPosition(Vector3 pos)
        {
            SetPositionInternal(pos);
        }
        
        private void SetPositionInternal(Vector3 pos)
        {
            transform.position = pos;

            hardwareRig.transform.position = transform.position;
            hardwareRig.transform.rotation = transform.rotation;
            
            UiEvents.MoveUiToPlayer.Invoke();
        }
    }
}
