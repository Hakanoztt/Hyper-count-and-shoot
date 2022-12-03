using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mobge.Core.Components
{
    public class Movement3DDrawer {
        private const float c_sampleCount = 40;

        private static AnimationWindow _window;


        public System.Action<Pose, float> onUpdateFromWindowExtra;
        public System.Action<Movement3D> onDataChange;
        public Pose originPose => _originPose;

        private bool _isWindowOpened = false;
        private Movement3D _movement;
        private float _time, _startTime;
        private Pose _originPose;
        private bool _isPreviewRequested, _isPreviewStarted;
        private Pose _currentPose = Pose.identity;
        private bool _isVisualDirty = false;

        private EditorTools _editorTools;

        public Movement3DDrawer()
        {
            if (_window == null) {
                _window = new AnimationWindow();
            }
            _editorTools = new EditorTools();
            _editorTools.AddTool(new EditorTools.Tool("open options")
            {
                activation = new EditorTools.ActivationRule()
                {
                    mouseButton = 1,
                },
                onRelease = OpenPopup,
            });
            _startTime = CurrentTime;
        }
        private float CurrentTime {
            get=> (float)EditorApplication.timeSinceStartup;
        }
        private void OpenPopup() {
            _isWindowOpened = true;
            _window.UpdatePosition();
            _window.position.size = new Vector2(_window.position.size.x, 465);
            _currentPose = WindowPose;

        }

        private Pose WindowPose {
            get {
                float time = _window.Time;
                Pose pc;
                pc.position = _movement.positionData.Evaluate(time);
                pc.rotation = Quaternion.Euler(_movement.rotationData.Evaluate(time));
                
                return pc;
            }
        }
        public bool OnSceneGUI(Pose parentPose, Movement3D movement) {
            _originPose = parentPose;
            _movement = movement;
            EnsureMovementCurveData();
            bool edited = false;
            // to do: _moement.EnsureData
            if (_isWindowOpened) {
                AnimationWindow.Curve[] curves;
                curves = new AnimationWindow.Curve[] {
                    new AnimationWindow.Curve(_movement.positionData.curves[0].ToAnimationCurve(), "x"),
                    new AnimationWindow.Curve(_movement.positionData.curves[1].ToAnimationCurve(), "y"),
                    new AnimationWindow.Curve(_movement.positionData.curves[2].ToAnimationCurve(), "z"),

                    new AnimationWindow.Curve(_movement.rotationData.curves[0].ToAnimationCurve(), "euler x"),
                    new AnimationWindow.Curve(_movement.rotationData.curves[1].ToAnimationCurve(), "euler y"),
                    new AnimationWindow.Curve(_movement.rotationData.curves[2].ToAnimationCurve(), "euler z"),
                };
                GUI.color = _isPreviewStarted ? Color.cyan : Color.white;
                _window.OnGUI(curves, UpdateData, UpdateVisualsFromWindow, GetCurrentValue, ref _time);
                _movement.totalTime = _time;
                edited = PoseHandle(curves, parentPose);
                if (edited || _isVisualDirty) {
                    _isVisualDirty = false;
                    UpdateTargetVisuals();
                }
                if (_isPreviewStarted) {
                    PreviewUpdate();
                    SceneView.RepaintAll();
                }
            }
            _editorTools.OnSceneGUI();
            return edited;
        }
        private bool PoseHandle(AnimationWindow.Curve[] curves, Pose parent) {
            bool updated = false;

            //elementPose = elementPose.GetTransformedBy(parent);
            var newPose = _currentPose.GetTransformedBy(parent);
            var oldPose = newPose;
            Handles.TransformHandle(ref newPose.position, ref newPose.rotation);
            if (oldPose != newPose) {
                newPose = newPose.GetTransformedBy(new Pose(-parent.position, Quaternion.Inverse(parent.rotation)));
                _currentPose = newPose;
                updated = true;
            }
            //_movement3DDrawer.UpdatePose(elementPose.position, elementPose.rotation);
            return updated;
        }
        private void EnsureMovementCurveData()
        {
            if (!_movement.positionData.isReady)
            {
                _movement.EditorInit(Vector3.zero, Vector3.zero, Vector3.one);
            }
        }
        private float GetCurrentValue(string label)
        {
            switch (label)
            {
                default:
                case "x":
                    return _currentPose.position.x;
                case "y":
                    return _currentPose.position.y;
                case "z":
                    return _currentPose.position.z;
                case "euler x":
                    return _currentPose.rotation.eulerAngles.x;
                case "euler y":
                    return _currentPose.rotation.eulerAngles.y;
                case "euler z":
                    return _currentPose.rotation.eulerAngles.z;
            }
        }
        private void UpdateData(AnimationWindow.Curve[] curves, float time)
        {
            _time = time;

            UpdateData(curves);
        }
        private void UpdateData(AnimationWindow.Curve[] curves)
        {

            _movement.positionData.curves[0].UpdateKeys(curves[0].curve);
            _movement.positionData.curves[1].UpdateKeys(curves[1].curve);
            _movement.positionData.curves[2].UpdateKeys(curves[2].curve);

            _movement.rotationData.curves[0].UpdateKeys(curves[3].curve);
            _movement.rotationData.curves[1].UpdateKeys(curves[4].curve);
            _movement.rotationData.curves[2].UpdateKeys(curves[5].curve);

            if (this.onDataChange != null) {
                onDataChange(_movement);
            }

        }
        private void PreviewUpdate()
        {
            UpdateVisualsFromWindow();
        }
        void UpdateTargetVisuals() {

            onUpdateFromWindowExtra?.Invoke(_currentPose.GetTransformedBy(_originPose), _window.Time);
        }
        public void UpdateVisualsFromWindow() {
            _currentPose = WindowPose;
            _isVisualDirty = true;
            
        }
        
    }
}