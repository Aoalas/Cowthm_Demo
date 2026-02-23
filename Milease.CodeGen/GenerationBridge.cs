#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Milease.CodeGen
{
    public static class GenerationBridge
    {
        public static IEnumerable<Type> GetAnimatableTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var disableTypes = GenerationDisableList.GetDisableTypeList();
            var disableNamespaces = GenerationDisableList.GetDisableNameSpaceList();
            
            var types = assemblies.SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    return ex.Types.Where(t => t != null);
                }
            }).Where(x => IsTypeAnimatable(x) && 
                          !disableTypes.Contains(x.FullName) && !disableNamespaces.Contains(x.Namespace));

            return types;
        }

        private static bool IsTypeAnimatable(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).ToList();
            var addOp = methods.Find(x =>
            {
                var param = x.GetParameters();
                return x.Name == "op_Addition" && param[0].ParameterType == type &&
                       param[1].ParameterType == type && x.ReturnType == type;
            });
            var subOp = methods.Find(x =>
            {
                var param = x.GetParameters();
                return x.Name == "op_Subtraction" && param[0].ParameterType == type &&
                       param[1].ParameterType == type && x.ReturnType == type;
            });
            var mulOp = methods.Find(x =>
            {
                var param = x.GetParameters();
                return x.Name == "op_Multiply" && param[0].ParameterType == type &&
                       (param[1].ParameterType == typeof(Single) || param[1].ParameterType == typeof(Double)) && x.ReturnType == type;
            });

            return addOp != null && subOp != null && mulOp != null;
        }
    }
}
#endif
