using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace RedVSBlue.WarriorManagerV1
{
    public class WarriorManagerScript : MonoBehaviour
    {
        public float walkingSpeed;
        public float destructionDistance;

        private readonly List<Transform> redTargets = new ();
        private readonly List<Transform> blueTargets = new ();
        
        private void Update()
        {
            // Profiler.BeginSample("FindGameObjectsWithTag");
            var blueWarriors = GameObject.FindGameObjectsWithTag("Blue");
            var redWarriors = GameObject.FindGameObjectsWithTag("Red");
            // Profiler.EndSample();

            // Profiler.BeginSample("GetClosestTargetWarriors");
            GetClosestTargetWarriors(blueWarriors, redWarriors, blueTargets);
            GetClosestTargetWarriors(redWarriors, blueWarriors, redTargets);
            // Profiler.EndSample();

            // Profiler.BeginSample("WarriorsMovement");
            WarriorsMovement(blueWarriors, blueTargets);
            WarriorsMovement(redWarriors, redTargets);
            // Profiler.EndSample();

            // Profiler.BeginSample("CheckCollision");
            CheckCollision(blueWarriors, blueTargets);
            CheckCollision(redWarriors, redTargets);
            // Profiler.EndSample();
        }

        private void GetClosestTargetWarriors(
            GameObject[] sourceWarriors,
            GameObject[] targetWarriors,
            List<Transform> targetsBuffer
        )
        {
            targetsBuffer.Clear();
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < sourceWarriors.Length; index++)
            {
                var sourceWarrior = sourceWarriors[index];
                var minDistance = float.MaxValue;
                Transform target = null;

                foreach (var targetWarrior in targetWarriors)
                {
                    // Profiler.BeginSample("get positions");
                    var sourcePosition = sourceWarrior.transform.position;
                    var targetPosition = targetWarrior.transform.position;
                    // Profiler.EndSample();
                    
                    // Profiler.BeginSample("compute distance");
                    var distance = (sourcePosition - targetPosition).magnitude;
                    // Profiler.EndSample();
                    
                    // Profiler.BeginSample("update distance");
                    if (distance <= minDistance)
                    {
                        target = targetWarrior.transform;
                        minDistance = distance;
                    }
                    // Profiler.EndSample();
                }

                targetsBuffer.Add(target);
            }
        }

        private void WarriorsMovement(GameObject[] warriors, List<Transform> targets)
        {
            for (var i = 0; i < warriors.Length; i++)
            {
                var direction = targets[i].position - warriors[i].transform.position;
                var normalizedDirection = direction.normalized;
                warriors[i].transform.position += walkingSpeed * Time.deltaTime * normalizedDirection;
            }
        }

        private void CheckCollision(GameObject[] warriors, List<Transform> targets)
        {
            for (var i = 0; i < warriors.Length; i++)
            {
                var distance = (targets[i].position - warriors[i].transform.position).magnitude;
                if (distance <= destructionDistance)
                {
                    Destroy(warriors[i]);
                    Destroy(targets[i].gameObject);
                }
            }
        }
    }
}