using System.Text;
using UnityEditor;
using UnityEngine;

namespace Mobge.Platformer
{
    [CustomEditor(typeof(GameSetup),true)]
    public class GameSetupEditor : Editor
    {
        private GameSetup _go;
        private StringBuilder _sb = new StringBuilder();
        private bool _isTeamsOpen;
        private bool _dirty;
        protected void OnEnable()
        {
            _go = target as GameSetup;
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (_go == null) return;
            _go.EnsureSetup();

            EditorLayoutDrawer.CustomListField<GameSetup.Team>("Teams", _go.Teams, (rectOffset, key) => {
                var t = _go.Teams[key];
                t.name = EditorGUI.TextField(rectOffset.NextRect(), key + " :name", t.name);
                if (GUI.Button(rectOffset.NextRect(), "edit enemies"))
                {
                    var p = new EditorPopup((rects, popup) => {
                        EditorGUI.LabelField(rects.NextRect(), "enemies");
                        var e = _go.Teams.GetKeyEnumerator();
                        while (e.MoveNext())
                        {
                            var team = e.Current;
                            var enemy = _go.Teams.IsEnemy(team, key);
                            EditorGUI.BeginChangeCheck();
                            enemy = EditorGUI.Toggle(rects.NextRect(), _go.Teams[team].name, enemy);
                            if (EditorGUI.EndChangeCheck())
                            {
                                _go.Teams.SetRelation(team, key, enemy);
                            }
                        }
                        if (GUI.changed)
                        {
                            _dirty = true;
                        }
                    });
                    p.Show(rectOffset.LastRect());
                }
                _go.Teams[key] = t;
            }, ref _isTeamsOpen);

            if (GUI.changed || _dirty)
            {
                _dirty = false;
                EditorExtensions.SetDirty(_go);
            }
        }
        public class BaseAttributeDrawer : PropertyDrawer
        {
            public GameSetup FindSetup(UnityEngine.Object @object)
            {
                var setup = @object as GameSetup;
                if (setup == null)
                {
                    var owner = @object as IGameComponent;
                    if (owner == null)
                    {
                        var c = @object as Component;
                        if (c != null)
                        {
                            owner = c.GetComponentInParent<IGameComponent>();
                        }
                    }
                    if (owner != null)
                    {
                        setup = owner.GameSetup;
                    }
                }
                return setup;
            }
        }
    }
    [CustomPropertyDrawer(typeof(TeamAttribute))]
    public class TeamAttributeDrawer : GameSetupEditor.BaseAttributeDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var setup = FindSetup(property.serializedObject.targetObject);
            EditorGUI.BeginChangeCheck();
            if (setup)
            {

                property.intValue = EditorDrawer.Popup(position, label.text, setup.Teams, property.intValue);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}