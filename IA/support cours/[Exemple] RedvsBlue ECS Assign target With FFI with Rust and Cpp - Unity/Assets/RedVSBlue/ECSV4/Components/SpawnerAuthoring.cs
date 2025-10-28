using Unity.Entities;
using UnityEngine;

namespace RedVSBlue.ECSV4.Components
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject prefabToSpawn;
        public float delay;
    }

    public class SpawnerAuthoringBaker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            DependsOn(authoring.prefabToSpawn);

            var prefabEntity = GetEntity(authoring.prefabToSpawn, TransformUsageFlags.Dynamic);
            
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new Spawner
            {
                entittyPrefabToSpawn = prefabEntity, 
                delay =  authoring.delay,
                lastSpawnTime = float.MinValue,
            });
        }
    }

    public struct Spawner: IComponentData
    {
        public Entity entittyPrefabToSpawn;
        public float delay;
        public float lastSpawnTime;
    }
}