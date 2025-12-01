using Lucene.Net.Analysis.Ko.Dict;

namespace Lucene.Net.Analysis.Ko
{
    public abstract class Token
    {

        private readonly char[] surfaceForm;
        private readonly int offset;
        private readonly int length;
        private readonly int startOffset;
        private readonly int endOffset;

        private int posIncr = 1;
        private int posLen = 1;

        public Token(char[] surfaceForm, int offset, int length, int startOffset, int endOffset)
        {
            this.surfaceForm = surfaceForm;
            this.offset = offset;
            this.length = length;
            this.startOffset = startOffset;
            this.endOffset = endOffset;
        }


        /// <summary>
        /// surfaceForm
        /// </summary>
        public virtual char[] GetSurfaceForm => surfaceForm;

        /// <summary>
        /// offset into surfaceForm
        /// </summary>
        public virtual int GetOffset => offset;

        /// <summary>
        /// length of surfaceForm
        /// </summary>
        public virtual int GetLength => length;

        /// <summary>
        /// surfaceForm as a String
        /// </summary>
        /// <returns>surfaceForm as a String</returns>
        public virtual string GetSurfaceFormString()
        {
            return new string(surfaceForm, offset, length);
        }


        /**
       * Get the {@link POS.Type} of the token.
       */
        public abstract POS.Type GetPOSType();

        /**
       * Get the left part of speech of the token.
       */
        public abstract POS.Tag GetLeftPOS();

        /**
       * Get the right part of speech of the token.
       */
        public abstract POS.Tag GetRightPOS();

        /**
       * Get the reading of the token.
       */
        public abstract string GetReading();

        /**
       * Get the {@link Morpheme} decomposition of the token.
       */
        public abstract IDictionary.Morpheme[] GetMorphemes();

        /**
       * Get the start offset of the term in the analyzed text.
       */
        public virtual int GetStartOffset() {
            return startOffset;
        }

        /**
       * Get the end offset of the term in the analyzed text.
       */
        public virtual int GetEndOffset() {
            return endOffset;
        }

        public virtual void SetPositionIncrement(int posIncr) {
            this.posIncr = posIncr;
        }

        public virtual int GetPositionIncrement() {
            return posIncr;
        }

        public virtual void SetPositionLength(int posLen) {
            this.posLen = posLen;
        }

        public virtual int GetPositionLength() {
            return posLen;
        }
    }
}