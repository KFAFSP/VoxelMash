using System;
using System.Collections.Generic;

namespace VoxelMash
{
    public static class EnumerableHelper
    {
        /// <summary>
        /// Wraps this object instance into an IEnumerable&lt;T&gt; consisting of a single item.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="AItem">The instance that will be wrapped.</param>
        /// <returns>An IEnumerable&lt;T&gt; consisting of a single item.</returns>
        /// <remarks>
        /// Source: http://stackoverflow.com/q/1577822
        /// </remarks>
        public static IEnumerable<T> Yield<T>(this T AItem)
        {
            yield return AItem;
        }

        /// <summary>
        /// Executes a foreach over the specified enumeration with the given action.
        /// </summary>
        /// <typeparam name="T">The enumeration object type.</typeparam>
        /// <param name="AEnumerable">The enumeration.</param>
        /// <param name="AAction">The action that will be executed.</param>
        public static void ForEach<T>(this IEnumerable<T> AEnumerable, Action<T> AAction)
        {
            foreach (T oItem in AEnumerable)
                AAction(oItem);
        }
    }
}
