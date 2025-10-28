using System;
using RedVSBlue.ECSV4.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace RedVSBlue.ECSV4.Systems
{
    public partial class TotoSystem : SystemBase
    {
        public event Action TriggerAction;

        protected override void OnUpdate()
        {
            TriggerAction?.Invoke();
        }
    }
    
    [BurstCompile]
    [WithAll(typeof(Warrior), typeof(Red))]
    [WithDisabled(typeof(Target))]
    public partial struct AssignTargetToRedEntitiesJob : IJobEntity
    {
        [ReadOnly]
        public NativeArray<Entity> TargetEntities;

        [ReadOnly] public ComponentLookup<LocalTransform> TargetPositionsLookup;

        public EntityCommandBuffer.ParallelWriter ECB;
        
        private void Execute(Entity entity, [ChunkIndexInQuery] int sortKey, ref Target target)
        {
            var position = TargetPositionsLookup.GetRefRO(entity).ValueRO.Position;

            var targetEntity = new Entity();
            var minDistance = float.MaxValue;

            for (var i = 0; i < TargetEntities.Length; i++)
            {
                var candidateEntity = TargetEntities[i];
                var candidatePosition = TargetPositionsLookup.GetRefRO(candidateEntity).ValueRO.Position;

                var squaredDistance = math.distancesq(position, candidatePosition);
                if (squaredDistance <= minDistance)
                {
                    minDistance = squaredDistance;
                    targetEntity = candidateEntity;
                }
            }

            target.targetEntity = targetEntity;
            ECB.SetComponentEnabled<Target>(sortKey, entity, true);
        }
    }

    [BurstCompile]
    [WithAll(typeof(Warrior), typeof(Blue))]
    [WithDisabled(typeof(Target))]
    public partial struct AssignTargetToBlueEntitiesJob : IJobEntity
    {
        [ReadOnly]
        public NativeArray<Entity> TargetEntities;

        [ReadOnly] public ComponentLookup<LocalTransform> TargetPositionsLookup;

        public EntityCommandBuffer.ParallelWriter ECB;
        
        private void Execute(Entity entity, [ChunkIndexInQuery] int sortKey, ref Target target)
        {
            var position = TargetPositionsLookup.GetRefRO(entity).ValueRO.Position;

            var targetEntity = new Entity();
            var minDistance = float.MaxValue;

            for (var i = 0; i < TargetEntities.Length; i++)
            {
                var candidateEntity = TargetEntities[i];
                var candidatePosition = TargetPositionsLookup.GetRefRO(candidateEntity).ValueRO.Position;

                var squaredDistance = math.distancesq(position, candidatePosition);
                if (squaredDistance <= minDistance)
                {
                    minDistance = squaredDistance;
                    targetEntity = candidateEntity;
                }
            }
            
            target.targetEntity = targetEntity;
            ECB.SetComponentEnabled<Target>(sortKey, entity, true);
        }
    }

    public partial struct AssignTargetSystem : ISystem
    {
        private EntityQuery _redWarriorsQuery;
        private EntityQuery _blueWarriorsQuery;

        private ComponentLookup<LocalTransform> _redLookup;
        private ComponentLookup<LocalTransform> _blueLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _redWarriorsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Warrior, Red>()
                .Build(ref state);
            _blueWarriorsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Warrior, Blue>()
                .Build(ref state);

            _redLookup = state.GetComponentLookup<LocalTransform>(true);
            _blueLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            return;
            
            _redLookup.Update(ref state);
            
            var redWarriorsEntities = _redWarriorsQuery.ToEntityArray(Allocator.TempJob);
            var blueWarriorsEntities = _blueWarriorsQuery.ToEntityArray(Allocator.TempJob);
            
            var blueECB = new EntityCommandBuffer(Allocator.TempJob);
            var blueParWriter = blueECB.AsParallelWriter();

            var blueJob = new AssignTargetToBlueEntitiesJob
            {
                ECB = blueParWriter,
                TargetEntities = redWarriorsEntities,
                TargetPositionsLookup = _redLookup
            };

            var blueJobHandle = blueJob.ScheduleParallel(state.Dependency);
            
            blueJobHandle.Complete();
            
            redWarriorsEntities.Dispose();
            
            blueECB.Playback(state.EntityManager);
            blueECB.Dispose();
            _blueLookup.Update(ref state);
            
            var redECB = new EntityCommandBuffer(Allocator.TempJob);
            var redParWriter = redECB.AsParallelWriter();
            
            var redJob = new AssignTargetToRedEntitiesJob
            {
                ECB = redParWriter,
                TargetEntities = blueWarriorsEntities,
                TargetPositionsLookup = _blueLookup
            };
            
            var redJobHandle = redJob.ScheduleParallel(state.Dependency);
            
            redJobHandle.Complete();
            
            blueWarriorsEntities.Dispose();
            
            redECB.Playback(state.EntityManager);
            redECB.Dispose();
        }
    }
}