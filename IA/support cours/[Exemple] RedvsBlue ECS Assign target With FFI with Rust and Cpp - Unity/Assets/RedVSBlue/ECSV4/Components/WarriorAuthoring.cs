using Unity.Entities;
using UnityEngine;

namespace RedVSBlue.ECSV4.Components
{
    public class WarriorAuthoring : MonoBehaviour
    {
    }

    public class WarriorAuthoringBaker : Baker<WarriorAuthoring>
    {
        public override void Bake(WarriorAuthoring authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new Warrior());
        }
    }

    public struct Warrior: IComponentData
    {
    }

    public struct Target : IComponentData, IEnableableComponent
    {
        public Entity targetEntity;
    }
}