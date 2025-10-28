using System;
using System.Runtime.InteropServices;
using RedVSBlue.ECSV4.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;

namespace RedVSBlue.ECSV4.Systems
{
    public partial struct AssignTargetFromNativeSystem : ISystem
    {
        private EntityQuery _redWarriorsQuery;
        private EntityQuery _blueWarriorsQuery;

        private EntityQuery _redWarriorsSourceQuery;
        private EntityQuery _blueWarriorsSourceQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _redWarriorsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Warrior, Red, LocalTransform>()
                .Build(ref state);
            _blueWarriorsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Warrior, Blue, LocalTransform>()
                .Build(ref state);
            _redWarriorsSourceQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Warrior, Red, LocalTransform>()
                .WithDisabled<Target>()
                .Build(ref state);
            _blueWarriorsSourceQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Warrior, Blue, LocalTransform>()
                .WithDisabled<Target>()
                .Build(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Profiler.BeginSample("Entity Arrays");
            state.Dependency.Complete();

            var redWarriorsEntities = _redWarriorsQuery.ToEntityArray(Allocator.TempJob);
            var blueWarriorsEntities = _blueWarriorsQuery.ToEntityArray(Allocator.TempJob);
            var redWarriorsSourceEntities = _redWarriorsSourceQuery.ToEntityArray(Allocator.TempJob);
            var blueWarriorsSourceEntities = _blueWarriorsSourceQuery.ToEntityArray(Allocator.TempJob);

            // Profiler.EndSample();
            // Profiler.BeginSample("Local Transform");

            var redWarriorsLocalTransform =
                _redWarriorsQuery.ToComponentDataListAsync<LocalTransform>(Allocator.TempJob, out var redJobHandle);

            var blueWarriorsLocalTransform =
                _blueWarriorsQuery.ToComponentDataListAsync<LocalTransform>(Allocator.TempJob, out var blueJobHandle);

            var redWarriorsSourceLocalTransform =
                _redWarriorsSourceQuery.ToComponentDataListAsync<LocalTransform>(Allocator.TempJob,
                    out var redSourceJobHandle);

            var blueWarriorsSourceLocalTransform =
                _blueWarriorsSourceQuery.ToComponentDataListAsync<LocalTransform>(Allocator.TempJob,
                    out var blueSourceJobHandle);

            redJobHandle.Complete();
            blueJobHandle.Complete();
            redSourceJobHandle.Complete();
            blueSourceJobHandle.Complete();
            // Profiler.EndSample();
            // Profiler.BeginSample("LocalTransform To Positions");
            var redWarriorsPositions = new NativeArray<float3>(redWarriorsLocalTransform.Length, Allocator.TempJob);
            for (var i = 0; i < redWarriorsLocalTransform.Length; i++)
            {
                redWarriorsPositions[i] = redWarriorsLocalTransform[i].Position;
            }

            var blueWarriorsPositions = new NativeArray<float3>(blueWarriorsLocalTransform.Length, Allocator.TempJob);
            for (var i = 0; i < blueWarriorsLocalTransform.Length; i++)
            {
                blueWarriorsPositions[i] = blueWarriorsLocalTransform[i].Position;
            }

            var redWarriorsSourcePositions =
                new NativeArray<float3>(redWarriorsSourceLocalTransform.Length, Allocator.TempJob);
            for (var i = 0; i < redWarriorsSourceLocalTransform.Length; i++)
            {
                redWarriorsSourcePositions[i] = redWarriorsSourceLocalTransform[i].Position;
            }

            var blueWarriorsSourcePositions =
                new NativeArray<float3>(blueWarriorsSourceLocalTransform.Length, Allocator.TempJob);
            for (var i = 0; i < blueWarriorsSourceLocalTransform.Length; i++)
            {
                blueWarriorsSourcePositions[i] = blueWarriorsSourceLocalTransform[i].Position;
            }
            // Profiler.EndSample();

            // Profiler.BeginSample("Native Calls");

            // var ptrToBlueIndices = Marshal.AllocHGlobal(sizeof(int) * blueWarriorsSourcePositions.Length);
            var blueIndicesArray = new NativeArray<int>(blueWarriorsSourcePositions.Length, Allocator.TempJob);

            unsafe
            {
                FFIAPI.ComputeTargets(
                    (IntPtr)blueWarriorsSourcePositions.GetUnsafeReadOnlyPtr(),
                    blueWarriorsSourcePositions.Length,
                    (IntPtr)redWarriorsPositions.GetUnsafeReadOnlyPtr(),
                    redWarriorsPositions.Length,
                    (IntPtr)blueIndicesArray.GetUnsafePtr()
                );
            }

            // var ptrToRedIndices = Marshal.AllocHGlobal(sizeof(int) * redWarriorsSourcePositions.Length);
            var redIndicesArray = new NativeArray<int>(redWarriorsSourcePositions.Length, Allocator.TempJob);

            unsafe
            {
                FFIAPI.ComputeTargets(
                    (IntPtr)redWarriorsSourcePositions.GetUnsafeReadOnlyPtr(),
                    redWarriorsSourcePositions.Length,
                    (IntPtr)blueWarriorsPositions.GetUnsafeReadOnlyPtr(),
                    blueWarriorsPositions.Length,
                    (IntPtr)redIndicesArray.GetUnsafePtr()
                );
            }
            // Profiler.EndSample();
            // Profiler.BeginSample("Indices to Targets");

            for (var i = 0; i < blueWarriorsSourceEntities.Length; i++)
            {
                var sourceEntity = blueWarriorsSourceEntities[i];
                var targetEntityIndex = blueIndicesArray[i];

                state.EntityManager.SetComponentData(sourceEntity, new Target
                {
                    targetEntity = redWarriorsEntities[targetEntityIndex]
                });
                state.EntityManager.SetComponentEnabled<Target>(sourceEntity, true);
            }

            for (var i = 0; i < redWarriorsSourceEntities.Length; i++)
            {
                var sourceEntity = redWarriorsSourceEntities[i];
                var targetEntityIndex = redIndicesArray[i];

                state.EntityManager.SetComponentData(sourceEntity, new Target
                {
                    targetEntity = blueWarriorsEntities[targetEntityIndex]
                });
                state.EntityManager.SetComponentEnabled<Target>(sourceEntity, true);
            }
            // Profiler.EndSample();
            // Profiler.BeginSample("Dispose");

            // Marshal.FreeHGlobal(ptrToBlueIndices);
            // Marshal.FreeHGlobal(ptrToRedIndices);

            redWarriorsEntities.Dispose();
            blueWarriorsEntities.Dispose();
            redWarriorsPositions.Dispose();
            blueWarriorsPositions.Dispose();

            redWarriorsLocalTransform.Dispose();
            blueWarriorsLocalTransform.Dispose();
            redWarriorsSourceLocalTransform.Dispose();
            blueWarriorsSourceLocalTransform.Dispose();

            redWarriorsSourceEntities.Dispose();
            blueWarriorsSourceEntities.Dispose();
            redWarriorsSourcePositions.Dispose();
            blueWarriorsSourcePositions.Dispose();

            blueIndicesArray.Dispose();
            redIndicesArray.Dispose();
            // Profiler.EndSample();
        }
    }
}