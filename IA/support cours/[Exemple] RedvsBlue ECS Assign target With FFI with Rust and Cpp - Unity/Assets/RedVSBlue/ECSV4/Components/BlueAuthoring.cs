using Unity.Entities;
using UnityEngine;

namespace RedVSBlue.ECSV4.Components
{
    public class BlueAuthoring : MonoBehaviour
    {
    }

    public class BlueAuthoringBaker : Baker<BlueAuthoring>
    {
        public override void Bake(BlueAuthoring authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new Blue());
        }
    }

    public struct Blue: IComponentData
    {
    }
}