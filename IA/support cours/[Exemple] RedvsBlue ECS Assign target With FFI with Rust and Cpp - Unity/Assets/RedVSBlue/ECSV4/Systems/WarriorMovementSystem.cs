using RedVSBlue.ECSV4.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace RedVSBlue.ECSV4.Systems
{
    [UpdateAfter(typeof(AssignTargetSystem))]
    internal partial struct WarriorMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WarriorSpeed>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<Pause>())
            {
                return;
            }
            
            
            var deltaTime = SystemAPI.Time.DeltaTime;
            var warriorSpeed = SystemAPI.GetSingleton<WarriorSpeed>();

            foreach (var (_, tr, target) in
                     SystemAPI.Query<RefRO<Warrior>, RefRW<LocalTransform>, RefRO<Target>>())
            {
                var targetPosition = state.EntityManager.GetComponentData<LocalTransform>(target.ValueRO.targetEntity)
                    .Position;
                var direction = targetPosition - tr.ValueRO.Position;
                var normalizedDirection = math.normalizesafe(direction, float3.zero);

                tr.ValueRW.Position += deltaTime * warriorSpeed.Value * normalizedDirection;
            }
        }
    }
}