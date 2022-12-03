using System;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.Core.Components;
using Mobge.HyperCasualSetup.RoadGenerator;
using UnityEngine;

namespace Mobge.RoadGenerator {
    public class RoadElementPrefabSpawnerComponent : ComponentDefinition<RoadElementPrefabSpawnerComponent.Data> {
        [Serializable]
        public class Data : PrefabSpawnerComponent.Data, IChild {
            [HideInInspector] public RoadElementData roadElementData;
            protected static RoadTracker _tempTracker;


            ElementReference IChild.Parent { get => roadElementData.roadGenerator; set { } }

            public Pose CurrentPos {
                get { return _tempTracker.Current; }
            }

            public Pose CalculatePose(in InitArgs initData) {
                var pose = CalculateAnchorPose(initData);
                var local = new Pose(position, _rotation);
                return local.GetTransformedBy(pose);
            }

            private Pose CalculateAnchorPose(in InitArgs initData) {
                roadElementData.UpdateTracker(initData, ref _tempTracker);
                return _tempTracker.Current;
            }
            public override void Start(in InitArgs initData) {
                var ins = Instantiate(res, initData.player.transform, false);

                _instance = ins.transform;
                _instance.gameObject.SetActive(false);
                Pose pose;
                IRoadElement roadElement = _instance.GetComponent<IRoadElement>();
                RoadTracker tracker = default;
                if (roadElement != null) {
                    tracker = roadElementData.CreateTracker(initData);
                    pose = tracker.Current;
                }
                else {
                    pose = CalculateAnchorPose(initData);
                }

                Pose localPose = new Pose(this.position, this._rotation);
                pose = localPose.GetTransformedBy(pose);

                _instance.localPosition = pose.position;
                _instance.localRotation = pose.rotation;
                _instance.localScale = _scale;
                _instance.gameObject.SetActive(true);

                if (roadElement != null) {
                    roadElement.SetTracker(initData.player, tracker, localPose);
                }

                var ce = _instance.GetComponent<IComponentExtension>();
                if (ce != null) {

                    ce.Start(initData);
                }

            }
        }
    }
}
            