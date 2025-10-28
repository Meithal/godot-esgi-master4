using UnityEngine;
using UnityEngine.Profiling;

namespace RedVSBlue.Classic
{
    public class WarriorScript : MonoBehaviour
    {
        public enum Faction
        {
            Red,
            Blue
        }
        
        public Faction faction;
        public float walkingSpeed;
        public float destructionDistance;

        private void Update()
        {
            // Profiler.BeginSample("GetClosestTargetWarrior");
            var target = GetClosestTargetWarrior();
            // Profiler.EndSample();
            // Profiler.BeginSample("WarriorMovement");
            WarriorMovement(target);
            // Profiler.EndSample();
            // Profiler.BeginSample("CheckCollision");
            CheckCollision(target);
            // Profiler.EndSample();
        }

        private void CheckCollision(Transform target)
        {
            var distance = (target.position - transform.position).magnitude;
            if (distance <= destructionDistance)
            {
                Destroy(gameObject);
                Destroy(target.gameObject);
            }
        }

        private void WarriorMovement(Transform target)
        {
            var direction = target.position - transform.position;
            var normalizedDirection = direction.normalized;
            transform.position += walkingSpeed * Time.deltaTime * normalizedDirection;
        }

        private Transform GetClosestTargetWarrior()
        {
            Transform target = null;

            Profiler.BeginSample("FindObjectsByType");
            var allWarriors = FindObjectsByType<WarriorScript>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );
            Profiler.EndSample();

            var minDistance = float.MaxValue;
            foreach (var warrior in allWarriors)
            {
                Profiler.BeginSample("Compare Faction");
                var sameFaction = warrior.faction == faction;
                Profiler.EndSample();
                
                if (sameFaction)
                {
                    continue;
                }

                Profiler.BeginSample("Compute and update min Distance");
                var distance = (transform.position - warrior.transform.position).magnitude;
                if (distance < minDistance)
                {
                    target = warrior.transform;
                    minDistance = distance;
                }
                Profiler.EndSample();
            }

            return target;
        }
    }
}