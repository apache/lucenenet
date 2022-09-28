namespace Lucene.Net.Analysis.Ko.Dict
{
    public interface IDictionary
    {
        class Morpheme {
            public POS.Tag posTag;
            public char[] surfaceForm;

            public Morpheme(POS.Tag posTag, char[] surfaceForm) {
                this.posTag = posTag;
                this.surfaceForm = surfaceForm;
            }
        }

        /**
        * Get left id of specified word
        */
        int GetLeftId(int wordId);

        /**
        * Get right id of specified word
        */
        int GetRightId(int wordId);

        /**
        * Get word cost of specified word
        */
        int GetWordCost(int wordId);

        /**
        * Get the {@link Type} of specified word (morpheme, compound, inflect or pre-analysis)
        */
        POS.Type GetPOSType(int wordId);

        /**
        * Get the left {@link Tag} of specfied word.
        *
        * For {@link Type#MORPHEME} and {@link Type#COMPOUND} the left and right POS are the same.
        */
        POS.Tag GetLeftPOS(int wordId);

        /**
        * Get the right {@link Tag} of specfied word.
        *
        * For {@link Type#MORPHEME} and {@link Type#COMPOUND} the left and right POS are the same.
        */
        POS.Tag GetRightPOS(int wordId);

        /**
        * Get the reading of specified word (mainly used for Hanja to Hangul conversion).
        */
        string GetReading(int wordId);

        /**
        * Get the morphemes of specified word (e.g. 가깝으나: 가깝 + 으나).
        */
        Morpheme[] GetMorphemes(int wordId, char[] surfaceForm, int off, int len);
    }
}