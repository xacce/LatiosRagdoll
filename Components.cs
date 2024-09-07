using Unity.Entities;

namespace LatiosRagdoll
{
    public partial struct LatiosRagdolledBone : IBufferElementData
    {
        public Entity collider;
    }

    public partial struct LatiosRagdolledAvatarBindings : IComponentData
    {
        public struct Blob
        {
            public BlobArray<int> boneIndexes;
            public bool addRagdolledBonesToLinkedGroup;
        }

        public BlobAssetReference<Blob> blob;
    }


    public partial struct LatiosRagdolledPrefab : IComponentData
    {
        public Entity physicsStructurePrefab;
    }

    public partial struct LatiosRagdolled : IComponentData
    {
        public enum State
        {
            Setuped,
            Ready,
        }

        public State state;
        public Entity ragdollWrapper;
    }

    public partial struct LatiosRagdollInstantiateTag : IComponentData
    {
    }
}