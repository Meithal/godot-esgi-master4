using RedVSBlue.ECSV4.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace RedVSBlue.ECSV4.Systems
{
    [UpdateAfter(typeof(CleanupTargetsSystem))]
    public partial struct DestroyEntitiesSystem: ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (target, tr, ent) in SystemAPI.Query<RefRO<Target>, RefRO<LocalTransform>>()
                     .WithAll<Warrior>()
                     .WithEntityAccess())
            {
                var targetPosition =
                    state.EntityManager.GetComponentData<LocalTransform>(target.ValueRO.targetEntity).Position;

                if (math.distancesq(targetPosition, tr.ValueRO.Position) < 1.0f)
                {
                    ecb.DestroyEntity(ent);
                    ecb.DestroyEntity(target.ValueRO.targetEntity);
                }
            }
            
            ecb.Playback(state.EntityManager);
        }
    }
}