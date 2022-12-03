using UnityEngine;
using UnityEditor;

namespace Mobge.Platformer.Character {
    [CustomEditor(typeof(AreaBasedAI))]
    [CanEditMultipleObjects]
    public class AreaBasedAIEditor : Editor
    {
        private const Tool c_toolId = (Tool)9273;
        private Tool _oldTool;
        private AreaBasedAI _go;
        private EditorTools _sceneHelper;
        private LayoutRectSource _tempRects;
        private bool _editMode;
        private int _selectedRule = -1;
        private EditorSelectionQueue<int> _selectionQueue = new EditorSelectionQueue<int>();
        protected void OnEnable() {
            _go = target as AreaBasedAI;
            EditEnabled = false;
            _tempRects = new LayoutRectSource();
        }
        private bool EditEnabled{
            get{
                return Tools.current == c_toolId;
            }
            set{
                var val = EditEnabled;
                if(val!=value){
                    if(value) {
                        _oldTool = Tools.current;
                        Tools.current = c_toolId;
                    }
                    else{
                        Tools.current = _oldTool;
                    }
                }
            }
        }
        private bool AnalyzeMode {
            get {
                return EditorApplication.isPlaying;
            }
        }
        private void CreateData(Character2D character) {
            _go.data = EditorExtensions.CreateAssetForObject<AreaBasedAIData>("select path for " + typeof(AreaBasedAIData), "message", character, "_ai", _go.data);
        }
        public override void OnInspectorGUI() {
            if(!_go) return;
            
            var character = _go.GetComponentInParent<Character2D>();
            if(character == null || character.gameObject == _go.gameObject) {
                var c = GUI.contentColor;
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField(typeof(Character2D) + " component required as parent.");
                GUI.contentColor = c;
                return;
            }
            base.OnInspectorGUI();
            if(!_go.data) {
                if(GUILayout.Button("create data")) {
                    CreateData(character);
                }
                else {
                    return;
                }
            }
            else {
                if(!EditorExtensions.AreFoldersTheSame(character, _go.data)){
                    var c = GUI.contentColor;
                    GUI.contentColor = Color.yellow;
                    EditorGUILayout.LabelField("Data is not in the same folder with character.");
                    if(GUILayout.Button("duplicate data")) {
                        
                        CreateData(character);
                    }
                    GUI.contentColor = c;

                }
            }
            
            EditEnabled = GUILayout.Toggle(EditEnabled, "edit");
            if(EditEnabled) {
                _tempRects.Reset(GUILayoutUtility.GetAspectRect(float.MaxValue));
                ActionGroupField(character, _tempRects, "default sequence", ref _go.data.defaultActions);
                _tempRects.ConvertToLayout();
                EditorLayoutDrawer.CustomArrayField("areas", ref _go.data.rules, (_rects, rule)=>{
                    rule.area = EditorGUI.RectField(_rects.NextRectWithLineCount(3), "area", rule.area);
                    rule.weight = EditorGUI.FloatField(_rects.NextRect(), "weight", rule.weight);
                    ActionGroupField(character, _rects, "sequence", ref rule.actions);
                    return rule;
                }, ref _selectedRule);
                _go.data.detectArea = EditorGUILayout.RectField("detect area", _go.data.detectArea);
                _go.data.releaseArea = EditorGUILayout.RectField("release area", _go.data.releaseArea);
            }
            else{
                _selectedRule = -1;
            }
            
            


            if(GUI.changed) {
                EditorExtensions.SetDirty(_go);
                if(_go.data) {
                    EditorExtensions.SetDirty(_go.data);
                }
            }
        }
        private static void ActionGroupField(Character2D character, LayoutRectSource rectSource, string label, ref AreaBasedAIData.ActionSequence actions){
            //actions.duration = EditorGUI.FloatField(rectSource.NextRect(), label + " duration", actions.duration);
            //var dur = actions.duration;
            EditorDrawer.CustomArrayField(rectSource, label, ref actions.actions, (_innerRects, action) => {
                var r = _innerRects.NextRect();
                var labelWidth = EditorGUIUtility.labelWidth;
                var r1 = r;
                var r2 = r;
                var h = (r.width-labelWidth)*0.5f;
                r1.width = h+labelWidth;
                r2.width = h;
                r2.x += h + labelWidth;
                action.type = (AreaBasedAIData.ActionType)EditorGUI.EnumPopup(r1, "type", action.type);
                switch(action.type){
                    case AreaBasedAIData.ActionType.WalkInputX:
                    action.ActionParameterI = (int)(WalkDirection)EditorGUI.EnumPopup(r2, (WalkDirection)action.ActionParameterI);
                    break;
                    case AreaBasedAIData.ActionType.ActionPress:
                    case AreaBasedAIData.ActionType.ActionRelease:
                    action.ActionParameterI = EditorDrawer.Popup(r2, null, character.States.mappings, action.ActionParameterI);
                    break;
                    case AreaBasedAIData.ActionType.WaitForAction:
                    break;
                    case AreaBasedAIData.ActionType.WaitTime:
                    action.ActionParameterF = EditorGUI.FloatField(r2, action.ActionParameterF);
                    break;
                    case AreaBasedAIData.ActionType.WaitWhileTargetInside:
                    action.ActionParameterF = EditorGUI.FloatField(r2, action.ActionParameterF);
                    break;
                }

                //EditorGUI.MinMaxSlider(_innerRects.NextRect(), "start end", ref action.start, ref action.end, 0, dur);
                return action;
            });
        }
        private enum WalkDirection{
            Front = 1,
            Back = -1,
            Stop = 0,
        }
        protected void OnSceneGUI() {
            if(!_go || !_go.data || !EditEnabled) return;
            EnsureTools();
            DrawGizmos();
            _sceneHelper.OnSceneGUI();
            if(Event.current.type == EventType.Used){
                Repaint();
            }
        }
        private void DrawGizmos() {
            Handles.matrix = _go.transform.localToWorldMatrix;
            int selectedId;
            if(AnalyzeMode) {
                selectedId = _go.CurrentRuleId;
            }
            else{
                selectedId = _selectedRule;
            }
            float maxWeight = 0;
            for(int i = 0; i < _go.data.rules.Length; i++) {
                maxWeight = Mathf.Max(maxWeight, _go.data.rules[i].weight);
            }
            for(int i = 0; i < _go.data.rules.Length; i++) {
                var rule = _go.data.rules[i];
                var s = i == selectedId;
                Color outlineColor;
                if(s) {
                    outlineColor = new Color(0.9f, 0.4f,0.4f,1f);
                    Handles.color = outlineColor;
                    if(!AnalyzeMode) {
                        rule.area = HandlesExtensions.RectHandle(rule.area);
                    }
                }
                else{
                    outlineColor = new Color(0.5f,0.5f,0.5f,1);
                }
                Handles.color = Color.white;
                Handles.DrawSolidRectangleWithOutline(rule.area, new Color(0.5f,0.5f,0.5f,rule.weight*0.5f/maxWeight),outlineColor);
            }
            
            Handles.color = Color.gray;
            Handles.DrawWireCube(_go.data.detectArea.center, _go.data.detectArea.size);
            Handles.color = Color.red;
            Handles.DrawWireCube(_go.data.releaseArea.center, _go.data.releaseArea.size);
            Handles.matrix = Matrix4x4.identity;
            Handles.color = Color.white;
        }
        private void EnsureTools() {
            if(_sceneHelper == null) {
                _sceneHelper = new EditorTools();
                _sceneHelper.AddTool(new EditorTools.Tool("select") {
                    activation = new EditorTools.ActivationRule(){
                        mouseButton = 0
                    },
                    onPress = SelectRectangle,
                });
            }
        }
        private Vector3 MousePos{
            get {
                var ray = _sceneHelper.MouseRay;
                var o = _go.transform.InverseTransformPoint(ray.origin);
                var d = _go.transform.InverseTransformVector(ray.direction);
                var pos = o + d * (o.z / -d.z);
                return pos;
            }
        }
        private bool SelectRectangle() {
            var mpos = MousePos;
            for(int i = 0; i < _go.data.rules.Length; i++){
                var r = _go.data.rules[i];
                if(r.area.Contains(mpos)){
                    _selectionQueue.AddCandidate(i);
                }
            }
            var b = _selectionQueue.SelectOne(out _selectedRule);
            return b;
        }
    }
}
