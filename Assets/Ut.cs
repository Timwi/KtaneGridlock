﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

using Rnd = UnityEngine.Random;

namespace Gridlock
{
    static class Ut
    {
        /// <summary>
        ///     Brings the elements of the given list into a random order.</summary>
        /// <typeparam name="T">
        ///     Type of elements in the list.</typeparam>
        /// <param name="list">
        ///     List to shuffle.</param>
        /// <returns>
        ///     The list operated on.</returns>
        public static IList<T> Shuffle<T>(this IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            for (int j = list.Count; j >= 1; j--)
            {
                int item = Rnd.Range(0, j);
                if (item < j - 1)
                {
                    var t = list[item];
                    list[item] = list[j - 1];
                    list[j - 1] = t;
                }
            }
            return list;
        }

        /// <summary>
        ///     Converts an <c>IEnumerable&lt;KeyValuePair&lt;TKey, TValue&gt;&gt;</c> into a <c>Dictionary&lt;TKey,
        ///     TValue&gt;</c>.</summary>
        /// <param name="source">
        ///     Source collection to convert to a dictionary.</param>
        /// <param name="comparer">
        ///     An optional equality comparer to compare keys.</param>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> comparer = null)
        {
            return source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, comparer ?? EqualityComparer<TKey>.Default);
        }

        public static T[] NewArray<T>(params T[] array) { return array; }

        /// <summary>
        ///     Turns all elements in the enumerable to strings and joins them using the specified <paramref
        ///     name="separator"/> and the specified <paramref name="prefix"/> and <paramref name="suffix"/> for each string.</summary>
        /// <param name="values">
        ///     The sequence of elements to join into a string.</param>
        /// <param name="separator">
        ///     Optionally, a separator to insert between each element and the next.</param>
        /// <param name="prefix">
        ///     Optionally, a string to insert in front of each element.</param>
        /// <param name="suffix">
        ///     Optionally, a string to insert after each element.</param>
        /// <param name="lastSeparator">
        ///     Optionally, a separator to use between the second-to-last and the last element.</param>
        /// <example>
        ///     <code>
        ///         // Returns "[Paris], [London], [Tokyo]"
        ///         (new[] { "Paris", "London", "Tokyo" }).JoinString(", ", "[", "]")
        ///         
        ///         // Returns "[Paris], [London] and [Tokyo]"
        ///         (new[] { "Paris", "London", "Tokyo" }).JoinString(", ", "[", "]", " and ");</code></example>
        public static string JoinString<T>(this IEnumerable<T> values, string separator = null, string prefix = null, string suffix = null, string lastSeparator = null)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (lastSeparator == null)
                lastSeparator = separator;

            using (var enumerator = values.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return "";

                // Optimise the case where there is only one element
                var one = enumerator.Current;
                if (!enumerator.MoveNext())
                    return prefix + one + suffix;

                // Optimise the case where there are only two elements
                var two = enumerator.Current;
                if (!enumerator.MoveNext())
                {
                    // Optimise the (common) case where there is no prefix/suffix; this prevents an array allocation when calling string.Concat()
                    if (prefix == null && suffix == null)
                        return one + lastSeparator + two;
                    return prefix + one + suffix + lastSeparator + prefix + two + suffix;
                }

                StringBuilder sb = new StringBuilder()
                    .Append(prefix).Append(one).Append(suffix).Append(separator)
                    .Append(prefix).Append(two).Append(suffix);
                var prev = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    sb.Append(separator).Append(prefix).Append(prev).Append(suffix);
                    prev = enumerator.Current;
                }
                sb.Append(lastSeparator).Append(prefix).Append(prev).Append(suffix);
                return sb.ToString();
            }
        }

        public static T PickRandom<T>(this IEnumerable<T> src)
        {
            if (src == null)
                throw new ArgumentNullException("src");

            var arr = (src as IList<T>) ?? src.ToArray();
            if (arr.Count == 0)
                throw new InvalidOperationException("Cannot pick a random element from an empty set.");
            return arr[Rnd.Range(0, arr.Count)];
        }

        public static int IndexOf(this IEnumerable source, Func<object, bool> predicate)
        {
            var i = 0;
            foreach (var obj in source)
            {
                if (predicate(obj))
                    return i;
                i++;
            }
            return -1;
        }
    }
}
