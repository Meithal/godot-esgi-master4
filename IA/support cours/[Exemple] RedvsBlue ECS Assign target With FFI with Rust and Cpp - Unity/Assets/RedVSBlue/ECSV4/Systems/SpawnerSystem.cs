using RedVSBlue.ECSV4.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace RedVSBlue.ECSV4.Systems
{
    [UpdateBefore(typeof(AssignTargetSystem))]
    internal partial struct SpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<Pause>())
            {
                return;
            }
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var time = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (spawner, tr) in
                     SystemAPI.Query<RefRW<Spawner>, RefRO<LocalTransform>>())
            {
                if (time - spawner.ValueRO.lastSpawnTime < spawner.ValueRO.delay)
                {
                    continue;
                }

                var warrior = ecb.Instantiate(spawner.ValueRO.entittyPrefabToSpawn);
                
                ecb.SetComponent(warrior, new LocalTransform
                {
                    Position = tr.ValueRO.Position + new float3(0f, 0.5f, 0f),
                    Rotation = tr.ValueRO.Rotation,
                    Scale = tr.ValueRO.Scale
                });
                
                ecb.AddComponent<Target>(warrior);
                ecb.SetComponentEnabled<Target>(warrior, false);
                
                spawner.ValueRW.lastSpawnTime = time;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}