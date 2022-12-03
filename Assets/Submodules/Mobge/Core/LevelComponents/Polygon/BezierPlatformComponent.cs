using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Serialization;
using UnityEngine;

namespace Mobge.Core.Components {
    public class BezierPlatformComponent : BasePlatformComponent<BezierPlatformComponent.Data> {
        [Serializable]
        public new class Data : BasePlatformComponent<BezierPlatformComponent.Data>.Data
        {
            public Data() {
                subdivisionCount = 6;
            }
#if UNITY_EDITOR
            public static BezierPath3D DefaultBezierPath {
                get {
                    var bezier = new BezierPath3D {
                        closed = true,
                        controlMode = BezierPath3D.ControlMode.Automatic
                    };
                    var points = new[] {
                        new BezierPath3D.Point {
                            position = new Vector3(-2, 2),
                        },
                        new BezierPath3D.Point {
                            position = new Vector3(-2, -2),
                        },
                        new BezierPath3D.Point {
                            position = new Vector3(2, -2),
                        },
                        new BezierPath3D.Point {
                            position = new Vector3(2, 2),
                        },
                    };
                    bezier.Points.SetArray(points, points.Length);
                    bezier.UpdateControlsForAuto();
                    return bezier;
                }
            }
            public BezierPath3D bezierData = DefaultBezierPath;
#else
            public BezierPath3D bezierData;
#endif
            public override Polygon[] GetPolygons() {
                Corner[] corners;
                if (bezierData.Points.Count > 1) {
                    var newCornerList = new List<Corner>();
                    var e = bezierData.GetEnumerator(1f / subdivisionCount);
                    while (e.MoveForwardByPercent(1f / subdivisionCount)) {
                        var s = e.CurrentPoint;
                        newCornerList.Add(new Corner(s));
                    }
                    corners = newCornerList.ToArray();
                }
                else {
                    corners = new Corner[0];
                }
                var polygons = new[] {
                        new Polygon(
                            corners,
                            1f,
                            false,
                            false,
                            false),
                    };
                return polygons;
            }

        }
    }
}

