using Lucene.Net.Index;
using System.Linq;

namespace Lucene.Net.Documents
{
    /// <summary>
    /// Extension methods to the <see cref="Document"/> class.
    /// </summary>
    public static class DocumentExtensions
    {
        /// <summary>
        /// Returns a field with the given name if any exist in this document cast to type <typeparam name="T"/>, or
        /// <c>null</c>. If multiple fields exists with this name, this method returns the
        /// first value added.
        /// <para/>
        /// LUCENENET specific
        /// </summary>
        /// <exception cref="InvalidCastException">If the field type cannot be cast to <typeparam name="T"/>.</exception>
        public static T GetField<T>(this Document document, string name) where T : IIndexableField
        {
            return (T)document.GetField(name);
        }

        /// <summary>
        /// Returns an array of <see cref="IIndexableField"/>s with the given name, cast to type <typeparam name="T"/>.
        /// This method returns an empty array when there are no
        /// matching fields. It never returns <c>null</c>.
        /// <para/>
        /// LUCENENET specific
        /// </summary>
        /// <param name="name"> the name of the field </param>
        /// <returns> a <see cref="T:IndexableField[]"/> array </returns>
        /// <exception cref="InvalidCastException">If the field type cannot be cast to <typeparam name="T"/>.</exception>
        public static T[] GetFields<T>(this Document document, string name) where T : IIndexableField
        {
            return document.GetFields(name).Cast<T>().ToArray();
        }
    }
}
