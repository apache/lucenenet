using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Extensions to <see cref="Queue{T}"/>
    /// </summary>
    internal static class QueueExtensions
    {
#if !FEATURE_QUEUE_TRYDEQUEUE_TRYPEEK
        /// <summary>
        /// Removes the object at the beginning of the <see cref="Queue{T}"/>,
        /// and copies it to the <paramref name="result"/> parameter.
        /// </summary>
        /// <typeparam name="T">The type of element in the <see cref="Queue{T}"/></typeparam>
        /// <param name="queue">The <see cref="Queue{T}"/> to be checked</param>
        /// <param name="result">The removed object</param>
        /// <returns><c>true</c> if the object was successfully removed; <c>false</c> if the <see cref="Queue{T}"/> is empty.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryDequeue<T>(this Queue<T> queue, out T result)
        {
            if (queue is null)
                throw new ArgumentNullException(nameof(queue));

            if (queue.Count > 0)
            {
                result = queue.Dequeue();
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Returns a value that indicates whether there is an object at the beginning of the <see cref="Queue{T}"/>,
        /// and if one is present, copies it to the <paramref name="result"/> parameter. The object is not removed from the <see cref="Queue{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of element in the <see cref="Queue{T}"/></typeparam>
        /// <param name="queue">The <see cref="Queue{T}"/> to be checked</param>
        /// <param name="result">If present, the object at the beginning of the <see cref="Queue{T}"/>; otherwise, the default value of <typeparamref name="T"/></param>
        /// <returns><c>true</c> if there is an object at the beginning of the <see cref="Queue{T}"/>; <c>false</c> if the <see cref="Queue{T}"/> is empty.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryPeek<T>(this Queue<T> queue, out T result)
        {
            if (queue is null)
                throw new ArgumentNullException(nameof(queue));

            if (queue.Count > 0)
            {
                result = queue.Peek();
                return true;
            }

            result = default;
            return false;
        }
#endif
    }
}
