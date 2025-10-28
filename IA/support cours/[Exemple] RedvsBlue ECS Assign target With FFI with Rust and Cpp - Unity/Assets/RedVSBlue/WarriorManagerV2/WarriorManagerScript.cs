using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace RedVSBlue.WarriorManagerV2
{
    public class WarriorManagerScript : MonoBehaviour
    {
        public float walkingSpeed;
        public float destructionDistance;

        private readonly List<Vector3> redPositions = new();
        private readonly List<Vector3> bluePositions = new();
        
        private readonly List<int> redTargets = new ();
        private readonly List<int> blueTargets = new ();
        
        private readonly List<int> redToDestroy = new ();
        private readonly List<int> blueToDestroy = new ();
        
        private void Update()
        {
            Profiler.BeginSample("FindGameObjectsWithTag");
            var blueWarriors = GameObject.FindGameObjectsWithTag("Blue");
            var redWarriors = GameObject.FindGameObjectsWithTag("Red");
            Profiler.EndSample();
            
            Profiler.BeginSample("ExtractPositions");
            ExtractPositions(blueWarriors, bluePositions);
            ExtractPositions(redWarriors, redPositions);
            Profiler.EndSample();

            Profiler.BeginSample("GetClosestTargetWarriors");
            GetClosestTargetWarriors(bluePositions, redPositions, blueTargets);
            GetClosestTargetWarriors(redPositions, bluePositions, redTargets);
            Profiler.EndSample();

            Profiler.BeginSample("WarriorsMovement");
            WarriorsMovement(bluePositions, redPositions, blueTargets);
            WarriorsMovement(redPositions, bluePositions, redTargets);
            Profiler.EndSample();

            Profiler.BeginSample("ApplyPositions");
            ApplyPositions(blueWarriors, bluePositions);
            ApplyPositions(redWarriors, redPositions);
            Profiler.EndSample();

            Profiler.BeginSample("CheckCollision");
            blueToDestroy.Clear();
            redToDestroy.Clear();
            CheckCollision(bluePositions, redPositions, blueTargets, blueToDestroy, redToDestroy);
            CheckCollision(redPositions, bluePositions, redTargets, redToDestroy, blueToDestroy);
            Profiler.EndSample();

            Profiler.BeginSample("DestroyWarriors");
            DestroyWarriors(blueWarriors, blueToDestroy);
            DestroyWarriors(redWarriors, redToDestroy);
            Profiler.EndSample();
        }

        private void DestroyWarriors(GameObject[] warriors, List<int> warriorsToDestroy)
        {
            var warriorsToDestroyCount = warriorsToDestroy.Count;
            for (var i = 0; i < warriorsToDestroyCount; i++)
            {
                var index = warriorsToDestroy[i];
                Destroy(warriors[index]);
            }
        }

        private void ApplyPositions(GameObject[] sourceWarriors, List<Vector3> warriorsPositions)
        {
            var warriorsCount = sourceWarriors.Length;
            for (var i = 0; i < warriorsCount; i++)
            {
                sourceWarriors[i].transform.position = warriorsPositions[i];
            }
        }

        private void ExtractPositions(GameObject[] sourceWarriors, List<Vector3> warriorsPositions)
        {
            warriorsPositions.Clear();
            var warriorsCount = sourceWarriors.Length;
            for (var i = 0; i < warriorsCount; i++)
            {
                warriorsPositions.Add(sourceWarriors[i].transform.position);
            }
        }

        private void GetClosestTargetWarriors(
            List<Vector3> sourcePositions,
            List<Vector3> targetPositions,
            List<int> targetsBuffer
        )
        {
            targetsBuffer.Clear();
            var sourcePositionsCount = sourcePositions.Count;
            var targetPositionsCount = targetPositions.Count;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < sourcePositionsCount; index++)
            {
                var sourcePosition = sourcePositions[index];
                var minDistance = float.MaxValue;
                var target = 0;

                for (var j = 0; j < targetPositionsCount; j++)
                {
                    var targetPosition = targetPositions[j];
                    
                    // Profiler.BeginSample("compute distance");
                    var distance = (sourcePosition - targetPosition).sqrMagnitude;
                    // Profiler.EndSample();
                    
                    // Profiler.BeginSample("update distance");
                    if (distance <= minDistance)
                    {
                        target = j;
                        minDistance = distance;
                    }
                    // Profiler.EndSample();
                }

                targetsBuffer.Add(target);
            }
        }

        private void WarriorsMovement(List<Vector3> warriors, 
            List<Vector3> targets,
            List<int> targetsIndices)
        {
            var warriorsCount = warriors.Count;
            for (var i = 0; i < warriorsCount; i++)
            {
                var direction = targets[targetsIndices[i]] - warriors[i];
                var normalizedDirection = direction.normalized;
                warriors[i] += walkingSpeed * Time.deltaTime * normalizedDirection;
            }
        }

        private void CheckCollision(
            List<Vector3> warriors, 
            List<Vector3> targets,
            List<int> targetsIndices, 
            List<int> sourcesToDestroy,
            List<int> targetsToDestroy)
        {
            var warriorsCount = warriors.Count;
            for (var i = 0; i < warriorsCount; i++)
            {
                var targetIndex = targetsIndices[i];
                var distance = (targets[targetIndex] - warriors[i]).sqrMagnitude;
                if (distance <= destructionDistance * destructionDistance)
                {
                    sourcesToDestroy.Add(i);
                    targetsToDestroy.Add(targetIndex);
                }
            }
        }
    }
}