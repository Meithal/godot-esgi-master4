using Unity.Entities;
using UnityEngine;

namespace RedVSBlue.ECSV4.Components
{
    public class RedAuthoring : MonoBehaviour
    {
    }

    public class RedAuthoringBaker : Baker<RedAuthoring>
    {
        public override void Bake(RedAuthoring authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new Red());
        }
    }
    
    public struct Red: IComponentData
    {
    }
}