using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioStream.NAudioStreamServices.Reflection_helper
{
    static class ReflectionHelperInstances
    {
        public static IEnumerable<T> CreatAllInstancesOf<T>()
        {
            return typeof(ReflectionHelper).Assembly.GetTypes()
                .Where(t => typeof(T).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract && t.IsClass)
                .Select(t => (T) Activator.CreateInstance(t));
        }
    }
}