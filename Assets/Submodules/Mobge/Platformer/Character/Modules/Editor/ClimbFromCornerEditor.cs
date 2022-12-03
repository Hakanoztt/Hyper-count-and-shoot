using UnityEditor;
using UnityEngine;

namespace Mobge.Platformer.Character{
    [CustomEditor(typeof(ClimbFromCorner))]
    public class ClimbFromCornerEditor : Editor{
        private ClimbFromCorner _go;
        private Character2D _clone;
        private int _selectedRow = -1;
        protected void OnEnable() {
            _go = target as ClimbFromCorner;
            
        }
        public Character2D EnsureClone() {
            if(!_clone) { 
                if(_go) {
                    var character = _go.GetComponentInParent<Character2D>();
                    if(character){
                        _clone = Instantiate(character, character.transform.position, character.transform.rotation, character.transform.parent);
                    }
                }
            }
            return _clone;
        }
        void DestroyClone(){
            if(_clone){
                DestroyImmediate(_clone.gameObject);
            }
        }
        protected void OnDisable() {
            DestroyClone();
        }

        public override void OnInspectorGUI(){
            if(!_go) return;
            var character = _go.GetComponentInParent<Character2D>();

            EditorLayoutDrawer.CustomArrayField("setups", ref _go.setups, (rectSource, t) => {
                t.climbedOffset = EditorGUI.Vector2Field(rectSource.NextRect(), "climb offset", t.climbedOffset);
                t.maxHeight = EditorGUI.FloatField(rectSource.NextRect(), "max height", t.maxHeight);
                t.duration = EditorGUI.FloatField(rectSource.NextRect(), "duration", t.duration);
                t.climbAnimation = EditorDrawer.Popup(rectSource, "animation", character.Animation.AnimationList, t.climbAnimation);
                return t;
            }, ref _selectedRow);

            
            if(_selectedRow < 0) {
                DestroyClone();
            }
            else {
                if(EnsureClone()) {
                    var setup = _go.setups[_selectedRow];
                    _clone.gameObject.SetActive(true);
                    _clone.transform.position = character.Position + (Vector3)(-setup.climbedOffset);
                    Mobge.Animation.AnimationState _climbAnim;
                    _climbAnim = _clone.Animation.GetCurrent(0);
                    if(_climbAnim == null || _climbAnim.AnimationId != setup.climbAnimation){
                        _climbAnim =_clone.Animation.PlayAnimation(0, setup.climbAnimation, false);
                        _climbAnim.Speed = 0;
                    }
                    _climbAnim.Time = EditorGUILayout.Slider(_climbAnim.Time, 0, _climbAnim.Duration);
                }
            }
            if(GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }
        protected void OnSceneGUI() {
            if(!_go) return;
            if(_selectedRow >= 0){
                var setup = _go.setups[_selectedRow];
                var character = _go.GetComponentInParent<Character2D>();
                var pos = character.Position;
                Handles.DrawLine(new Vector3(pos.x-setup.climbedOffset.x, pos.y + setup.maxHeight, pos.z), new Vector3(pos.x+setup.climbedOffset.x, pos.y + setup.maxHeight, pos.z));
                //Handles.DrawLine(new Vector3(pos.x-setup.climbedOffset.x, pos.y-setup.maxHeight, pos.z), new Vector3(pos.x+setup.climbedOffset.x, pos.y-setup.maxHeight, pos.z));
            }
        }
    }
}