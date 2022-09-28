using IDictionary = Lucene.Net.Analysis.Ko.Dict.IDictionary;

namespace Lucene.Net.Analysis.Ko
{
    public class DictionaryToken : Token
    {
        private readonly int wordId;
        private readonly KoreanTokenizer.KoreanTokenizerType type;
        private readonly IDictionary dictionary;

        public DictionaryToken(KoreanTokenizer.KoreanTokenizerType type, IDictionary dictionary, int wordId, char[] surfaceForm,
                             int offset, int length, int startOffset, int endOffset)
            : base(surfaceForm, offset, length, startOffset, endOffset) {
            this.type = type;
            this.dictionary = dictionary;
            this.wordId = wordId;
        }


        public override string ToString() {
            return "DictionaryToken(\"" + GetSurfaceFormString() + "\" pos=" + GetStartOffset() + " length=" + GetLength +
                " posLen=" + GetPositionLength() + " type=" + type + " wordId=" + wordId +
                " leftID=" + dictionary.GetLeftId(wordId) + ")";
        }

        /**
        * Returns the type of this token
        * @return token type, not null
        */
        public KoreanTokenizer.KoreanTokenizerType GetType() {
            return type;
        }

        /**
        * Returns true if this token is known word
        * @return true if this token is in standard dictionary. false if not.
        */
        public bool IsKnown() {
            return type == KoreanTokenizer.KoreanTokenizerType.KNOWN;
        }

        /**
        * Returns true if this token is unknown word
        * @return true if this token is unknown word. false if not.
        */
        public bool IsUnknown() {
            return type == KoreanTokenizer.KoreanTokenizerType.UNKNOWN;
        }

        /**
        * Returns true if this token is defined in user dictionary
        * @return true if this token is in user dictionary. false if not.
        */
        public bool IsUser() {
            return type == KoreanTokenizer.KoreanTokenizerType.USER;
        }

        public override POS.Type GetPOSType()
        {
            return dictionary.GetPOSType(wordId);
        }

        public override POS.Tag GetLeftPOS()
        {
            return dictionary.GetLeftPOS(wordId);
        }
        public override POS.Tag GetRightPOS()
        {
            return dictionary.GetRightPOS(wordId);
        }
        public override string GetReading()
        {
            return dictionary.GetReading(wordId);
        }

        public override IDictionary.Morpheme[] GetMorphemes()
        {
            return dictionary.GetMorphemes(wordId, GetSurfaceForm, GetOffset, GetLength);
        }
}
}