using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace RedVSBlue.ECSV4.Components
{
    public class GameplaySettingsAuthoring: MonoBehaviour
    {
        public float warriorSpeed;

        private class AuthoringBaker : Baker<GameplaySettingsAuthoring>
        {
            public override void Bake(GameplaySettingsAuthoring authoring)
            {
                var ent = GetEntity(TransformUsageFlags.None);
                AddComponent(ent, new WarriorSpeed
                {
                    Value = authoring.warriorSpeed
                });
            }
        }
    }

    public struct WarriorSpeed : IComponentData
    {
        public float Value;
    }
}