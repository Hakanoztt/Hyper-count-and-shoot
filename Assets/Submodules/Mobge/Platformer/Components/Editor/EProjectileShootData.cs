using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.Platformer {
    [CustomPropertyDrawer(typeof(ProjectileShootData))]
    public class EProjectileShootData : PropertyDrawer
    {
        private static LayoutRectSource _layoutRects;
        protected void OnEnable() {

        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if(_layoutRects == null) {
                _layoutRects = new LayoutRectSource();
            }
             _layoutRects.Reset(position);
             EditorGUI.PropertyField(_layoutRects.NextRect(), property);
             if(property.isExpanded) {
                EditorGUI.indentLevel++;
                var speed = property.FindPropertyRelative(nameof(ProjectileShootData.speed));
                var angle = property.FindPropertyRelative(nameof(ProjectileShootData.angle));
                var reduceRate = property.FindPropertyRelative(nameof(ProjectileShootData.reduceRate));
                var aimMode = property.FindPropertyRelative(nameof(ProjectileShootData.aimMode));
                EditorGUI.PropertyField(_layoutRects.NextRect(), speed);
                EditorGUI.PropertyField(_layoutRects.NextRect(), angle);
                switch((ProjectileShootData.AimMode)aimMode.intValue) {
                    case ProjectileShootData.AimMode.FixedValues:
                    break;
                    default:
                    case ProjectileShootData.AimMode.ReduceAngleChooseFaster:
                    case ProjectileShootData.AimMode.ReduceAngleChooseSlower:
                    reduceRate.floatValue = EditorGUI.FloatField(_layoutRects.NextRect(), "angle reduce rate", reduceRate.floatValue);
                    break;
                    case ProjectileShootData.AimMode.ReduceSpeed:
                    reduceRate.floatValue = EditorGUI.FloatField(_layoutRects.NextRect(), "speed reduce rate", reduceRate.floatValue);
                    reduceRate.floatValue = Mathf.Max(reduceRate.floatValue, 0);
                    break;
                    case ProjectileShootData.AimMode.RelativeHeight:
                    reduceRate.floatValue = EditorGUI.FloatField(_layoutRects.NextRect(), "extra height", reduceRate.floatValue);
                    break;
                }
                EditorGUI.PropertyField(_layoutRects.NextRect(), aimMode);
                EditorGUI.indentLevel--;
             }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            OnGUI(new Rect(-10000, -10000, 10, 10), property, label);
            return _layoutRects.Height;
        }
        public static void PreviewGUI(Rect position, ProjectileShootData data, float gravity, ref float virtualWidth) {
            var r = position;
            var lCenter = new Vector2(r.x, r.center.y);
            var rCenter = new Vector2(r.xMax, r.center.y);
            var center = r.center;
            // calculate matrix
            var scl = r.size.x / virtualWidth;
            var defMat = Handles.matrix;
            var mat = Matrix4x4.TRS(new Vector2(r.x, center.y), Quaternion.identity, new Vector3(scl,-scl,scl));
            // draw content
            Handles.matrix = mat;
            Vector3 target = Vector3.zero;
            EProjectileShootData.SceneGUI(data,null, Vector3.zero, gravity, ref target);
            Handles.matrix = defMat;
             // draw zoom editor
            Handles.color = Color.white;
            Handles.DrawLine(lCenter, rCenter);
            float edgeHeight = EditorGUIUtility.singleLineHeight;
            Handles.DrawLine(new Vector2(lCenter.x, lCenter.y + edgeHeight), new Vector2(lCenter.x, lCenter.y - edgeHeight));
            Handles.DrawLine(new Vector2(rCenter.x, rCenter.y + edgeHeight), new Vector2(rCenter.x, rCenter.y - edgeHeight));
            Handles.DrawLine(new Vector2(center.x, center.y + edgeHeight), new Vector2(center.x, center.y - edgeHeight));
            // draw zoom settings
            var lw = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth =  30;
            Vector2 settingSize = new Vector2(60, EditorGUIUtility.singleLineHeight);
            Rect settingsRect = new Rect(center - settingSize*0.5f, settingSize);
            EditorGUI.DrawRect(settingsRect, new Color(0,0,0,0.7f));
            virtualWidth = EditorGUI.FloatField(settingsRect, "<->", virtualWidth);
            EditorGUIUtility.labelWidth = lw;
        }
        public static void SceneGUI(ProjectileShootData data ,Transform root, Vector3 localOffset, float gravity, ref Vector3 target) {
            //Gizmos.matrix = root.localToWorldMatrix;
            if(root){
                localOffset = root.TransformPoint(localOffset);
                target += root.position;
                target = UnityEditor.Handles.PositionHandle(target, Quaternion.identity);
            }

            UnityEditor.Handles.color = new Color(1.0f,1.0f,0.5f,1.0f);
            int lineCount = 10;
            float rad = data.angle * Mathf.Deg2Rad;
            switch(data.aimMode) {
                case ProjectileShootData.AimMode.ReduceAngleChooseFaster:
                case ProjectileShootData.AimMode.ReduceAngleChooseSlower:
                for(int i = 0; i <= lineCount; i++) {
                    var a = rad * (data.reduceRate+(1-data.reduceRate)*(lineCount-i)/(float)lineCount);
                    var cos = Mathf.Cos(a);
                    var sin = Mathf.Sin(a);
                    var dir = new Vector2(cos, sin);
                    DrawProjectile(localOffset, new Vector3(0, gravity), dir*data.speed, 5.0f, 0.1f);
                }
                UnityEditor.Handles.color = new Color(0.5f,0.5f,1.0f,1.0f);
                {
                    var a = rad * (data.reduceRate+(1-data.reduceRate )* 0.65f);
                    var cos = Mathf.Cos(a);
                    var sin = Mathf.Sin(a);
                    var dir = new Vector2(cos, sin);
                    DrawProjectile(localOffset, new Vector3(0, gravity), dir*data.speed, 5.0f, 0.1f);
                }
                break;
                case ProjectileShootData.AimMode.ReduceSpeed:
                {
                    var cos = Mathf.Cos(rad);
                    var sin = Mathf.Sin(rad);
                    var dir = new Vector2(cos, sin);
                    for(int i = 0; i <= lineCount; i++) {
                        var vel = data.speed * (data.reduceRate+(1-data.reduceRate)*(lineCount-i)/(float)lineCount);
                        DrawProjectile(localOffset, new Vector3(0, gravity), dir*vel, 5.0f, 0.1f);
                    }
                    UnityEditor.Handles.color = new Color(0.5f,0.5f,1.0f,1.0f);
                    //DrawProjectile(localOffset, new Vector3(0, gravity), dir * data.speed * (data.reduceRate+(1-data.reduceRate )* 0.65f), 5.0f, 0.1f);
                    break;
                }
                case ProjectileShootData.AimMode.FixedValues:{
                    var cos = Mathf.Cos(rad);
                    var sin = Mathf.Sin(rad);
                    var dir = new Vector2(cos, sin);
                    DrawProjectile(localOffset, new Vector3(0 ,gravity), dir*data.speed, 5.0f, 0.1f);
                }
                break;
                case ProjectileShootData.AimMode.RelativeHeight: {
                    
                    for(int i = 0; i <= lineCount; i++) {
                        float a = Mathf.LerpUnclamped(90, data.angle, i/(float)lineCount) * Mathf.Deg2Rad;
                        var cos = Mathf.Cos(a);
                        var sin = Mathf.Sin(a);
                        var dir = new Vector2(cos, sin);
                        DrawProjectile(localOffset, new Vector3(0,gravity), data.speed*dir,5.0f, 0.1f);
                    }
                }
                break;
            }
            Vector3 velocity;
            var b = data.TryCalculatingVelocity(out velocity, gravity, target - localOffset, false, true);
            //Debug.Log(b);
            if(b) {
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(target, "" + (velocity.magnitude));
                UnityEditor.Handles.color = new Color(0.5f,1.0f,1.0f,1.0f);
                DrawProjectile(localOffset, new Vector3(0, gravity), velocity, 5.0f, 0.1f);
                
            }
            UnityEditor.Handles.color = Color.white;
            if(root) {
                target -= root.position;
            }
            //Gizmos.matrix = Matrix4x4.identity;
        }
        public static void DrawProjectile(Vector3 pos, Vector3 gravity, Vector3 startVelocity, float totalTime, float timeStep) {
            Vector3 currentPos = pos;
            for(float a = timeStep; a <= totalTime; a += timeStep) {
                var newPos = pos + startVelocity*a+0.5f*a*a*gravity;
                UnityEditor.Handles.DrawLine(currentPos, newPos);
                currentPos = newPos;
            }
        }

    }
}