#if UNITY_EDITOR
using LatiosKinematicAnnotation.Authoring;
using LatiosKinematicAnnotation.Authoring.So;
using Src.GameReady.DotsRag.Authoring;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace LatiosRagdoll.Authoring
{
    [RequireComponent(typeof(LatiosEditorBones))]
    public class LatiosRagdolledAuthoring : MonoBehaviour
    {
        [SerializeField] private bool addRagdolledBonesToLinkedGroup = true;
        [SerializeField] private bool atStart;
        [SerializeField] public LatiosPathsAnnotations annotations_s;
        [SerializeField] public LatiosRagdollAvatarAuthoring ragdolledAvatarPrefab;

        private class LatiosRagdolledBaker : Baker<LatiosRagdolledAuthoring>
        {
            public override void Bake(LatiosRagdolledAuthoring authoring)
            {
                if (authoring.annotations_s == null || authoring.ragdolledAvatarPrefab == null) return;
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new LatiosRagdolledPrefab()
                {
                    physicsStructurePrefab = GetEntity(authoring.ragdolledAvatarPrefab, TransformUsageFlags.None)
                });
                var ragdolled = authoring.ragdolledAvatarPrefab.GetComponentsInChildren<AnnotatedGameObject>();
                var avatarBoneMap = GetComponent<LatiosEditorBones>().bones;
                BlobBuilder builder = new BlobBuilder(Allocator.Temp);
                ref var definition = ref builder.ConstructRoot<LatiosRagdolledAvatarBindings.Blob>();

                int[] indexes = new int[ragdolled.Length];
                for (int i = 0; i < ragdolled.Length; i++)
                {
                    var rag = ragdolled[i];
                    indexes[i] = -1;
                    if (rag.annotation_s == null)
                    {
                        Debug.LogError($"Specify annotation for {rag}", rag.gameObject);
                        continue;
                    }

                    if (authoring.annotations_s.TryGetPathAnnotated(rag.annotation_s, out var path))
                    {
                        // var validPath = $"{String.Join("/", path.Split('/').Reverse())}/"; //todo its funny, but not good  bruh
                        bool found = false;
                        foreach (var _bone in avatarBoneMap)
                        {
                            if (_bone.path == path)
                            {
                                indexes[i] = _bone.boneIndex;
                                found = true;
                                break;
                            }
                        }

                        if (!found) Debug.LogError($"Cant found boneIndex for annotated bone {rag.annotation_s.name} via path {path}");
                    }
                    else
                    {
                        Debug.LogError($"Cant find binding path for {rag.annotation_s} inside {authoring.annotations_s} file");
                    }
                }

                var indexesBaked = builder.Allocate(ref definition.boneIndexes, indexes.Length);
                for (int i = 0; i < indexes.Length; i++)
                {
                    indexesBaked[i] = indexes[i];
                }

                definition.addRagdolledBonesToLinkedGroup = authoring.addRagdolledBonesToLinkedGroup;
                BlobAssetReference<LatiosRagdolledAvatarBindings.Blob> blobReference = builder.CreateBlobAssetReference<LatiosRagdolledAvatarBindings.Blob>(Allocator.Persistent);
                AddBlobAsset(ref blobReference, out var hash);
                builder.Dispose();

                AddComponent(e, new LatiosRagdolledAvatarBindings { blob = blobReference });
                if (authoring.atStart) AddComponent<LatiosRagdollInstantiateTag>(e);
            }
        }
    }
}
#endif