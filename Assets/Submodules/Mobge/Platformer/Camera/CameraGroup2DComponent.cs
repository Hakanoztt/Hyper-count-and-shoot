using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Platformer
{
    public class CameraGroup2DComponent : ComponentDefinition<CameraGroup2DComponent.Data>
    {
        public class Data : Camera2DComponent.BaseData
        {
            public float maxZOffset = 25;
            public bool snapGround = false;
            public float snapBreakOffset = 0.7f; 
            public float maxHorizonralOffsetRate = 0.2f;
            public Vector3 followForce = new Vector3(10, 10, 10);
            public DynamicData[] points;
            private ActionManager.Routine _updateRoutine;
            public override void Start(in InitArgs initData) {
                var data = new Side2DCameraData();
                data.maxZOffset = maxZOffset;
                data.snapGround = snapGround;
                data.snapBreakOffset = snapBreakOffset;
                data.maxHorizonralOffsetRate = maxHorizonralOffsetRate;
                data.followForce = followForce;
                base.Start(initData, data);
            }
            protected override void Activate() {
                _updateRoutine.EnsureRunning(_player.ActionManager, UpdateData);
                base.Activate();
            }

            protected override void Deactivate() {
                base.Deactivate();
                _updateRoutine.Stop();
            }
            private void UpdateData(float obj) {
                //var cam = ACameraController.FindOrCreateForLevel<Side2DCamera>(_player);
                
            }

            public struct DynamicData
            {
                public float zOffset;
                public Vector2 movementRate;
                public float verticalOffset;
                public float horizontalOffset;
                public float verticalOffsetAir;
            }
        }
    }
}