using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Standard
{
    internal class ClassicTokenizerImpl : IStandardTokenizerInterface
    {
        /** This character denotes the end of file */
        public const int YYEOF = -1;

        /** initial size of the lookahead buffer */
        private const int ZZ_BUFFERSIZE = 4096;

        /** lexical states */
        public const int YYINITIAL = 0;

        /**
        * ZZ_LEXSTATE[l] is the state in the DFA for the lexical state l
        * ZZ_LEXSTATE[l+1] is the state in the DFA for the lexical state l
        *                  at the beginning of a line
        * l is of the form l = 2*k, k a non negative integer
        */
        private readonly int[] ZZ_LEXSTATE = { 
            0, 0
        };

        /** 
        * Translates characters to character classes
        */
        private const string ZZ_CMAP_PACKED =
          "\u0009\0\u0001\0\u0001\u000d\u0001\0\u0001\0\u0001\u000c\u0012\0\u0001\0\u0005\0\u0001\u0005" +
          "\u0001\u0003\u0004\0\u0001\u0009\u0001\u0007\u0001\u0004\u0001\u0009\u000a\u0002\u0006\0\u0001\u0006\u001a\u000a" +
          "\u0004\0\u0001\u0008\u0001\0\u001a\u000a\u002f\0\u0001\u000a\u000a\0\u0001\u000a\u0004\0\u0001\u000a" +
          "\u0005\0\u0017\u000a\u0001\0\u001f\u000a\u0001\0\u0128\u000a\u0002\0\u0012\u000a\u001c\0\u005e\u000a" +
          "\u0002\0\u0009\u000a\u0002\0\u0007\u000a\u000e\0\u0002\u000a\u000e\0\u0005\u000a\u0009\0\u0001\u000a" +
          "\u008b\0\u0001\u000a\u000b\0\u0001\u000a\u0001\0\u0003\u000a\u0001\0\u0001\u000a\u0001\0\u0014\u000a" +
          "\u0001\0\u002c\u000a\u0001\0\u0008\u000a\u0002\0\u001a\u000a\u000c\0\u0082\u000a\u000a\0\u0039\u000a" +
          "\u0002\0\u0002\u000a\u0002\0\u0002\u000a\u0003\0\u0026\u000a\u0002\0\u0002\u000a\u0037\0\u0026\u000a" +
          "\u0002\0\u0001\u000a\u0007\0\u0027\u000a\u0048\0\u001b\u000a\u0005\0\u0003\u000a\u002e\0\u001a\u000a" +
          "\u0005\0\u000b\u000a\u0015\0\u000a\u0002\u0007\0\u0063\u000a\u0001\0\u0001\u000a\u000f\0\u0002\u000a" +
          "\u0009\0\u000a\u0002\u0003\u000a\u0013\0\u0001\u000a\u0001\0\u001b\u000a\u0053\0\u0026\u000a\u015f\0" +
          "\u0035\u000a\u0003\0\u0001\u000a\u0012\0\u0001\u000a\u0007\0\u000a\u000a\u0004\0\u000a\u0002\u0015\0" +
          "\u0008\u000a\u0002\0\u0002\u000a\u0002\0\u0016\u000a\u0001\0\u0007\u000a\u0001\0\u0001\u000a\u0003\0" +
          "\u0004\u000a\u0022\0\u0002\u000a\u0001\0\u0003\u000a\u0004\0\u000a\u0002\u0002\u000a\u0013\0\u0006\u000a" +
          "\u0004\0\u0002\u000a\u0002\0\u0016\u000a\u0001\0\u0007\u000a\u0001\0\u0002\u000a\u0001\0\u0002\u000a" +
          "\u0001\0\u0002\u000a\u001f\0\u0004\u000a\u0001\0\u0001\u000a\u0007\0\u000a\u0002\u0002\0\u0003\u000a" +
          "\u0010\0\u0007\u000a\u0001\0\u0001\u000a\u0001\0\u0003\u000a\u0001\0\u0016\u000a\u0001\0\u0007\u000a" +
          "\u0001\0\u0002\u000a\u0001\0\u0005\u000a\u0003\0\u0001\u000a\u0012\0\u0001\u000a\u000f\0\u0001\u000a" +
          "\u0005\0\u000a\u0002\u0015\0\u0008\u000a\u0002\0\u0002\u000a\u0002\0\u0016\u000a\u0001\0\u0007\u000a" +
          "\u0001\0\u0002\u000a\u0002\0\u0004\u000a\u0003\0\u0001\u000a\u001e\0\u0002\u000a\u0001\0\u0003\u000a" +
          "\u0004\0\u000a\u0002\u0015\0\u0006\u000a\u0003\0\u0003\u000a\u0001\0\u0004\u000a\u0003\0\u0002\u000a" +
          "\u0001\0\u0001\u000a\u0001\0\u0002\u000a\u0003\0\u0002\u000a\u0003\0\u0003\u000a\u0003\0\u0008\u000a" +
          "\u0001\0\u0003\u000a\u002d\0\u0009\u0002\u0015\0\u0008\u000a\u0001\0\u0003\u000a\u0001\0\u0017\u000a" +
          "\u0001\0\u000a\u000a\u0001\0\u0005\u000a\u0026\0\u0002\u000a\u0004\0\u000a\u0002\u0015\0\u0008\u000a" +
          "\u0001\0\u0003\u000a\u0001\0\u0017\u000a\u0001\0\u000a\u000a\u0001\0\u0005\u000a\u0024\0\u0001\u000a" +
          "\u0001\0\u0002\u000a\u0004\0\u000a\u0002\u0015\0\u0008\u000a\u0001\0\u0003\u000a\u0001\0\u0017\u000a" +
          "\u0001\0\u0010\u000a\u0026\0\u0002\u000a\u0004\0\u000a\u0002\u0015\0\u0012\u000a\u0003\0\u0018\u000a" +
          "\u0001\0\u0009\u000a\u0001\0\u0001\u000a\u0002\0\u0007\u000a\u0039\0\u0001\u0001\u0030\u000a\u0001\u0001" +
          "\u0002\u000a\u000c\u0001\u0007\u000a\u0009\u0001\u000a\u0002\u0027\0\u0002\u000a\u0001\0\u0001\u000a\u0002\0" +
          "\u0002\u000a\u0001\0\u0001\u000a\u0002\0\u0001\u000a\u0006\0\u0004\u000a\u0001\0\u0007\u000a\u0001\0" +
          "\u0003\u000a\u0001\0\u0001\u000a\u0001\0\u0001\u000a\u0002\0\u0002\u000a\u0001\0\u0004\u000a\u0001\0" +
          "\u0002\u000a\u0009\0\u0001\u000a\u0002\0\u0005\u000a\u0001\0\u0001\u000a\u0009\0\u000a\u0002\u0002\0" +
          "\u0002\u000a\u0022\0\u0001\u000a\u001f\0\u000a\u0002\u0016\0\u0008\u000a\u0001\0\u0022\u000a\u001d\0" +
          "\u0004\u000a\u0074\0\u0022\u000a\u0001\0\u0005\u000a\u0001\0\u0002\u000a\u0015\0\u000a\u0002\u0006\0" +
          "\u0006\u000a\u004a\0\u0026\u000a\u000a\0\u0027\u000a\u0009\0\u005a\u000a\u0005\0\u0044\u000a\u0005\0" +
          "\u0052\u000a\u0006\0\u0007\u000a\u0001\0\u003f\u000a\u0001\0\u0001\u000a\u0001\0\u0004\u000a\u0002\0" +
          "\u0007\u000a\u0001\0\u0001\u000a\u0001\0\u0004\u000a\u0002\0\u0027\u000a\u0001\0\u0001\u000a\u0001\0" +
          "\u0004\u000a\u0002\0\u001f\u000a\u0001\0\u0001\u000a\u0001\0\u0004\u000a\u0002\0\u0007\u000a\u0001\0" +
          "\u0001\u000a\u0001\0\u0004\u000a\u0002\0\u0007\u000a\u0001\0\u0007\u000a\u0001\0\u0017\u000a\u0001\0" +
          "\u001f\u000a\u0001\0\u0001\u000a\u0001\0\u0004\u000a\u0002\0\u0007\u000a\u0001\0\u0027\u000a\u0001\0" +
          "\u0013\u000a\u000e\0\u0009\u0002\u002e\0\u0055\u000a\u000c\0\u026c\u000a\u0002\0\u0008\u000a\u000a\0" +
          "\u001a\u000a\u0005\0\u004b\u000a\u0095\0\u0034\u000a\u002c\0\u000a\u0002\u0026\0\u000a\u0002\u0006\0" +
          "\u0058\u000a\u0008\0\u0029\u000a\u0557\0\u009c\u000a\u0004\0\u005a\u000a\u0006\0\u0016\u000a\u0002\0" +
          "\u0006\u000a\u0002\0\u0026\u000a\u0002\0\u0006\u000a\u0002\0\u0008\u000a\u0001\0\u0001\u000a\u0001\0" +
          "\u0001\u000a\u0001\0\u0001\u000a\u0001\0\u001f\u000a\u0002\0\u0035\u000a\u0001\0\u0007\u000a\u0001\0" +
          "\u0001\u000a\u0003\0\u0003\u000a\u0001\0\u0007\u000a\u0003\0\u0004\u000a\u0002\0\u0006\u000a\u0004\0" +
          "\u000d\u000a\u0005\0\u0003\u000a\u0001\0\u0007\u000a\u0082\0\u0001\u000a\u0082\0\u0001\u000a\u0004\0" +
          "\u0001\u000a\u0002\0\u000a\u000a\u0001\0\u0001\u000a\u0003\0\u0005\u000a\u0006\0\u0001\u000a\u0001\0" +
          "\u0001\u000a\u0001\0\u0001\u000a\u0001\0\u0004\u000a\u0001\0\u0003\u000a\u0001\0\u0007\u000a\u0ecb\0" +
          "\u0002\u000a\u002a\0\u0005\u000a\u000a\0\u0001\u000b\u0054\u000b\u0008\u000b\u0002\u000b\u0002\u000b\u005a\u000b" +
          "\u0001\u000b\u0003\u000b\u0006\u000b\u0028\u000b\u0003\u000b\u0001\0\u005e\u000a\u0011\0\u0018\u000a\u0038\0" +
          "\u0010\u000b\u0100\0\u0080\u000b\u0080\0\u19b6\u000b\u000a\u000b\u0040\0\u51a6\u000b\u005a\u000b\u048d\u000a" +
          "\u0773\0\u2ba4\u000a\u215c\0\u012e\u000b\u00d2\u000b\u0007\u000a\u000c\0\u0005\u000a\u0005\0\u0001\u000a" +
          "\u0001\0\u000a\u000a\u0001\0\u000d\u000a\u0001\0\u0005\u000a\u0001\0\u0001\u000a\u0001\0\u0002\u000a" +
          "\u0001\0\u0002\u000a\u0001\0\u006c\u000a\u0021\0\u016b\u000a\u0012\0\u0040\u000a\u0002\0\u0036\u000a" +
          "\u0028\0\u000c\u000a\u0074\0\u0003\u000a\u0001\0\u0001\u000a\u0001\0\u0087\u000a\u0013\0\u000a\u0002" +
          "\u0007\0\u001a\u000a\u0006\0\u001a\u000a\u000a\0\u0001\u000b\u003a\u000b\u001f\u000a\u0003\0\u0006\u000a" +
          "\u0002\0\u0006\u000a\u0002\0\u0006\u000a\u0002\0\u0003\u000a\u0023\0";

        /** 
        * Translates characters to character classes
        */
        private static readonly char[] ZZ_CMAP = zzUnpackCMap(ZZ_CMAP_PACKED);

        /** 
         * Translates DFA states to action switch labels.
         */
        private static readonly int[] ZZ_ACTION = zzUnpackAction();

        private const String ZZ_ACTION_PACKED_0 =
        "\u0001\0\u0001\u0001\u0003\u0002\u0001\u0003\u0001\u0001\u000b\0\u0001\u0002\u0003\u0004" +
        "\u0002\0\u0001\u0005\u0001\0\u0001\u0005\u0003\u0004\u0006\u0005\u0001\u0006\u0001\u0004" +
        "\u0002\u0007\u0001\u0008\u0001\0\u0001\u0008\u0003\0\u0002\u0008\u0001\u0009\u0001\u000a" +
        "\u0001\u0004";

        private static int[] zzUnpackAction()
        {
            int[] result = new int[51];
            int offset = 0;
            offset = zzUnpackAction(ZZ_ACTION_PACKED_0, offset, result);
            return result;
        }

        private static int zzUnpackAction(String packed, int offset, int[] result)
        {
            int i = 0;       /* index in packed string  */
            int j = offset;  /* index in unpacked array */
            int l = packed.Length;
            while (i < l)
            {
                int count = packed[i++];
                int value = packed[i++];
                do result[j++] = value; while (--count > 0);
            }
            return j;
        }

        /** 
        * Translates a state to a row index in the transition table
        */
        private static readonly int[] ZZ_ROWMAP = zzUnpackRowMap();

        private const String ZZ_ROWMAP_PACKED_0 =
        "\0\0\0\u000e\0\u001c\0\u002a\0\u0038\0\u000e\0\u0046\0\u0054" +
        "\0\u0062\0\u0070\0\u007e\0\u008c\0\u009a\0\u00a8\0\u00b6\0\u00c4" +
        "\0\u00d2\0\u00e0\0\u00ee\0\u00fc\0\u010a\0\u0118\0\u0126\0\u0134" +
        "\0\u0142\0\u0150\0\u015e\0\u016c\0\u017a\0\u0188\0\u0196\0\u01a4" +
        "\0\u01b2\0\u01c0\0\u01ce\0\u01dc\0\u01ea\0\u01f8\0\u00d2\0\u0206" +
        "\0\u0214\0\u0222\0\u0230\0\u023e\0\u024c\0\u025a\0\u0054\0\u008c" +
        "\0\u0268\0\u0276\0\u0284";

        private static int[] zzUnpackRowMap()
        {
            int[] result = new int[51];
            int offset = 0;
            offset = zzUnpackRowMap(ZZ_ROWMAP_PACKED_0, offset, result);
            return result;
        }

        private static int zzUnpackRowMap(String packed, int offset, int[] result)
        {
            int i = 0;  /* index in packed string  */
            int j = offset;  /* index in unpacked array */
            int l = packed.Length;
            while (i < l)
            {
                int high = packed[i++] << 16;
                result[j++] = high | packed[i++];
            }
            return j;
        }

        /** 
        * The transition table of the DFA
        */
        private static readonly int[] ZZ_TRANS = zzUnpackTrans();

        private const String ZZ_TRANS_PACKED_0 =
        "\u0001\u0002\u0001\u0003\u0001\u0004\u0007\u0002\u0001\u0005\u0001\u0006\u0001\u0007\u0001\u0002" +
        "\u000f\0\u0002\u0003\u0001\0\u0001\u0008\u0001\0\u0001\u0009\u0002\u000a\u0001\u000b" +
        "\u0001\u0003\u0004\0\u0001\u0003\u0001\u0004\u0001\0\u0001\u000c\u0001\0\u0001\u0009" +
        "\u0002\u000d\u0001\u000e\u0001\u0004\u0004\0\u0001\u0003\u0001\u0004\u0001\u000f\u0001\u0010" +
        "\u0001\u0011\u0001\u0012\u0002\u000a\u0001\u000b\u0001\u0013\u0010\0\u0001\u0002\u0001\0" +
        "\u0001\u0014\u0001\u0015\u0007\0\u0001\u0016\u0004\0\u0002\u0017\u0007\0\u0001\u0017" +
        "\u0004\0\u0001\u0018\u0001\u0019\u0007\0\u0001\u001a\u0005\0\u0001\u001b\u0007\0" +
        "\u0001\u000b\u0004\0\u0001\u001c\u0001\u001d\u0007\0\u0001\u001e\u0004\0\u0001\u001f" +
        "\u0001\u0020\u0007\0\u0001\u0021\u0004\0\u0001\u0022\u0001\u0023\u0007\0\u0001\u0024" +
        "\u000d\0\u0001\u0025\u0004\0\u0001\u0014\u0001\u0015\u0007\0\u0001\u0026\u000d\0" +
        "\u0001\u0027\u0004\0\u0002\u0017\u0007\0\u0001\u0028\u0004\0\u0001\u0003\u0001\u0004" +
        "\u0001\u000f\u0001\u0008\u0001\u0011\u0001\u0012\u0002\u000a\u0001\u000b\u0001\u0013\u0004\0" +
        "\u0002\u0014\u0001\0\u0001\u0029\u0001\0\u0001\u0009\u0002\u002a\u0001\0\u0001\u0014" +
        "\u0004\0\u0001\u0014\u0001\u0015\u0001\0\u0001\u002b\u0001\0\u0001\u0009\u0002\u002c" +
        "\u0001\u002d\u0001\u0015\u0004\0\u0001\u0014\u0001\u0015\u0001\0\u0001\u0029\u0001\0" +
        "\u0001\u0009\u0002\u002a\u0001\0\u0001\u0016\u0004\0\u0002\u0017\u0001\0\u0001\u002e" +
        "\u0002\0\u0001\u002e\u0002\0\u0001\u0017\u0004\0\u0002\u0018\u0001\0\u0001\u002a" +
        "\u0001\0\u0001\u0009\u0002\u002a\u0001\0\u0001\u0018\u0004\0\u0001\u0018\u0001\u0019" +
        "\u0001\0\u0001\u002c\u0001\0\u0001\u0009\u0002\u002c\u0001\u002d\u0001\u0019\u0004\0" +
        "\u0001\u0018\u0001\u0019\u0001\0\u0001\u002a\u0001\0\u0001\u0009\u0002\u002a\u0001\0" +
        "\u0001\u001a\u0005\0\u0001\u001b\u0001\0\u0001\u002d\u0002\0\u0003\u002d\u0001\u001b" +
        "\u0004\0\u0002\u001c\u0001\0\u0001\u002f\u0001\0\u0001\u0009\u0002\u000a\u0001\u000b" +
        "\u0001\u001c\u0004\0\u0001\u001c\u0001\u001d\u0001\0\u0001\u0030\u0001\0\u0001\u0009" +
        "\u0002\u000d\u0001\u000e\u0001\u001d\u0004\0\u0001\u001c\u0001\u001d\u0001\0\u0001\u002f" +
        "\u0001\0\u0001\u0009\u0002\u000a\u0001\u000b\u0001\u001e\u0004\0\u0002\u001f\u0001\0" +
        "\u0001\u000a\u0001\0\u0001\u0009\u0002\u000a\u0001\u000b\u0001\u001f\u0004\0\u0001\u001f" +
        "\u0001\u0020\u0001\0\u0001\u000d\u0001\0\u0001\u0009\u0002\u000d\u0001\u000e\u0001\u0020" +
        "\u0004\0\u0001\u001f\u0001\u0020\u0001\0\u0001\u000a\u0001\0\u0001\u0009\u0002\u000a" +
        "\u0001\u000b\u0001\u0021\u0004\0\u0002\u0022\u0001\0\u0001\u000b\u0002\0\u0003\u000b" +
        "\u0001\u0022\u0004\0\u0001\u0022\u0001\u0023\u0001\0\u0001\u000e\u0002\0\u0003\u000e" +
        "\u0001\u0023\u0004\0\u0001\u0022\u0001\u0023\u0001\0\u0001\u000b\u0002\0\u0003\u000b" +
        "\u0001\u0024\u0006\0\u0001\u000f\u0006\0\u0001\u0025\u0004\0\u0001\u0014\u0001\u0015" +
        "\u0001\0\u0001\u0031\u0001\0\u0001\u0009\u0002\u002a\u0001\0\u0001\u0016\u0004\0" +
        "\u0002\u0017\u0001\0\u0001\u002e\u0002\0\u0001\u002e\u0002\0\u0001\u0028\u0004\0" +
        "\u0002\u0014\u0007\0\u0001\u0014\u0004\0\u0002\u0018\u0007\0\u0001\u0018\u0004\0" +
        "\u0002\u001c\u0007\0\u0001\u001c\u0004\0\u0002\u001f\u0007\0\u0001\u001f\u0004\0" +
        "\u0002\u0022\u0007\0\u0001\u0022\u0004\0\u0002\u0032\u0007\0\u0001\u0032\u0004\0" +
        "\u0002\u0014\u0007\0\u0001\u0033\u0004\0\u0002\u0032\u0001\0\u0001\u002e\u0002\0" +
        "\u0001\u002e\u0002\0\u0001\u0032\u0004\0\u0002\u0014\u0001\0\u0001\u0031\u0001\0" +
        "\u0001\u0009\u0002\u002a\u0001\0\u0001\u0014\u0003\0";

        private static int[] zzUnpackTrans()
        {
            int[] result = new int[658];
            int offset = 0;
            offset = zzUnpackTrans(ZZ_TRANS_PACKED_0, offset, result);
            return result;
        }

        private static int zzUnpackTrans(String packed, int offset, int[] result)
        {
            int i = 0;       /* index in packed string  */
            int j = offset;  /* index in unpacked array */
            int l = packed.Length;
            while (i < l)
            {
                int count = packed[i++];
                int value = packed[i++];
                value--;
                do result[j++] = value; while (--count > 0);
            }
            return j;
        }

        /* error codes */
        private const int ZZ_UNKNOWN_ERROR = 0;
        private const int ZZ_NO_MATCH = 1;
        private const int ZZ_PUSHBACK_2BIG = 2;

        /* error messages for the codes above */
        private static readonly String[] ZZ_ERROR_MSG = {
        "Unkown internal scanner error",
        "Error: could not match input",
        "Error: pushback value was too large"
        };

        /**
        * ZZ_ATTRIBUTE[aState] contains the attributes of state <code>aState</code>
        */
        private static readonly int[] ZZ_ATTRIBUTE = zzUnpackAttribute();

        private const String ZZ_ATTRIBUTE_PACKED_0 =
        "\u0001\0\u0001\u0009\u0003\u0001\u0001\u0009\u0001\u0001\u000b\0\u0004\u0001\u0002\0" +
        "\u0001\u0001\u0001\0\u000f\u0001\u0001\0\u0001\u0001\u0003\0\u0005\u0001";

        private static int[] zzUnpackAttribute()
        {
            int[] result = new int[51];
            int offset = 0;
            offset = zzUnpackAttribute(ZZ_ATTRIBUTE_PACKED_0, offset, result);
            return result;
        }

        private static int zzUnpackAttribute(String packed, int offset, int[] result)
        {
            int i = 0;       /* index in packed string  */
            int j = offset;  /* index in unpacked array */
            int l = packed.Length;
            while (i < l)
            {
                int count = packed[i++];
                int value = packed[i++];
                do result[j++] = value; while (--count > 0);
            }
            return j;
        }

        /** the input device */
        private TextReader zzReader;

        /** the current state of the DFA */
        private int zzState;

        /** the current lexical state */
        private int zzLexicalState = YYINITIAL;

        /** this buffer contains the current text to be matched and is
        the source of the yytext() string */
        private char[] zzBuffer = new char[ZZ_BUFFERSIZE];

        /** the textposition at the last accepting state */
        private int zzMarkedPos;

        /** the current text position in the buffer */
        private int zzCurrentPos;

        /** startRead marks the beginning of the yytext() string in the buffer */
        private int zzStartRead;

        /** endRead marks the last character in the buffer, that has been read
        from input */
        private int zzEndRead;

        /** number of newlines encountered up to the start of the matched text */
        private int yyline;

        /** the number of characters up to the start of the matched text */
        private int yychar;

        /**
        * the number of characters from the last newline up to the start of the 
        * matched text
        */
        private int yycolumn;

        /** 
        * zzAtBOL == true <=> the scanner is currently at the beginning of a line
        */
        private bool zzAtBOL = true;

        /** zzAtEOF == true <=> the scanner is at the EOF */
        private bool zzAtEOF;

        /** denotes if the user-EOF-code has already been executed */
        private bool zzEOFDone;


        /* user code: */

        public const int ALPHANUM = StandardTokenizer.ALPHANUM;
        public const int APOSTROPHE = StandardTokenizer.APOSTROPHE;
        public const int ACRONYM = StandardTokenizer.ACRONYM;
        public const int COMPANY = StandardTokenizer.COMPANY;
        public const int EMAIL = StandardTokenizer.EMAIL;
        public const int HOST = StandardTokenizer.HOST;
        public const int NUM = StandardTokenizer.NUM;
        public const int CJ = StandardTokenizer.CJ;
        public const int ACRONYM_DEP = StandardTokenizer.ACRONYM_DEP;

        public static readonly String[] TOKEN_TYPES = StandardTokenizer.TOKEN_TYPES;

        public int YYChar
        {
            get { return yychar; }
        }

        public void GetText(Tokenattributes.ICharTermAttribute t)
        {
            t.CopyBuffer(zzBuffer, zzStartRead, zzMarkedPos - zzStartRead);
        }

        /**
        * Creates a new scanner
        * There is also a java.io.InputStream version of this constructor.
        *
        * @param   in  the java.io.Reader to read input from.
        */
        internal ClassicTokenizerImpl(TextReader input)
        {
            this.zzReader = input;
        }

        private static char[] zzUnpackCMap(String packed)
        {
            char[] map = new char[0x10000];
            int i = 0;  /* index in packed string  */
            int j = 0;  /* index in unpacked array */
            while (i < 1154)
            {
                int count = packed[i++];
                char value = packed[i++];
                do map[j++] = value; while (--count > 0);
            }
            return map;
        }

        private bool zzRefill()
        {

            /* first: make room (if you can) */
            if (zzStartRead > 0)
            {
                Array.Copy(zzBuffer, zzStartRead,
                                 zzBuffer, 0,
                                 zzEndRead - zzStartRead);

                /* translate stored positions */
                zzEndRead -= zzStartRead;
                zzCurrentPos -= zzStartRead;
                zzMarkedPos -= zzStartRead;
                zzStartRead = 0;
            }

            /* is the buffer big enough? */
            if (zzCurrentPos >= zzBuffer.Length)
            {
                /* if not: blow it up */
                char[] newBuffer = new char[zzCurrentPos * 2];
                Array.Copy(zzBuffer, 0, newBuffer, 0, zzBuffer.Length);
                zzBuffer = newBuffer;
            }

            /* finally: fill the buffer with new input */
            int numRead = zzReader.Read(zzBuffer, zzEndRead,
                                                    zzBuffer.Length - zzEndRead);

            if (numRead > 0)
            {
                zzEndRead += numRead;
                return false;
            }
            // unlikely but not impossible: read 0 characters, but not at end of stream    
            if (numRead == 0)
            {
                int c = zzReader.Read();
                if (c <= 0)
                {
                    return true;
                }
                else
                {
                    zzBuffer[zzEndRead++] = (char)c;
                    return false;
                }
            }

            // numRead < 0
            return true;
        }

        public void yyclose()
        {
            zzAtEOF = true;            /* indicate end of file */
            zzEndRead = zzStartRead;  /* invalidate buffer    */

            if (zzReader != null)
                zzReader.Close();
        }

        public void YYReset(TextReader reader)
        {
            zzReader = reader;
            zzAtBOL = true;
            zzAtEOF = false;
            zzEOFDone = false;
            zzEndRead = zzStartRead = 0;
            zzCurrentPos = zzMarkedPos = 0;
            yyline = yychar = yycolumn = 0;
            zzLexicalState = YYINITIAL;
            if (zzBuffer.Length > ZZ_BUFFERSIZE)
                zzBuffer = new char[ZZ_BUFFERSIZE];
        }

        public int yystate()
        {
            return zzLexicalState;
        }

        public void yybegin(int newState)
        {
            zzLexicalState = newState;
        }

        public String yytext()
        {
            return new String(zzBuffer, zzStartRead, zzMarkedPos - zzStartRead);
        }

        public char yycharat(int pos)
        {
            return zzBuffer[zzStartRead + pos];
        }

        public int YYLength
        {
            get { return zzMarkedPos - zzStartRead; }
        }

        private void zzScanError(int errorCode)
        {
            String message;
            try
            {
                message = ZZ_ERROR_MSG[errorCode];
            }
            catch (IndexOutOfRangeException e)
            {
                message = ZZ_ERROR_MSG[ZZ_UNKNOWN_ERROR];
            }

            throw new Exception(message);
        }

        public void yypushback(int number)
        {
            if (number > YYLength)
                zzScanError(ZZ_PUSHBACK_2BIG);

            zzMarkedPos -= number;
        }

        public int GetNextToken()
        {
            int zzInput;
            int zzAction;

            // cached fields:
            int zzCurrentPosL;
            int zzMarkedPosL;
            int zzEndReadL = zzEndRead;
            char[] zzBufferL = zzBuffer;
            char[] zzCMapL = ZZ_CMAP;

            int[] zzTransL = ZZ_TRANS;
            int[] zzRowMapL = ZZ_ROWMAP;
            int[] zzAttrL = ZZ_ATTRIBUTE;

            while (true)
            {
                zzMarkedPosL = zzMarkedPos;

                yychar += zzMarkedPosL - zzStartRead;

                zzAction = -1;

                zzCurrentPosL = zzCurrentPos = zzStartRead = zzMarkedPosL;

                zzState = ZZ_LEXSTATE[zzLexicalState];

                // set up zzAction for empty match case:
                int zzAttributes = zzAttrL[zzState];
                if ((zzAttributes & 1) == 1)
                {
                    zzAction = zzState;
                }


            //zzForAction:
                {
                    while (true)
                    {

                        if (zzCurrentPosL < zzEndReadL)
                            zzInput = zzBufferL[zzCurrentPosL++];
                        else if (zzAtEOF)
                        {
                            zzInput = YYEOF;
                            break;
                        }
                        else
                        {
                            // store back cached positions
                            zzCurrentPos = zzCurrentPosL;
                            zzMarkedPos = zzMarkedPosL;
                            bool eof = zzRefill();
                            // get translated positions and possibly new buffer
                            zzCurrentPosL = zzCurrentPos;
                            zzMarkedPosL = zzMarkedPos;
                            zzBufferL = zzBuffer;
                            zzEndReadL = zzEndRead;
                            if (eof)
                            {
                                zzInput = YYEOF;
                                break;
                            }
                            else
                            {
                                zzInput = zzBufferL[zzCurrentPosL++];
                            }
                        }
                        int zzNext = zzTransL[zzRowMapL[zzState] + zzCMapL[zzInput]];
                        if (zzNext == -1) break;
                        zzState = zzNext;

                        zzAttributes = zzAttrL[zzState];
                        if ((zzAttributes & 1) == 1)
                        {
                            zzAction = zzState;
                            zzMarkedPosL = zzCurrentPosL;
                            if ((zzAttributes & 8) == 8) break;
                        }

                    }
                }

                // store back cached position
                zzMarkedPos = zzMarkedPosL;

                switch (zzAction < 0 ? zzAction : ZZ_ACTION[zzAction])
                {
                    case 1:
                        { /* Break so we don't hit fall-through warning: */
                            break;/* ignore */
                        }
                    case 11: break;
                    case 2:
                        {
                            return ALPHANUM;
                        }
                    case 12: break;
                    case 3:
                        {
                            return CJ;
                        }
                    case 13: break;
                    case 4:
                        {
                            return HOST;
                        }
                    case 14: break;
                    case 5:
                        {
                            return NUM;
                        }
                    case 15: break;
                    case 6:
                        {
                            return APOSTROPHE;
                        }
                    case 16: break;
                    case 7:
                        {
                            return COMPANY;
                        }
                    case 17: break;
                    case 8:
                        {
                            return ACRONYM_DEP;
                        }
                    case 18: break;
                    case 9:
                        {
                            return ACRONYM;
                        }
                    case 19: break;
                    case 10:
                        {
                            return EMAIL;
                        }
                    case 20: break;
                    default:
                        if (zzInput == YYEOF && zzStartRead == zzCurrentPos)
                        {
                            zzAtEOF = true;
                            return YYEOF;
                        }
                        else
                        {
                            zzScanError(ZZ_NO_MATCH);
                        }
                        break;
                }
            }
        }
    }
}
