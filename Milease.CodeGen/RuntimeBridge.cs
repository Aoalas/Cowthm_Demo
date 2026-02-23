using System;
using UnityEngine;

namespace Milease.CodeGen
{
    public static class RuntimeBridge
    {
        public static CalculateFunction<E> GetFunc<E>()
        {
            if (!GeneratedCalculation.calculateFunctions.TryGetValue(typeof(E), out var result))
            {
                result = new CalculateFunction<E>((a, b, p) => (E)GeneratedCalculation._mil_generated_calc(a, b, p));
                GeneratedCalculation.calculateFunctions.Add(typeof(E), result);
            }
            return (CalculateFunction<E>)result;
        }
        
        public static OffsetCalculateFunction<E> GetOffsetFunc<E>()
        {
            if (!GeneratedCalculation.offsetCalculateFunctions.TryGetValue(typeof(E), out var result))
            {
                result = new OffsetCalculateFunction<E>((a, b, p, o) => (E)GeneratedCalculation._mil_generated_calc_offset(a, b, p, o));
                GeneratedCalculation.offsetCalculateFunctions.Add(typeof(E), result);
            }
            return (OffsetCalculateFunction<E>)result;
        }
        
        public static DeltaCalculateFunction<E> GetDeltaFunc<E>()
        {
            if (!GeneratedCalculation.deltaCalculateFunctions.TryGetValue(typeof(E), out var result))
            {
                result = new DeltaCalculateFunction<E>((a, b) => (E)GeneratedCalculation._mil_generated_calc_delta(a, b));
                GeneratedCalculation.deltaCalculateFunctions.Add(typeof(E), result);
            }
            return (DeltaCalculateFunction<E>)result;
        }
        
        public static bool TryGetGetter<T, E>(string member, out Func<T, E> getter)
        {
            var key = (typeof(T), member);
            if (!GeneratedAccessors.getters.TryGetValue(key, out var result))
            {
                GeneratedAccessors.getters.Add(key, null);
                getter = null;
                return false; 
            }

            getter = (Func<T, E>)result;
            return result != null;
        }
        
        public static bool TryGetSetter<T, E>(string member, out Action<T, E> setter)
        {
            var key = (typeof(T), member);
            if (!GeneratedAccessors.setters.TryGetValue(key, out var result))
            {
                GeneratedAccessors.setters.Add(key, null);
                setter = null;
                return false; 
            }

            setter = (Action<T, E>)result;
            return result != null;
        }
    }
}
