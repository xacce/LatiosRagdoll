using Latios.Kinemation;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Aspects;
using Unity.Transforms;
using UnityEngine;

namespace LatiosRagdoll
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    partial struct RagdolSpawnSystem : ISystem
    {
        [BurstCompile]
        partial struct SpawnRagdolls : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;
            [NativeSetThreadIndex] private int index;

            [BurstCompile]
            void Execute(in LatiosRagdolledPrefab prefab, in LatiosRagdolledAvatarBindings bindings, LatiosRagdollInstantiate inst, Entity entity)
            {
                var spawned = ecb.Instantiate(index, prefab.physicsStructurePrefab);
            
                ecb.AddComponent(index, entity, new LatiosRagdolled
                {
                    ragdollWrapper = spawned,
                    state = LatiosRagdolled.State.Setuped,
                    applyPowerDirection = inst.applyPowerDirection,
                    applyPowerFrom = inst.applyPowerFrom,
                });
                if (bindings.blob.Value.addRagdolledBonesToLinkedGroup)
                {
                    ecb.AppendToBuffer(index, entity, new LinkedEntityGroup() { Value = spawned });
                }

                ecb.RemoveComponent<LatiosRagdollInstantiate>(index, entity);
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
    public partial struct ApplyImpulsesToBone : IJobEntity
    {
        [BurstCompile]
        public void Execute(ref LationRagdolledBoneWithInfluence influence, RigidBodyAspect aspect)
        {
            if (influence.applyPowerFrom.Equals(float3.zero)) return;
            aspect.ApplyImpulseAtPointWorldSpace(influence.applyPowerDirection, influence.applyPowerFrom);
            influence.applyPowerFrom = float3.zero;
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
                 
                    case LatiosRagdolled.State.Setuped:
                        ragdolled.state = LatiosRagdolled.State.Ready;
                        for (int i = 0; i < ragdolledBones.Length; i++)
                        {
                            var boneIndex = boneIndexes[i];
                            if (boneIndex == -1) continue;

                            var ragdolledBone = ragdolledBones[i];
                            var bone = bones[boneIndex];
                            var lt = new LocalTransform() { Position = bone.worldPosition, Rotation = bone.worldRotation, Scale = 1f };
                            ecb.SetComponent(index, ragdolledBone.collider, lt);
                            if (!ragdolled.applyPowerFrom.Equals(float3.zero))
                            {
                                ecb.AddComponent(index, ragdolledBone.collider, new LationRagdolledBoneWithInfluence()
                                {
                                    applyPowerDirection = ragdolled.applyPowerDirection,
                                    applyPowerFrom = ragdolled.applyPowerFrom
                                });
                            }
                        }

                        break;
                    case LatiosRagdolled.State.Ready:
                        for (int i = 0; i < ragdolledBones.Length; i++)
                        {
                            var boneIndex = boneIndexes[i];
                            if (boneIndex == -1) continue;
                            var ragdolledBone =  ragdolledBones[i];
                            var bone = bones[boneIndex];
                            var ltw = localToWorldRo[ragdolledBone.collider];
                            bone.worldPosition = ltw.Position;
                            bone.worldRotation = ltw.Rotation;
                           
                        }

                        break;
                }

                ragdolled.applyPowerFrom = float3.zero;
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
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
            new ApplyImpulsesToBone().ScheduleParallel();
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