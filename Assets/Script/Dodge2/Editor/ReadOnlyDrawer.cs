using UnityEditor;
using UnityEngine;

// =============================================================================
// ReadOnlyDrawer.cs  (Editor 전용 — 빌드에 안 들어감)
// -----------------------------------------------------------------------------
// [ReadOnly] 가 붙은 필드를 인스펙터에서 회색(편집 불가)으로 그린다.
// =============================================================================
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool prevEnabled = GUI.enabled;
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = prevEnabled;
    }
}
