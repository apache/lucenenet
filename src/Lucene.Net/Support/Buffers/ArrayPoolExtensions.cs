using System.Buffers;
using System.Runtime.CompilerServices;
#nullable enable

namespace Lucene.Net.Support.Buffers
{
    /// <summary>
    /// Extensions to <see cref="ArrayPool{T}"/>
    /// </summary>
    internal static class ArrayPoolExtensions
    {
        /// <summary>
        /// Returns to the pool an array that was previously obtained via <see cref="ArrayPool{T}.Rent"/> on the same
        /// <see cref="ArrayPool{T}"/> instance. This method is a no-op if <paramref name="array"/> is <c>null</c>.
        /// </summary>
        /// <param name="pool">This <see cref="ArrayPool{T}"/>.</param>
        /// <param name="array">
        /// The buffer previously obtained from <see cref="ArrayPool{T}.Rent"/> to return to the pool. If <c>null</c>,
        /// no operation will take place.
        /// </param>
        /// <param name="clearArray">
        /// If <c>true</c> and if the pool will store the buffer to enable subsequent reuse, <see cref="ReturnIfNotNull"/>
        /// will clear <paramref name="array"/> of its contents so that a subsequent consumer via <see cref="ArrayPool{T}.Rent"/>
        /// will not see the previous consumer's content.  If <c>false</c> or if the pool will release the buffer,
        /// the array's contents are left unchanged.
        /// </param>
        /// <remarks>
        /// Once a buffer has been returned to the pool, the caller gives up all ownership of the buffer
        /// and must not use it. The reference returned from a given call to <see cref="ArrayPool{T}.Rent"/> must only be
        /// returned via <see cref="ReturnIfNotNull"/> once.  The default <see cref="ArrayPool{T}"/>
        /// may hold onto the returned buffer in order to rent it again, or it may release the returned buffer
        /// if it's determined that the pool already has enough buffers stored.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReturnIfNotNull<T>(this ArrayPool<T> pool, T[]? array, bool clearArray = false)
        {
            if (array != null)
            {
                pool.Return(array, clearArray);
            }
        }
    }
}
