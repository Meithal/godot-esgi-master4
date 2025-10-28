using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;


namespace RedVSBlue.FFI.Scripts
{
    public class DemoFFIScript : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log(FFIAPI.Return42());
            Debug.Log(FFIAPI.MyAdd(45, 24));
            
            var eltPtr = FFIAPI.ReturnMyStruct();

            var elt = Marshal.PtrToStructure<float3>(eltPtr);
            Debug.Log(elt.x);
            Debug.Log(elt.y);
            Debug.Log(elt.z);
            
            
            FFIAPI.DeleteMyStruct(eltPtr);

            var sourcePositions = new float3[]
            {
                new float3(0, 0, 0),
                new float3(10, 10, 10),
            };
            var targetPositions = new float3[]
            {
                new float3(9, 9, 9),
                new float3(50, 50, 50),
                new float3(1, 1, 1),
                new float3(-0.5f, -0.5f, -0.5f),
            };
            var indices = new int[]
            {
                0,
                0
            };
            unsafe
            {
                var ptrToSourcePositions = Marshal.AllocHGlobal(sizeof(float3) * sourcePositions.Length);
                var ptr = (float3*) ptrToSourcePositions.ToPointer();
                for (var i = 0; i < sourcePositions.Length; i++)
                {
                    ptr[i] = sourcePositions[i];
                }
                var ptrToTargetPositions = Marshal.AllocHGlobal(sizeof(float3) * targetPositions.Length);
                ptr = (float3*) ptrToTargetPositions.ToPointer();
                for (var i = 0; i < targetPositions.Length; i++)
                {
                    ptr[i] = targetPositions[i];
                }
                var ptrToIndices = Marshal.AllocHGlobal(sizeof(int) * sourcePositions.Length);
                
                FFIAPI.ComputeTargets(ptrToSourcePositions,
                    sourcePositions.Length,
                    ptrToTargetPositions,
                    targetPositions.Length,
                    ptrToIndices
                    );

                var ptrIndices = (int*) ptrToIndices.ToPointer();

                for (var i = 0; i < sourcePositions.Length; i++)
                {
                    indices[i] = ptrIndices[i];
                }
                
                Marshal.FreeHGlobal(ptrToSourcePositions);
                Marshal.FreeHGlobal(ptrToTargetPositions);
                Marshal.FreeHGlobal(ptrToIndices);

                Debug.Log("Affichons les indices !!!");
                for (var i = 0; i < indices.Length; i++)
                {
                    Debug.Log(indices[i]);
                }
            }
        }

        private void Update()
        {
            var sourcePositions = new float3[10_000];
            var targetPositions = new float3[10_000];
            var indices = new int[10_000];
            unsafe
            {
                var ptrToSourcePositions = Marshal.AllocHGlobal(sizeof(float3) * sourcePositions.Length);
                var ptr = (float3*)ptrToSourcePositions.ToPointer();
                for (var i = 0; i < sourcePositions.Length; i++)
                {
                    ptr[i] = sourcePositions[i];
                }

                var ptrToTargetPositions = Marshal.AllocHGlobal(sizeof(float3) * targetPositions.Length);
                ptr = (float3*)ptrToTargetPositions.ToPointer();
                for (var i = 0; i < targetPositions.Length; i++)
                {
                    ptr[i] = targetPositions[i];
                }

                var ptrToIndices = Marshal.AllocHGlobal(sizeof(int) * sourcePositions.Length);

                FFIAPI.ComputeTargets(ptrToSourcePositions,
                    sourcePositions.Length,
                    ptrToTargetPositions,
                    targetPositions.Length,
                    ptrToIndices
                );

                var ptrIndices = (int*)ptrToIndices.ToPointer();

                for (var i = 0; i < sourcePositions.Length; i++)
                {
                    indices[i] = ptrIndices[i];
                }

                Marshal.FreeHGlobal(ptrToSourcePositions);
                Marshal.FreeHGlobal(ptrToTargetPositions);
                Marshal.FreeHGlobal(ptrToIndices);
            }
        }
    }
}