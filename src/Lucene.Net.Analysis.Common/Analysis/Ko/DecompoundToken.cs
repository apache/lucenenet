using Lucene.Net.Analysis.Ko.Dict;

namespace Lucene.Net.Analysis.Ko
{
    public class DecompoundToken : Token
    {
        private readonly POS.Tag posTag;

        public DecompoundToken(POS.Tag posTag, char[] surfaceForm, int startOffset, int endOffset)
            : base(surfaceForm, 0, surfaceForm.Length, startOffset, endOffset)
        {
            this.posTag = posTag;
        }

        public override string ToString() {
            return "DecompoundToken(\"" + GetSurfaceForm + "\" pos=" + GetStartOffset() + " length=" + GetLength +
                   " startOffset=" + GetStartOffset() + " endOffset=" + GetEndOffset() + ")";
        }

        public override POS.Type GetPOSType()
        {
            return POS.Type.MORPHEME;
        }

        public override POS.Tag GetLeftPOS()
        {
            return posTag;
        }

        public override POS.Tag GetRightPOS()
        {
            return posTag;
        }

        public override string GetReading()
        {
            return null;
        }

        public override IDictionary.Morpheme[] GetMorphemes()
        {
            return null;
        }
    }
}