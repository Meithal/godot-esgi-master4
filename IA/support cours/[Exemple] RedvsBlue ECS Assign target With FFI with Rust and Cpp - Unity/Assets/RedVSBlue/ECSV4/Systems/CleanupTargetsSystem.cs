using RedVSBlue.ECSV4.Components;
using Unity.Burst;
using Unity.Entities;

namespace RedVSBlue.ECSV4.Systems
{
    [UpdateBefore(typeof(AssignTargetSystem))]
    public partial struct CleanupTargetsSystem: ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (target, ent) in SystemAPI.Query<RefRO<Target>>()
                     .WithAll<Warrior>()
                     .WithEntityAccess())
            {
                if (state.EntityManager.Exists(target.ValueRO.targetEntity))
                {
                    continue;
                }
                
                state.EntityManager.SetComponentEnabled<Target>(ent, false);
            }
        }
    }
}