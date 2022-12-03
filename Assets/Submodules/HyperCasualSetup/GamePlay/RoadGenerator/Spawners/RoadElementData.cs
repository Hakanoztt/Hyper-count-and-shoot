using Mobge.Core;
using Mobge.Core.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    [Serializable]
    public struct RoadElementData {
        [ElementReference(typeof(RoadGeneratorComponent.Data))] public ElementReference roadGenerator;
        public int pieceIndex;
        [Range(0f,1f)] public float percentage;

        public void UpdateTracker(in BaseComponent.InitArgs initData, ref RoadTracker tracker) {
            var data = initData.components[roadGenerator] as RoadGeneratorComponent.Data;
            data.EnsureRoad(initData.player);
            tracker.Update(data, pieceIndex, percentage);
        }
        public RoadTracker CreateTracker(in BaseComponent.InitArgs initData) {
            var t = new RoadTracker();
            UpdateTracker(initData, ref t);
            return t;
        }
    }
    public interface IRoadElement {
        public void SetTracker(LevelPlayer player, in RoadTracker tracker, in Pose localOffset);
    }
}