namespace FxttMonitorNotifier.Droid.Extensions
{
    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class JsonEqualityComparer<T> : IEqualityComparer<T>
    {
        public bool Equals(T firstObject, T secondObject) 
            => String.Equals(JsonConvert.SerializeObject(firstObject), JsonConvert.SerializeObject(secondObject));

        public int GetHashCode(T @object) => JsonConvert.SerializeObject(@object).GetHashCode();
    }

    public static partial class LinqExtensions
    {
        public static IEnumerable<T> ExceptUsingJsonComparer<T>(this IEnumerable<T> first, IEnumerable<T> second)
            => first.Except(second, new JsonEqualityComparer<T>());
    }
}