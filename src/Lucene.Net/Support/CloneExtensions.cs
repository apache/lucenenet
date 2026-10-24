using System;

namespace Lucene.Net
{
    /// <summary>
    /// Extensions to clone types and cast to the proper derived type.
    /// </summary>
    public static class CloneExtensions
    {
        /// <summary>
        /// Creates a clone of the specified object.
        /// </summary>
        /// <typeparam name="T">The compile-time type of the object being cloned.</typeparam>
        /// <param name="source">The object to clone.</param>
        /// <returns>
        /// A clone of <paramref name="source"/>, with the same compile-time type.
        /// </returns>
        /// <remarks>
        /// The depth of the copy and the state shared with the original depend
        /// on the specific type being cloned. See the documentation of
        /// <typeparamref name="T"/> for type-specific details.
        /// </remarks>
        public static T Clone<T>(this T source) where T : ICloneable
        {
            return (T)source.Clone();
        }
    }
}
