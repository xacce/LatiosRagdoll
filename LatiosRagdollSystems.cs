using Latios.Kinemation;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;

namespace LatiosRagdoll
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    partial struct RagdolSpawnSystem : ISystem
    {
        [BurstCompile]
        [WithAll(typeof(LatiosRagdollInstantiateTag))]
        partial struct SpawnRagdolls : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;
            [NativeSetThreadIndex] private int index;

            [BurstCompile]
            void Execute(in LatiosRagdolledPrefab prefab, in LatiosRagdolledAvatarBindings bindings, Entity entity)
            {
                var spawned = ecb.Instantiate(index, prefab.physicsStructurePrefab);
                ecb.RemoveComponent<LatiosRagdollInstantiateTag>(index, entity);
                ecb.AddComponent(index, entity, new LatiosRagdolled { ragdollWrapper = spawned, state = LatiosRagdolled.State.Setuped });
                if (bindings.blob.Value.addRagdolledBonesToLinkedGroup)
                {
                    ecb.AppendToBuffer(index, entity, new LinkedEntityGroup() { Value = spawned });
                }
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            new SpawnRagdolls()
            {
                ecb = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        public struct Lookups
        {
            [ReadOnly] public ComponentLookup<LocalToWorld> localToWorldRo;

            public Lookups(ref SystemState state) : this()
            {
                localToWorldRo = state.GetComponentLookup<LocalToWorld>();
            }

            [BurstCompile]
            public void Update(ref SystemState state)
            {
                localToWorldRo.Update(ref state);
            }
        }
    }


    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    // [UpdateBefore(typeof(Unity.Transforms.TransformSystemGroup))]
    public partial struct RagdollSyncSystem : ISystem
    {
        private Lookups _lookups;

        [BurstCompile]
        partial struct Sync : IJobEntity
        {
            [NativeSetThreadIndex] private int index;
            [ReadOnly] public ComponentLookup<LocalToWorld> localToWorldRo;
            [ReadOnly] public BufferLookup<LatiosRagdolledBone> ragdolledboneRo;
            public EntityCommandBuffer.ParallelWriter ecb;

            [BurstCompile]
            public void Execute(ref LatiosRagdolled ragdolled, in LatiosRagdolledAvatarBindings bindings, OptimizedSkeletonAspect skeletonAspect, Entity entity)
            {
                var ragdolledBones = ragdolledboneRo[ragdolled.ragdollWrapper];
                var bones = skeletonAspect.bones;
                ref var boneIndexes = ref bindings.blob.Value.boneIndexes;
                switch (ragdolled.state)
                {
                    case LatiosRagdolled.State.Ready:
                        for (int i = 0; i < boneIndexes.Length; i++)
                        {
                            var boneIndex = boneIndexes[i];
                            if (boneIndex == -1) continue;
                            var ragdolledBone = ragdolledBones[i];
                            var bone = bones[boneIndex];
                            var ltw = localToWorldRo[ragdolledBone.collider];
                            bone.worldPosition = ltw.Position;
                            bone.worldRotation = ltw.Rotation;
                        }

                        break;
                    case LatiosRagdolled.State.Setuped:
                        ragdolled.state = LatiosRagdolled.State.Ready;
                        for (int i = 0; i < boneIndexes.Length; i++)
                        {
                            var boneIndex = boneIndexes[i];
                            if (boneIndex == -1) continue;

                            var ragdolledBone = ragdolledBones[i];
                            var bone = bones[boneIndex];
                            var lt = new LocalTransform() { Position = bone.worldPosition, Rotation = bone.worldRotation, Scale = 1f };
                            ecb.SetComponent(index, ragdolledBone.collider, lt);
                        }

                        break;
                }
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _lookups = new Lookups(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _lookups.Update(ref state);
            var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            new Sync()
            {
                localToWorldRo = _lookups.localToWorldRo,
                ragdolledboneRo = _lookups.ragdolledboneRo,
                ecb = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        public struct Lookups
        {
            [ReadOnly] public ComponentLookup<LocalToWorld> localToWorldRo;
            [ReadOnly] public BufferLookup<LatiosRagdolledBone> ragdolledboneRo;

            public Lookups(ref SystemState state) : this()
            {
                localToWorldRo = state.GetComponentLookup<LocalToWorld>();
                ragdolledboneRo = state.GetBufferLookup<LatiosRagdolledBone>();
            }

            [BurstCompile]
            public void Update(ref SystemState state)
            {
                localToWorldRo.Update(ref state);
                ragdolledboneRo.Update(ref state);
            }
        }
    }
}