using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace RedVSBlue.WarriorManagerV3
{
    [BurstCompile]
    struct GetClosestTargetWarriorsJob : IJob
    {
        [ReadOnly]
        public NativeList<Vector3> sourcePositions;
        
        [ReadOnly]
        public NativeList<Vector3> targetPositions;
        
        [WriteOnly]
        public NativeList<int> targetsBuffer;

        public void Execute()
        {
            targetsBuffer.Clear();
            var sourcePositionsCount = sourcePositions.Length;
            var targetPositionsCount = targetPositions.Length;
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
    }


    public class WarriorManagerScript : MonoBehaviour
    {
        public float walkingSpeed;
        public float destructionDistance;
        
        private NativeList<Vector3> redPositions;
        private NativeList<Vector3> bluePositions;

        private NativeList<int> redTargets;
        private NativeList<int> blueTargets;

        private NativeList<int> redToDestroy;
        private NativeList<int> blueToDestroy;

        private NativeList<TransformAccess> redTransforms;
        private NativeList<TransformAccess> blueTransforms;

        private void Start()
        {
            redPositions = new NativeList<Vector3>(Allocator.Persistent);
            bluePositions = new NativeList<Vector3>(Allocator.Persistent);
            
            redTargets = new NativeList<int>(Allocator.Persistent);
            blueTargets = new NativeList<int>(Allocator.Persistent);
            
            redToDestroy = new NativeList<int>(Allocator.Persistent);
            blueToDestroy = new NativeList<int>(Allocator.Persistent);
            
            redTransforms = new NativeList<TransformAccess>(Allocator.Persistent);
            blueTransforms = new NativeList<TransformAccess>(Allocator.Persistent);
        }

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
            var blueJob = new GetClosestTargetWarriorsJob
            {
                sourcePositions = bluePositions,
                targetPositions = redPositions,
                targetsBuffer = blueTargets
            };
            var blueHandle = blueJob.Schedule();
            var redJob = new GetClosestTargetWarriorsJob
            {
                sourcePositions = redPositions,
                targetPositions = bluePositions,
                targetsBuffer = redTargets
            };
            var redHandle = redJob.Schedule();

            blueHandle.Complete();
            redHandle.Complete();

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

        private void DestroyWarriors(GameObject[] warriors, NativeList<int> warriorsToDestroy)
        {
            var warriorsToDestroyCount = warriorsToDestroy.Length;
            for (var i = 0; i < warriorsToDestroyCount; i++)
            {
                var index = warriorsToDestroy[i];
                Destroy(warriors[index]);
            }
        }

        private void ApplyPositions(GameObject[] sourceWarriors, NativeList<Vector3> warriorsPositions)
        {
            var warriorsCount = sourceWarriors.Length;
            for (var i = 0; i < warriorsCount; i++)
            {
                sourceWarriors[i].transform.position = warriorsPositions[i];
            }
        }

        private void ExtractPositions(GameObject[] sourceWarriors, NativeList<Vector3> warriorsPositions)
        {
            warriorsPositions.Clear();
            var warriorsCount = sourceWarriors.Length;
            for (var i = 0; i < warriorsCount; i++)
            {
                warriorsPositions.Add(sourceWarriors[i].transform.position);
            }
        }

        private void WarriorsMovement(NativeList<Vector3> warriors,
            NativeList<Vector3> targets,
            NativeList<int> targetsIndices)
        {
            var warriorsCount = warriors.Length;
            for (var i = 0; i < warriorsCount; i++)
            {
                var direction = targets[targetsIndices[i]] - warriors[i];
                var normalizedDirection = direction.normalized;
                warriors[i] += walkingSpeed * Time.deltaTime * normalizedDirection;
            }
        }

        private void CheckCollision(
            NativeList<Vector3> warriors,
            NativeList<Vector3> targets,
            NativeList<int> targetsIndices,
            NativeList<int> sourcesToDestroy,
            NativeList<int> targetsToDestroy)
        {
            var warriorsCount = warriors.Length;
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

        private void OnDestroy()
        {
            redPositions.Dispose();
            bluePositions.Dispose();
            
            redTargets.Dispose();
            blueTargets.Dispose();
            
            redToDestroy.Dispose();
            blueToDestroy.Dispose();
        }
    }
}