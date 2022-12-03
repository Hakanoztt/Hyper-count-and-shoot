using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.StateMachineAI {
    public class TriggerReporter : MonoBehaviour {

        private RoutineManager _routineManager;
        private int _index;
        private bool _started;
        private float _nextTestStart;
        Action<Action>[] _tests;

        protected void Awake() {
            _routineManager = new RoutineManager();

            _tests = new Action<Action>[] {
                TestEnterExit, TestEnableDisable, TestSpawnDestroy
            };




        }

        protected void FixedUpdate() {
            _routineManager.Update(Time.fixedDeltaTime);
            if (_nextTestStart < Time.fixedTime) {
                if (_index < _tests.Length) {
                    var test = _tests[_index];
                    if (!_started) {
                        _started = true;
                        test(OnTestFinish);
                    }
                }
            }
        }

        private void OnTestFinish() {
            _started = false;
            _index++;
            _nextTestStart = Time.fixedTime + 0.5f;
            transform.DestroyAllChildren();
        }

        protected void TestEnterExit(Action onFinish) {
            var p1 = Vector3.zero;
            var p2 = new Vector3(0, 0, 10f);
            var p3 = new Vector3(0, 0, -10f);
            var b1 = CreateBody(p1, 1f, true);
            var b2 = CreateBody(p2, 1f, false);

            _routineManager.DoAction((c, d) => {
                Debug.Log("standart enter: " + b1.entered + ", " + b2.entered);
                Debug.Log("standart exit: " + b1.exited + ", " + b2.exited);

                onFinish();

            }, 1f, (progress, d) => {
                b2.rb.MovePosition(transform.TransformPoint(Vector3.LerpUnclamped(p2, p3, progress)));
                
            });
        }


        protected void TestEnableDisable(Action onFinish) {
            var b1 = CreateBody(new Vector3(0, 0, 0), 2f, true);
            var b2 = CreateBody(new Vector3(0, 0, 1f), 2f, false);
            b2.gameObject.SetActive(false);

            _routineManager.DoAction((c, d) => {
                b2.gameObject.SetActive(true);
                _routineManager.DoAction((c2, d2) => {
                    Debug.Log("activate inside collider: " + b1.entered + ", " + b2.entered);
                    b1.Reset();
                    b2.Reset();
                    b2.gameObject.SetActive(false);
                    _routineManager.DoAction((c3, d3) => {
                        Debug.Log("deactivate inside collider: " + b1.exited + ", " + b2.exited);
                        onFinish();
                    }, 0.5f);
                }, 0.5f);
            }, 0.5f);
        }


        protected void TestSpawnDestroy(Action onFinish) {
            var b1 = CreateBody(new Vector3(0, 0, 0), 2f, true);
            _routineManager.DoAction((c, d) => {
                var b2 = CreateBody(new Vector3(0, 0, 1f), 2f, false);
                _routineManager.DoAction((c2, d2) => {
                    Debug.Log("spawn inside collider: " + b1.entered + ", " + b2.entered);
                    b1.Reset();
                    b2.Reset();
                    b2.gameObject.DestroySelf();
                    _routineManager.DoAction((c3, d3) => {
                        Debug.Log("destroy inside collider: " + b1.exited);
                        onFinish();
                    }, 0.5f);
                }, 0.5f);
            }, 0.5f);
        }

        private TriggerSniffer CreateBody(Vector3 position, float radius, bool trigger) {
            var rb = new GameObject("rb").AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.transform.SetParent(transform, false);
            rb.transform.localPosition = position;
            var col = rb.gameObject.AddComponent<SphereCollider>();
            col.radius = radius;
            col.isTrigger = trigger;
            var callbacks = rb.gameObject.AddComponent<TriggerSniffer>();
            callbacks.rb = rb;
            return callbacks;
        }

        private class TriggerSniffer : MonoBehaviour {

            public bool entered;
            public bool exited;

            public Rigidbody rb;

            protected void OnTriggerEnter(Collider other) {
                Debug.Log("enter");
                entered = true;
            }
            protected void OnTriggerExit(Collider other) {
                Debug.Log("exit");
                exited = true;
            }
            public void Reset() {
                entered = false;
                exited = false;
            }
        }

    }
}