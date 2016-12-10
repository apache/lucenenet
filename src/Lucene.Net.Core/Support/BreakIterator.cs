using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// The <code>BreakIterator</code> class implements methods for finding
    /// the location of boundaries in text. Instances of <code>BreakIterator</code>
    /// maintain a current position and scan over text
    /// returning the index of characters where boundaries occur.
    /// Internally, <code>BreakIterator</code> scans text using a
    /// <code>CharacterIterator</code>, and is thus able to scan text held
    /// by any object implementing that protocol. A <code>StringCharacterIterator</code>
    /// is used to scan <code>String</code> objects passed to <code>SetText</code>.
    /// </summary>
    // LUCENENET TODO: Continue documentation...
    public abstract class BreakIterator : ICloneable
    {
        /// <summary>
        /// Constructor. BreakIterator is stateless and has no default behavior.
        /// </summary>
        protected BreakIterator()
        {
        }

        /// <summary>
        /// Create a copy of this iterator
        /// </summary>
        /// <returns>A member-wise copy of this</returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// DONE is returned by Previous(), Next(), Next(int), Preceding(int)
        /// and Following(int) when either the first or last text boundary has been
        /// reached.
        /// </summary>
        public static readonly int DONE = -1;

        /// <summary>
        /// Returns the first boundary. The iterator's current position is set
        /// to the first text boundary.
        /// </summary>
        /// <returns>The character index of the first text boundary</returns>
        public abstract int First();

        /// <summary>
        /// Returns the last boundary. The iterator's current position is set
        /// to the last text boundary.
        /// </summary>
        /// <returns>The character index of the last text boundary.</returns>
        public abstract int Last();

        /// <summary>
        /// Returns the nth boundary from the current boundary. If either
        /// the first or last text boundary has been reached, it returns
        /// <see cref="BreakIterator.DONE"/> and the current position is set to either
        /// the first or last text boundary depending on which one is reached. Otherwise,
        /// the iterator's current position is set to the new boundary.
        /// For example, if the iterator's current position is the mth text boundary
        /// and three more boundaries exist from the current boundary to the last text
        /// boundary, the Next(2) call will return m + 2. The new text position is set
        /// to the (m + 2)th text boundary. A Next(4) call would return
        /// <see cref="BreakIterator.DONE"/> and the last text boundary would become the
        /// new text position.
        /// </summary>
        /// <param name="n">
        /// which boundary to return.  A value of 0
        /// does nothing.  Negative values move to previous boundaries
        /// and positive values move to later boundaries.
        /// </param>
        /// <returns>
        /// The character index of the nth boundary from the current position
        /// or <see cref="BreakIterator.DONE"/> if either first or last text boundary
        /// has been reached.
        /// </returns>
        public abstract int Next(int n);

        /// <summary>
        /// Returns the boundary following the current boundary. If the current boundary
        /// is the last text boundary, it returns <c>BreakIterator.DONE</c> and
        /// the iterator's current position is unchanged. Otherwise, the iterator's
        /// current position is set to the boundary following the current boundary.
        /// </summary>
        /// <returns>
        /// The character index of the next text boundary or
        /// <see cref="BreakIterator.DONE"/> if the current boundary is the last text
        /// boundary.
        /// Equivalent to Next(1).
        /// </returns>
        /// <seealso cref="Next(int)"/>
        public abstract int Next();

        /// <summary>
        /// Returns the boundary preceding the current boundary. If the current boundary
        /// is the first text boundary, it returns <code>BreakIterator.DONE</code> and
        /// the iterator's current position is unchanged. Otherwise, the iterator's
        /// current position is set to the boundary preceding the current boundary.
        /// </summary>
        /// <returns>
        /// The character index of the previous text boundary or
        /// <see cref="BreakIterator.DONE"/> if the current boundary is the first text
        /// boundary.
        /// </returns>
        public abstract int Previous();

        /// <summary>
        /// Returns the first boundary following the specified character offset. If the
        /// specified offset equals to the last text boundary, it returns
        /// <see cref="BreakIterator.DONE"/> and the iterator's current position is unchanged.
        /// Otherwise, the iterator's current position is set to the returned boundary.
        /// The value returned is always greater than the offset or the value
        /// <see cref="BreakIterator.DONE"/>.
        /// </summary>
        /// <param name="offset">the character offset to begin scanning.</param>
        /// <returns>
        /// The first boundary after the specified offset or
        /// <see cref="BreakIterator.DONE"/> if the last text boundary is passed in
        /// as the offset.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// if the specified offset is less than
        /// the first text boundary or greater than the last text boundary.
        /// </exception>
        public abstract int Following(int offset);

        /// <summary>
        /// Returns the last boundary preceding the specified character offset. If the
        /// specified offset equals to the first text boundary, it returns
        /// <see cref="BreakIterator.DONE"/> and the iterator's current position is unchanged.
        /// Otherwise, the iterator's current position is set to the returned boundary.
        /// The value returned is always less than the offset or the value
        /// <see cref="BreakIterator.DONE"/>.
        /// </summary>
        /// <param name="offset">the character offset to begin scanning.</param>
        /// <returns>
        /// The last boundary before the specified offset or
        /// <see cref="BreakIterator.DONE"/> if the first text boundary is passed in
        /// as the offset.
        /// </returns>
        public virtual int Preceding(int offset)
        {
            // NOTE:  This implementation is here solely because we can't add new
            // abstract methods to an existing class.  There is almost ALWAYS a
            // better, faster way to do this.
            int pos = Following(offset);
            while (pos >= offset && pos != DONE)
            {
                pos = Previous();
            }
            return pos;
        }

        /// <summary>
        /// Returns true if the specified character offset is a text boundary.
        /// </summary>
        /// <param name="offset">the character offset to check.</param>
        /// <returns><c>true</c> if "offset" is a boundary position, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentException">
        /// if the specified offset is less than
        /// the first text boundary or greater than the last text boundary.
        /// </exception>
        public bool IsBoundary(int offset)
        {
            // NOTE: This implementation probably is wrong for most situations
            // because it fails to take into account the possibility that a
            // CharacterIterator passed to setText() may not have a begin offset
            // of 0.  But since the abstract BreakIterator doesn't have that
            // knowledge, it assumes the begin offset is 0.  If you subclass
            // BreakIterator, copy the SimpleTextBoundary implementation of this
            // function into your subclass.  [This should have been abstract at
            // this level, but it's too late to fix that now.]
            if (offset == 0)
            {
                return true;
            }
            int boundary = Following(offset - 1);
            if (boundary == DONE)
            {
                throw new ArgumentException();
            }
            return boundary == offset;
        }

        /// <summary>
        /// Returns character index of the text boundary that was most
        /// recently returned by Next(), Next(int), Previous(), First(), Last(),
        /// Following(int) or Preceding(int). If any of these methods returns
        /// <see cref="BreakIterator.DONE"/> because either first or last text boundary
        /// has been reached, it returns the first or last text boundary depending on
        /// which one is reached.
        /// </summary>
        /// <returns>
        /// The text boundary returned from the above methods, first or last
        /// text boundary.
        /// </returns>
        /// <seealso cref="Next()"/>
        /// <seealso cref="Next(int)"/>
        /// <seealso cref="Previous()"/>
        /// <seealso cref="First()"/>
        /// <seealso cref="Last()"/>
        /// <seealso cref="Following(int)"/>
        /// <seealso cref="Preceding(int)"/>
        public abstract int Current { get; }

        /// <summary>
        /// Get the text being scanned
        /// </summary>
        /// <returns>the text being scanned</returns>
        public abstract CharacterIterator GetText();

        /// <summary>
        /// Set a new text string to be scanned.  The current scan
        /// position is reset to First().
        /// </summary>
        /// <param name="newText">new text to scan.</param>
        public abstract void SetText(string newText);


        ///// <summary>
        ///// Set a new text string to be scanned.  The current scan
        ///// position is reset to First().
        ///// </summary>
        ///// <param name="newText">new text to scan.</param>
        //public virtual void SetText(string newText)
        //{
        //    SetText(new StringCharacterIterator(newText));
        //    //throw new NotImplementedException();
        //}

        ///// <summary>
        ///// Set a new text for scanning.  The current scan
        ///// position is reset to First().
        ///// </summary>
        ///// <param name="newText">new text to scan.</param>
        //public abstract void SetText(CharacterIterator newText);


        // LUCENENET TODO:
        //private static readonly int CHARACTER_INDEX = 0;
        //private static readonly int WORD_INDEX = 1;
        //private static readonly int LINE_INDEX = 2;
        //private static readonly int SENTENCE_INDEX = 3;


        //private static readonly SoftReference<BreakIteratorCache>[] iterCache = (SoftReference<BreakIteratorCache>[])new SoftReference<?>[4];

        ///// <summary>
        ///// Returns a new <see cref="BreakIterator"/> instance
        ///// for word breaks
        ///// for the current culture.
        ///// </summary>
        ///// <returns>A break iterator for word breaks</returns>
        //public static BreakIterator GetWordInstance()
        //{
        //    return GetWordInstance(CultureInfo.CurrentCulture);
        //}


    }
}
