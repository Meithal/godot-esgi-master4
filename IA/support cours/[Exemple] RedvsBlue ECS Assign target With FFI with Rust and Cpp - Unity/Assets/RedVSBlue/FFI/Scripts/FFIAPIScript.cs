using System;
using System.Runtime.InteropServices;

public static class FFIAPI
{
    [DllImport("_2025_3A_RVJV_FFI_RedVersusBlue_Rust", EntryPoint = "returnMyStruct")]
    public static extern IntPtr ReturnMyStruct();
    
    [DllImport("_2025_3A_RVJV_FFI_RedVersusBlue_Rust", EntryPoint = "deleteMyStruct")]
    public static extern void DeleteMyStruct(IntPtr ptr);  
    
    [DllImport("_2025_3A_RVJV_FFI_RedVersusBlue_Rust", EntryPoint = "return42")]
    public static extern int Return42();  
    
    [DllImport("_2025_3A_RVJV_FFI_RedVersusBlue_Rust", EntryPoint = "my_add")]
    public static extern int MyAdd(int a, int b);
    
    [DllImport("_2025_3A_RVJV_FFI_RedVersusBlue_Rust", EntryPoint = "compute_targets")]
    public static extern void ComputeTargets(
            IntPtr sourcePositionsArray,
            int sourcePositionsArrayLength,
            IntPtr targetPositionsArray,
            int targetPositionsArrayLength,
            IntPtr resultIndices
        );
}
