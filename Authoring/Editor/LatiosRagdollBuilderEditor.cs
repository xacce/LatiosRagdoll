using System.Collections.Generic;
using LatiosKinematicAnnotation.Authoring;
using Src.GameReady.DotsRag.Authoring;
using UnityEditor;
using UnityEngine;

namespace LatiosRagdoll.Authoring.Editor
{
    [CustomEditor(typeof(LatiosRagdolledAuthoring))]
    public class LatiosRagdollBuilderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var trg = target as LatiosRagdolledAuthoring;
            if (GUILayout.Button("Create ragdoll dummy for annotated"))
            {
                var editorBones = trg.GetComponent<LatiosEditorBones>();
                var go = new GameObject($"DummyRagdoll_{trg.gameObject.name}");
                var ragdolledBonesGo = go.AddComponent<LatiosRagdollAvatarAuthoring>();
                var ragdolledBones = new List<AnnotatedGameObject>();
                go.transform.SetPositionAndRotation(trg.transform.position, trg.transform.rotation);
                for (int i = 0; i < trg.annotations_s.annotates.Length; i++)
                {
                    var annotated = trg.annotations_s.annotates[i];
                    if (editorBones.TryGetBoneByPath(annotated.path, out var boneTransform))
                    {
                        var boneGo = new GameObject($"Bone {annotated.annotation.name}");
                        var annotatedGo = boneGo.AddComponent<AnnotatedGameObject>();
                        annotatedGo.annotation_s = annotated.annotation;
                        boneGo.transform.parent = go.transform;
                        boneGo.transform.localPosition = boneTransform.position;
                        boneGo.transform.localRotation = boneTransform.orientation;
                        ragdolledBones.Add(annotatedGo);
                        EditorUtility.SetDirty(boneGo);
                    }
                }

                ragdolledBonesGo.bones = ragdolledBones.ToArray();

                EditorUtility.SetDirty(ragdolledBonesGo);
            }

            base.OnInspectorGUI();
        }
    }
}