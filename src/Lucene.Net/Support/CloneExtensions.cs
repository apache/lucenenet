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

        /// <summary>
        /// Specifies that a type is cloneable and requires covariant casting
        /// via the <see cref="Clone{T}(T)"/> extension method.
        /// </summary>
        /// <remarks>
        /// This interface is nested because it has no purpose other than to provide
        /// a constraint for types to expose a strongly typed covariant return type.
        /// We also don't want it to collide with <see cref="System.ICloneable"/>.
        /// </remarks>
        public interface ICloneable
        {
            /// <summary>
            /// Creates a new object that is a copy of the current instance.
            /// </summary>
            /// <returns>The cloned instance.</returns>
            object Clone();
        }
    }
}
