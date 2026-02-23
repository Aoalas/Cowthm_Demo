#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Milease.CodeGen
{
    internal static class GenerationDisableList
    {
        // Put the types for which animation code generation is disabled here
        internal static HashSet<string> GetDisableTypeList()
        {
            return new HashSet<string>()
            {
                "System.Drawing.SizeF",
                "UnityEditor.ComplexD"
            };
        }
        
        internal static HashSet<string> GetDisableNameSpaceList()
        {
            return new HashSet<string>()
            {
                // Unity.Mathematics
                "Unity.Mathematics"
            };
        }
    }
}
#endif
