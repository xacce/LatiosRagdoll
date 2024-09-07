#if UNITY_EDITOR
using System;
using Unity.Entities;
using UnityEngine;

namespace Src.GameReady.DotsRag.Authoring
{
    public class LatiosRagdollAvatarAuthoring : MonoBehaviour
    {
        [SerializeField] public AnnotatedGameObject[] bones = Array.Empty<AnnotatedGameObject>();

        private class Baker : Baker<LatiosRagdollAvatarAuthoring>
        {
            public override void Bake(LatiosRagdollAvatarAuthoring authoring)
            {
                var e = GetEntity(authoring,TransformUsageFlags.None);
                var bones = AddBuffer<LatiosRagdolledBone>(e);
                foreach (var bone in authoring.bones)
                {
                    bones.Add(new LatiosRagdolledBone { collider = GetEntity(bone, TransformUsageFlags.Dynamic) });
                }
            }
        }
    }
}
#endif