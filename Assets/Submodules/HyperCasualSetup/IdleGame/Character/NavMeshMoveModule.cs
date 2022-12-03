using Mobge.Graph;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Mobge.IdleGame
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshMoveModule : MonoBehaviour {

        private NavMeshAgent _navMeshAgent;


        public float Speed { 
            get=> _navMeshAgent.speed;
            set => _navMeshAgent.speed = value;
        }
        public float TurnSpeed {
            get => _navMeshAgent.angularSpeed;
            set => _navMeshAgent.angularSpeed = value;
        }
        public Vector2 Input { get; set; }

        private float _navigatingEnabledTime;
        private bool _navigating;



        public int AreaMask {
            get => _navMeshAgent.areaMask;
            set => _navMeshAgent.areaMask = value;
        }
        public bool IsNavigating {
            get {
                return _navigating || _navigatingEnabledTime >= Time.fixedTime;
            }
        }

        public Vector3 CurrentVelocity { get => _navMeshAgent.velocity; }

        public bool IsOnNavMesh { get => _navMeshAgent.isOnNavMesh; }

        public void Teleport(Pose pose) {
            if (IsOnNavMesh) {
                _navMeshAgent.Warp(pose.position);
                transform.rotation = pose.rotation;
            }
            else {
                transform.position = pose.position;
                transform.rotation = pose.rotation;
            }
        }

        public bool NavigationEnabled {
            get {
                return _navMeshAgent.enabled;
            }
            set {
                _navMeshAgent.enabled = value;
            }
        }

        protected void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();

        }

        public void Navigate(Vector3 worldtarget) {
            _navMeshAgent.SetDestination(worldtarget);
            _navigatingEnabledTime = Time.fixedTime + Time.fixedDeltaTime*1.5f;
        }
        public void StopNavigating() {
            _navMeshAgent.ResetPath();
        }

        public void SetWorldInput(Vector3 input)
        {
            float x = CalculateTurnInput(input);
            float y = input.magnitude;

            Input = new Vector2(x, y);
        }
        private float CalculateTurnInput(Vector3 worldDirection) {
            Vector3 f = transform.forward;
            f.y = 0;
            worldDirection.y = 0;

            Vector3 normal = new Vector3(-worldDirection.z, 0, worldDirection.x);

            bool reverse = Vector3.Dot(f, worldDirection) < 0;

            float difSin = Vector3.Dot(f, normal);
            if (reverse) {
                if (difSin > 0) {
                    difSin = 1;
                }
                else {
                    difSin = -1;
                }
            }

            return Mathf.Clamp(difSin, -1, 1);
        }
        public void Turn(Vector3 worldDirection) {
            var i = this.Input;
            i.x = CalculateTurnInput(worldDirection);
            this.Input = i;

        }

        protected void Update()
        {
            bool navigating = _navMeshAgent.pathPending || _navMeshAgent.hasPath;
            if (_navigating != navigating) {
                _navigating = navigating;
                if (!_navigating) {
                    _navigatingEnabledTime = Time.fixedTime + Time.fixedDeltaTime * 1.5f;
                }
            }

            if (!IsNavigating) {
                float dt = Time.deltaTime;
                
                transform.Rotate(Vector3.up, TurnSpeed * dt * Input.x);
                float linSpeed = Speed * Input.y;

                var des = transform.forward * linSpeed;
                var dif = _navMeshAgent.velocity - des;
                if (dif.sqrMagnitude > 0.0001f) {
                    _navMeshAgent.velocity = des;
                }
            }

        }
    }
}