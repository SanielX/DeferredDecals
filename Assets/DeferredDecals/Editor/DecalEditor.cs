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
    SerializedProperty featuresProperty;
    MaterialEditor editor;

    private void OnEnable()
    {
        matProperty = serializedObject.FindProperty("m_Material");
        layerProperty = serializedObject.FindProperty("m_Layer");
        featuresProperty = serializedObject.FindProperty("m_FeatureSet");
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        PropertyField(layerProperty);
        PropertyField(matProperty);

       // featuresProperty.intValue = MaskField(new GUIContent(featuresProperty.displayName), featuresProperty.intValue, featuresProperty.enumDisplayNames);

        if (EditorGUI.EndChangeCheck() || (matProperty.objectReferenceValue != null && editor == null))
        {
            if (matProperty.objectReferenceValue != null)
                editor = (MaterialEditor)CreateEditor(matProperty.objectReferenceValue);
            else
            {
                editor?.OnDisable();
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
