using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Drones
{
    public class Drone : NetworkBehaviour
    {
        [SerializeField] private MeshRenderer _renderer;
        
        [SerializeField] private DroneParticles _particles;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private DroneAudioClips _audioClips;
        public int damage { get; private set; }
        public int defense { get; private set; }
        public int teamIndex { get; private set; }

        private StarBase origin, destination;
        private int[] route;
        private int destinationIndex;
        private bool stopped = true;
        private bool killed = false;
        private Vector3 curPosition;
        
        [Serializable] private struct DroneParticles
        {
            public ParticleSystem explosion;
            public ParticleSystem trail;
        }
        
        [Serializable] private struct DroneAudioClips
        {
            public List<AudioClip> combatSFX;
        }
        
        private void Start()
        {
            // hook the drones into an event to detect when the game is over
            GameStateManager.GameOverEvent.AddListener(OnGameOver);
        }

        public void FixedUpdate()
        {
            if (stopped)
            {
                return;
            }
            float step = GameSettingsManager.Instance.droneSpeed * Time.fixedDeltaTime;
            transform.localPosition =
                Vector3.MoveTowards(transform.localPosition, destination.transform.localPosition, step);
        }

        public void OnCollisionEnter(Collision other)
        {
            // Only the state authority should process collisions
            if (!NetworkManager.Instance.HasStateAuthority) return;
            
            if (stopped) return; 

            if (other.gameObject.CompareTag("Drone"))
            {
                if (other.transform.parent != transform.parent) return; // Only collide if they are on the same path

                ProcessDroneCollision(other);
            }

            if (other.gameObject.CompareTag("StarBase"))
            {
                if (other.gameObject == destination.gameObject) // prevent collisions with other bases
                {
                    OnArrival();
                }
            }
        }

        private void ProcessDroneCollision(Collision other)
        {
            if (NetworkManager.Instance.HasStateAuthority)
            {
                Drone otherDrone = other.gameObject.GetComponent<Drone>();
                if (otherDrone.teamIndex == teamIndex || otherDrone.stopped || gameObject.IsDestroyed()) return;

                var incDamage = otherDrone.damage;
                
                otherDrone.RPC_ProcessDroneCollision(damage);
                RPC_ProcessDroneCollision(incDamage);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_ProcessDroneCollision(int incomingDamage)
        {
            ProcessDroneCollisionInternal(incomingDamage);
        }

        private void ProcessDroneCollisionInternal(int incomingDamage)
        {
            defense -= incomingDamage;

            _audioSource.volume = GameSettingsManager.SFXVolume;
            _audioSource.PlayOneShot(_audioClips.combatSFX[Random.Range(0, _audioClips.combatSFX.Count)]);
            
            if (defense <= 0)
            {
                KillDrone();
            }
        }

        public void KillDrone()
        {
            KillDroneInternal();
            stopped = true;
        }
        private void KillDroneInternal()
        {
            killed = true;
            TeamAnalyticsManager.Instance.RemoveDronesFromTeam(teamIndex, 1);
            _rigidbody.constraints = RigidbodyConstraints.None;
            _particles.explosion.Play();
            _particles.trail.Stop();
            Collider col = GetComponent<Collider>();
            if (col != null ) col.enabled = false;

            var yeetDirection = new Vector3(Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f));
    
            _rigidbody.AddExplosionForce(GameSettingsManager.Instance.droneLaunchSpeed, transform.position + yeetDirection + transform.forward * 0.5f, 2f);
            _rigidbody.AddTorque(yeetDirection * 100f);
 
            StartCoroutine(DestroyTimer());
        }


        private IEnumerator DestroyTimer()
        {
            var time = GameSettingsManager.Instance.droneDestroyTime;

            var scale = _renderer.transform.localScale;
            while (time > 0)
            {
                time -= Time.deltaTime;
                _renderer.transform.localScale = scale * (time / GameSettingsManager.Instance.droneDestroyTime);
                yield return new WaitForFixedUpdate();
            }
            
            if (NetworkManager.Instance.HasStateAuthority)
            {
                Runner.Despawn(Object);
            }
        }

        private void OnGameOver()
        {
            Destroy(gameObject);
        }

        public void SetRoute(int[] path, List<StarBase> connections, Vector3 startPos)
        {
            curPosition = startPos;
            destinationIndex = 0;
            route = path;
            UpdateDestination(connections);
        }

        public void PopulateStats(int team, Color teamColor, int damage, int defense)
        {
            teamIndex = team;
            this.damage = damage;
            this.defense = defense;
            _renderer.material.color = teamColor;
            _particles.explosion.GetComponent<ParticleSystemRenderer>().material.color = teamColor;
        }

        private Transform _oldParent;

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        public void RPC_Send()
        {
            SendInternal();
        }
        
        LinePath pathBetween;
        
        private void SendInternal()
        {
            if (this == null || gameObject.IsDestroyed()) return;
            transform.localPosition = curPosition;

            // set the drone to be a child of the path
            _oldParent = transform.parent;
            pathBetween = StarMapData.GetLinePathBetween(origin, destination);
            transform.parent = pathBetween.transform;
            pathBetween.AddDroneToLine(origin);

            Quaternion rotation = Quaternion.LookRotation(destination.transform.position - transform.position);
            transform.rotation = rotation;

            gameObject.SetActive(true);
            stopped = false;
            
            origin.audioManager.PlayUnitLaunchClip();
        }

        public void UpdateDestination(List<StarBase> connections)
        {
            origin = StarMapData.StarBases[route[destinationIndex]];
            destinationIndex++;
            destination = connections.Find(x => x.BaseId == route[destinationIndex]);
        }

        private void OnArrival()
        {
            RPC_OnArrival();
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_OnArrival()
        {
            stopped = true;
            transform.parent = _oldParent;
            if (pathBetween != null)
            {
                pathBetween.RemoveDroneFromLine(origin);
                pathBetween = null;
            }

            //gameObject.SetActive(false);
            curPosition = destination.transform.localPosition;
            if (destination.BaseId == route[^1] || destination.TeamIndex != teamIndex)
            {
                if (!NetworkManager.Instance.HasStateAuthority) return;

                destination.DroneArrived(this);

                return;
            }

            destination.DroneContinuing(this);
        }
    }
}