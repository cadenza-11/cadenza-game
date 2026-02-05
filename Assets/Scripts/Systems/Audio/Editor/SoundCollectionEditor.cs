using UnityEditor;
using UnityEngine;
using Cadenza;

[CustomEditor(typeof(SoundCollection))]
public class SoundCollectionEditor : Editor
{
    SerializedProperty uiEvents;
    SerializedProperty gameplayEvents;

    void OnEnable()
    {
        this.uiEvents = this.serializedObject.FindProperty("uiEvents");
        this.gameplayEvents = this.serializedObject.FindProperty("gameplayEvents");

        this.EnsureSize<Sound.UI>(this.uiEvents);
        this.EnsureSize<Sound.Gameplay>(this.gameplayEvents);

        this.serializedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI()
    {
        this.serializedObject.Update();

        this.DrawEnumArray<Sound.UI>("UI Sounds", this.uiEvents);
        EditorGUILayout.Space(8);
        this.DrawEnumArray<Sound.Gameplay>("Gameplay Sounds", this.gameplayEvents);

        this.serializedObject.ApplyModifiedProperties();
    }

    private void EnsureSize<TEnum>(SerializedProperty array)
        where TEnum : System.Enum
    {
        int enumCount = System.Enum.GetValues(typeof(TEnum)).Length;

        if (array.arraySize != enumCount)
        {
            int oldSize = array.arraySize;
            array.arraySize = enumCount;

            // Preserve existing elements (Unity usually does this, but we’re explicit)
            for (int i = oldSize; i < enumCount; i++)
            {
                array.GetArrayElementAtIndex(i).Reset();
            }
        }
    }

    private void DrawEnumArray<TEnum>(string header, SerializedProperty array)
        where TEnum : System.Enum
    {
        EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        string[] names = System.Enum.GetNames(typeof(TEnum));

        for (int i = 0; i < names.Length; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(element, new GUIContent(names[i]));
        }

        EditorGUI.indentLevel--;
    }
}
