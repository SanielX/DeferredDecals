using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static UnityEditor.EditorGUILayout;
using HG.DeferredDecals;

[CustomEditor(typeof(Decal))]
public class DecalEditor : Editor
{
    SerializedProperty matProperty;
    SerializedProperty layerProperty;
    MaterialEditor editor;

    private void OnEnable()
    {
        matProperty = serializedObject.FindProperty("m_Material");
        layerProperty = serializedObject.FindProperty("m_Layer");
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        PropertyField(layerProperty);
        PropertyField(matProperty);

        if (EditorGUI.EndChangeCheck() || (matProperty != null && editor == null))
        {
            if (matProperty.objectReferenceValue != null)
                editor = (MaterialEditor)CreateEditor(matProperty.objectReferenceValue);
            else
            {
                editor.OnDisable();
                editor = null;
            }
        }

        if (editor)
        {
            editor.DrawHeader();
            editor.OnInspectorGUI();

            editor.serializedObject.ApplyModifiedProperties();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
