using System;
using System.Collections.Generic;
using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge{
    public partial class ElementEditor {
        

        public class VisualHandler
        {
            public class LogicComponentProperties {
                public float marginRate = 0.15f;
                public float slotRadiusRate = 0.35f;
                public float slotSpaceRate = 0.2f;
            }
            private static LogicSlot[] _emptySlots = new LogicSlot[0];
            private static LogicComponentProperties _logicProperties = new LogicComponentProperties();
            private static Texture2D _pixelTexture;
            //private Axis _axis;
            public Matrix4x4 matrix = Matrix4x4.identity;
            public Vector2 ScreenMousePosition
            {
                get => Event.current.mousePosition;
            }
			public void DrawSelectionRect(Rect r)
            {
                Handles.BeginGUI();
                EditorGUI.DrawRect(r, new Color(0.6f, 0.6f, 1, 0.1f));
                Handles.EndGUI();
                //Handles.color = Color.white;
                //Handles.DrawSolidRectangleWithOutline(r, new Color(0.6f, 0.6f, 1, 0.1f), new Color(0.6f, 0.6f, 1, 0.3f));
            }
            public VisualHandler() {
                if(_pixelTexture == null) {
                    _pixelTexture = new Texture2D(1, 1);
                    _pixelTexture.SetPixel(0, 0, Color.white);
                }
            }
            public Ray MouseRay {
                get {
                    var ray = HandleUtility.GUIPointToWorldRay(ScreenMousePosition);
                    var inverse = Handles.matrix.inverse;
                    var o = inverse.MultiplyPoint3x4(ray.origin);
                    var d = inverse.MultiplyVector(ray.direction);
                    return new Ray(o, d);
                }
            }
            public Vector3 MousePosition {
                get {
                    var pivot = SceneView.lastActiveSceneView.pivot;
                    var cameraPos = SceneView.lastActiveSceneView.camera.transform.position;
                    var distance = Vector3.Distance(cameraPos, pivot);
                    return MouseRay.GetPoint(distance);
                }
            }
            public  float ElementRadius(Vector3 position) {
                var size = HandleUtility.GetHandleSize(position);
                return size * 0.2f;
            }
            private void DrawSlots(AEditableElement.SlotList slots, Vector3 startPos, float radius, in Axis a) {
                float sRadius = _logicProperties.slotRadiusRate * radius;
                
                for(int i = 0; i < slots.Count; i++) {
                    Handles.color = Color.black;
                    Handles.DrawSolidDisc(startPos, a.forward, sRadius);
                    Handles.color = Color.green;
                    Handles.DrawSolidDisc(startPos, a.forward, sRadius * 0.5f);
                    slots.SetPosition(i, startPos, sRadius, a);
                    startPos -= (_logicProperties.slotRadiusRate * 2 + _logicProperties.slotSpaceRate) * radius * a.up;

                }
            }
            public void DrawElement(AEditableElement e, Vector3 pos, bool highlited, bool selected, bool logicEnabled) {
                float r = ElementRadius(pos) / Handles.matrix.lossyScale.magnitude * 1.8f;
                float dim = r;
                // if(selected) {
                //     dim *= 1.2f;
                // }
                // float z = pos.z;
                var inputSlots = e.inputSlots;
                var outputSlots = e.outputSlots;
                bool logicMode = logicEnabled && inputSlots.Count + outputSlots.Count > 0;
                
                var lp = _logicProperties;
                Vector2 extends;
                if(logicMode) {
                    // float slotRadius = r * 0.3f;
                    float hWidth = dim;
                    float hHeight = dim;
                    int maxCount = Mathf.Max(inputSlots.Count, outputSlots.Count);
                    float requiredHeightRate = maxCount * (2 * lp.slotRadiusRate + lp.slotSpaceRate) - lp.slotSpaceRate + 2 * lp.marginRate;
                    hHeight = Mathf.Max(hHeight, requiredHeightRate * r * 0.5f);
                    extends = new Vector2(hWidth, hHeight);
                }
                else {
                    extends = new Vector2(dim, dim);
                }
                //Rect rect = new Rect((Vector2)pos - extends, extends * 2f);
                Handles.color = Color.white;
                Color color;
                if (selected) {
                    color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
                }
                else {
                    if (highlited) {
                        color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    }
                    else {
                        color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
                    }
                }
                e.selectionArea.Update(pos, extends * 2f);
                //var normal = (Camera.current.transform.position - Handles.matrix.MultiplyPoint3x4(pos)).normalized;
                var normal = -Camera.current.CameraToWorldPointRay(Handles.matrix.MultiplyPoint3x4(pos)).direction;
                Axis a = new Axis(normal, Camera.current.transform.up);
                var corners = e.selectionArea.GetCorners(a);
				Handles.DrawSolidRectangleWithOutline(corners, color, new Color(0.5f, 0.5f, 0.5f, 1));
				if (e.IconTexture) {
					Vector3 _offset = new Vector3 {
						x = (corners[0].x - corners[3].x) / 32f,
						y = (corners[2].y - corners[3].y) / 8f
					};
                    var _r = corners[3] + _offset;
					//var _r = e.selectionArea.Corners[3];
					Handles.Label(_r, e.IconTexture);
                } 

				if (logicMode) {
                    Vector2 outputPos = extends - new Vector2(0, lp.marginRate + lp.slotRadiusRate) * r;
                    Vector2 inputPos = outputPos;
                    inputPos.x = - extends.x + lp.marginRate * r;
					// var sRadius = lp.slotRadiusRate * r;
                    DrawSlots(e.inputSlots, pos + a.Convert(inputPos), r, a);
                    DrawSlots(e.outputSlots, pos + a.Convert(outputPos), r, a);
                }
            }
            public Bezier GetBezier(Vector3 input, in Axis inDir, Vector3 output, in Axis outDir) {
                Bezier b;
                b.point1 = input;
                b.point2 = output;
                var offset = (output - input).magnitude * 0.5f;
                float yDif = Mathf.Abs(input.y - output.y);
                float xDif = (output.x - input.x) * 0.4f;
                float offsetY = xDif > 0 && yDif < xDif ? xDif : 0;
                b.tangent1 = input;
                b.tangent2 = output;
                b.tangent1 += inDir.Convert(new Vector2(-offset, offsetY));
                b.tangent2 += outDir.Convert(new Vector2(offset, offsetY));
                b.quality = Bezier.Quality.Low;
				b.generatedBezierPoints = Handles.MakeBezierPoints(b.point1, b.point2, b.tangent1, b.tangent2, (int)b.quality * 2);
				return b;
            }
            private void DrawBezier(in Bezier bezier, Color color, int width = 2) {
                Handles.DrawBezier(bezier.point1, bezier.point2, bezier.tangent1, bezier.tangent2, color, null, width);
            }
            public void DrawBezier(Vector3 input, in Axis inDir, Vector3 output, in Axis outDir) {
                DrawBezier(GetBezier(input, inDir, output, outDir), Color.green);
            }
            public void DrawHihlightedBezier(Vector3 input, in Axis inDir, Vector3 output, in Axis outDir) {
                DrawBezier(GetBezier(input, inDir, output, outDir), Color.white, 10);
            }
            public ConnectionBezier GetConnectionBezier(Vector3 input, in Axis inDir, Vector3 output, in Axis outDir, Color color, int index = default) {
                ConnectionBezier cb;
				cb.color = color;
                cb.connection = GetBezier(input, inDir, output, outDir);
                Vector3 mpo = cb.connection.tangent1 * 0.3f;
                Vector3 mpi = cb.connection.point1 * 0.2f + cb.connection.tangent2 * 0.5f;
                cb.deleteButton.position = mpo + mpi;
				//cb.deleteButton.position.y += index / cb.deleteButton.position.magnitude;
				cb.deleteButton.radius = _logicProperties.slotRadiusRate * ElementRadius(cb.deleteButton.position);
                cb.index = index;
                return cb;
            }
            public void DrawConection(in ConnectionBezier connection, bool deleteMode) {
				if (deleteMode) {
					DrawBezier(connection.connection, Color.red, 10);
				} else {
					DrawBezier(connection.connection, connection.color);
				}

				float r = connection.deleteButton.radius;
                float cornerD = r * Mathf.Sqrt(0.5f);
                var offset = new Vector3(cornerD, cornerD);
                var offset2 = new Vector3(cornerD, -cornerD);
                var pos = connection.deleteButton.position;

                var lpos = pos;
                lpos.x += r * 4;
                DrawLabel(lpos, connection.index.ToString(), deleteMode ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.3f));

                Handles.color = Color.red;
                Handles.DrawSolidDisc(pos, Vector3.forward, r);
                Handles.color = Color.black;
                Handles.DrawLine(pos + offset, pos - offset);
                Handles.DrawLine(pos + offset2, pos - offset2);

            }
            public void DrawLabel(Vector3 position, string label,Color bgColor, Vector2 guiOffset = default) {
                var t = Event.current.type;
                if (t != EventType.Layout && t != EventType.Repaint) {
                    return;
                }
                float guiMargin = 3;

                var guiPosition = HandleUtility.WorldToGUIPoint(position);
                var size = GUIStyle.none.CalcSize(new GUIContent(label));
                size.x += guiMargin * 2;
                size.y += guiMargin * 2;
                Vector3 startCorner = guiPosition;
                startCorner.x -= size.x;
                startCorner.y -= size.y;
                var guiRect = new Rect(startCorner, size);
                Handles.BeginGUI();
                guiRect.position += guiOffset;
                EditorGUI.DrawRect(guiRect, bgColor);
                // guiRect.position += new Vector2(guiMargin, guiMargin);
                using (Scopes.GUIColor(Color.black)) {
                    GUI.Label(guiRect, label);
                }
                Handles.EndGUI();
                //Handles.BeginGUI(guiRect);
                //Handles.EndGUI();
                //point.x += hsize.x * guiMargin / size.x;
                //point.y += hsize.y * guiMargin / size.y;
                //Handles.Label(point, name);
            }
            public void DrawSlotLabel(Vector3 position, string label) {
                DrawLabel(position, label, Color.cyan);
               
            }
            public void DrawSlotLabelOld(Vector3 position, string name) {
                float guiMargin = 3;

                var guiPosition = HandleUtility.WorldToGUIPoint(Handles.matrix.inverse.MultiplyPoint3x4(position));
                var size = GUIStyle.none.CalcSize(new GUIContent(name));
                size.x += guiMargin * 2;
                size.y += guiMargin * 2;
                Vector3 startCorner = guiPosition;
                startCorner.x -= size.x;
                startCorner.y -= size.y;
                var c = HandleUtility.GUIPointToWorldRay(startCorner);
                var point = c.origin + c.direction * 10;
                var hsize = position - point;
                Handles.DrawSolidRectangleWithOutline(new Rect(point, hsize), Color.cyan, Color.cyan);
                point.x += hsize.x * guiMargin / size.x;
                point.y += hsize.y * guiMargin / size.y;
                Handles.Label(point, name);
            }
        }
    }
}