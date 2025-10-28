using System;
using RedVSBlue.ECSV4.Systems;
using Unity.Entities;
using UnityEngine;

namespace RedVSBlue.ECSV4.Components
{
    public class ButtonHandlerScript : MonoBehaviour
    {
        public void Start()
        {
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TotoSystem>()
                .TriggerAction += () =>
            {
                // Debug.Log("Triggered !!!");
            };
        }


        private Entity? pauseEntity;
        public void OnButtonClicked()
        {
            if (pauseEntity == null)
            {
                pauseEntity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateSingleton<Pause>();
            }
            else
            {
                World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(pauseEntity.Value);
                pauseEntity = null;
            }
        }
    }

    struct Pause : IComponentData
    {
        
    }
}
