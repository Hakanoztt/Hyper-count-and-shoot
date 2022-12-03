//using System;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core
{
    [CustomPropertyDrawer(typeof(EnumFlagAttribute))]
    public class EnumFlagsEditor : PropertyDrawer
    {
        // doc : https://docs.unity3d.com/ScriptReference/PropertyDrawer.GetPropertyHeight.html
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            EnumFlagAttribute f = attribute as EnumFlagAttribute;
            int length = Mathf.CeilToInt(property.enumNames.Length * 1.0f / f.i_Column);
            return (length * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing));
        }

        // doc : https://docs.unity3d.com/ScriptReference/PropertyDrawer.OnGUI.html
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnumFlagAttribute f = attribute as EnumFlagAttribute;
            int bValue = 0;
            bool[] isBPressed = new bool[property.enumNames.Length];
            CalculateButtonConstants(ref position, ref f, ref property, out Vector2 button);
            EditorGUI.LabelField(new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height), label);

            EditorGUI.BeginChangeCheck();
            {
                for (int i = 0; i < property.enumNames.Length; i++)
                {
                    if ((property.intValue & (1 << i)) == 1 << i)
                    {
                        isBPressed[i] = true;
                    }
                    float i_row = Mathf.Floor(i / f.i_Column);
                    Rect buttonPos = new Rect(position.x + EditorGUIUtility.labelWidth + button.x * (i % f.i_Column),
                                              position.y + (i_row * button.y), 
                                              button.x, 
                                              button.y);
                    isBPressed[i] = GUI.Toggle(buttonPos, isBPressed[i], property.enumNames[i], "Button");
                    if (isBPressed[i])
                    {
                        bValue += 1 << i;
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = bValue;
            }
        }

        private void CalculateButtonConstants(ref Rect p, ref EnumFlagAttribute f, ref SerializedProperty sp, out Vector2 button)
        {
            button.x = (p.width - EditorGUIUtility.labelWidth) / f.i_Column;
            button.y = (p.height / Mathf.Ceil(sp.enumNames.Length * 1.0f / f.i_Column));
            //Debug.Log("Button width:" + button.x + " Button height:" + button.y + "     p.width:" + p.width + " p.height:" + p.height );
            //Debug.Log(EditorGUIUtility.currentViewWidth);
            //Debug.Log(EditorGUIUtility.fieldWidth);
            
        }
    }
}