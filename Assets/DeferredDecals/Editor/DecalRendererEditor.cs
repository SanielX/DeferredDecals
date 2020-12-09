using HG.DeferredDecals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace HostGame.Editors
{
    [CustomEditor(typeof(DeferredDecalRenderer))]
    public class DecalRendererEditor : UnityEditor.Editor
    {
        DeferredDecalRenderer myTarget;

        private void OnEnable()
        {
            myTarget = (DeferredDecalRenderer)target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Rendered Decals Count", myTarget.renderedDecals);
            EditorGUI.EndDisabledGroup();
        }
    }
}
