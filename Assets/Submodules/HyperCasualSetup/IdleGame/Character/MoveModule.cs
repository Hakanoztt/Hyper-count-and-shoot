using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mobge.IdleGame {

    public class MoveModule : MonoBehaviour {

        public float speed = 8f;
        public float turnSpeed = 540f;

        public Vector2 Input { get; set; }

        private CharacterController _controller;


        protected void Awake() {
            _controller = GetComponent<CharacterController>();
        }

        public void SetWorldInput(Vector3 input) {
            Vector3 f = transform.forward;
            f.y = 0;
            input.y = 0;

            Vector3 normal = new Vector3(-input.z, 0, input.x);

            bool reverse = Vector3.Dot(f, input) < 0;

            float difSin = Vector3.Dot(f, normal);
            if (reverse) {
                if(difSin > 0) {
                    difSin = 1;
                }
                else {
                    difSin = -1;
                }
            }

            float x = Mathf.Clamp(10 * difSin, -1, 1);
            float y = input.magnitude;

            Input = new Vector2(x, y);

            
        }

        protected void Update() {
            float dt = Time.fixedDeltaTime;
            _controller.transform.Rotate(Vector3.up, turnSpeed * dt * Input.x);
            float linSpeed = speed * Input.y;
            //_controller.Move(_controller.transform.forward * linSpeed);
            _controller.SimpleMove(_controller.transform.forward * linSpeed);
        }
    }
}