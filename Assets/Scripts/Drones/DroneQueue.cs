using System.Collections.Generic;
using Fusion;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Drones
{
    public class DroneQueue : NetworkBehaviour
    {
        [SerializeField] private Drone dronePrefab;
        private Queue<Drone> _droneQueue;
        public List<StarBase> connectedBases;
        [SerializeField] [ReadOnly] private int _teamId = -1;

        public int TeamIndex
        {
            get => _teamId;
            set
            {
                _teamId = value;
                teamUpdated.Invoke();
            }
        }

        public UnityEvent teamUpdated = new();

        public int droneDamage = 1;
        public int droneDefense = 1;
        [Networked] private ref DroneMovementTicker _moveTicker => ref MakeRef(DroneMovementTicker.CreateSecondTicker);


        public override void Spawned()
        {
            _moveTicker.Initialize(Runner);
            _droneQueue = new Queue<Drone>();
        }

        public override void FixedUpdateNetwork()
        {
            if (NetworkManager.Instance.HasStateAuthority)
            {
                _moveTicker.Tick(Runner);
                if (_moveTicker.moveHasTicked) Tick();
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        protected void RPC_SendDroneLocal(int droneCount)
        {
            RemoveDrones(droneCount);
        }

        protected void CreateDrone(int[] route)
        {
            var dest = connectedBases.Find(x => x.BaseId == route[1]);
            // spawn the drone outside of the base hitbox
            var forward = Quaternion.LookRotation(dest.transform.position -
                transform.position).normalized;
            Runner.Spawn(dronePrefab, transform.position, forward, 
                onBeforeSpawned: (r, o) => { RPC_ConfigureDrone(route, o); });
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ConfigureDrone(int[] route, NetworkObject o)
        {
            ConfigureDroneInternal(route, o);
        }

        private void ConfigureDroneInternal(int[] route, NetworkObject o)
        {
            var drone = o.GetComponent<Drone>();
            drone.transform.SetParent(transform.parent);
            drone.transform.localScale = Vector3.one;
            drone.gameObject.SetActive(false);
            drone.SetRoute(route, connectedBases, transform.localPosition);
            drone.PopulateStats(TeamIndex, TeamAnalyticsManager.Instance.teamSettings.GetTeamData(TeamIndex).teamColor,
                droneDamage, droneDefense);
            _droneQueue.Enqueue(drone);
        }

        int _lastTick = 0;
        protected void Tick()
        {
            if (!NetworkManager.Instance.HasStateAuthority) return;
            
            if (_droneQueue == null || _droneQueue.Count == 0) return;
            if (_lastTick == _moveTicker.moveTicksElapsed) return;
            _lastTick = _moveTicker.moveTicksElapsed;
            Drone drone = _droneQueue.Dequeue();
            drone.RPC_Send();
        }

        public void DroneContinuing(Drone drone)
        {
            drone.UpdateDestination(connectedBases);
            drone.gameObject.SetActive(false);
            _droneQueue.Enqueue(drone);
        }

        
    
        internal static bool _isSettingWaypoint;
        internal static DroneQueue _originBase;
        internal DroneQueue _waypointBase;

        public void WaypointPress()
        {
            if (_isSettingWaypoint)
            {
                WaypointSecondPress();
            }
            else
            {
                WaypointFirstPress();
            }
        }
    
        public void WaypointFirstPress()
        {
            _isSettingWaypoint = true;
            _originBase = this;
        }
    
        public void WaypointSecondPress()
        {
            if (!_isSettingWaypoint) return;

            _originBase.SetWaypoint(this);
        }
    
        public void SetWaypoint(DroneQueue waypoint)
        {
            if (_originBase == waypoint)
            {
                _isSettingWaypoint = false;
                _originBase = null;
                return;
            }
            
            _waypointBase = waypoint;
            _isSettingWaypoint = false;
            _originBase = null;
        }

        public void CancelWaypoint()
        {
            SetWaypoint(null);
        }
        
        public virtual void DroneArrived(Drone drone) { }
        public virtual void AddDrone() { }
        public virtual void AddDrones(int amount) { }
        public virtual void RemoveDrones(int amount) { }
        
    }
}