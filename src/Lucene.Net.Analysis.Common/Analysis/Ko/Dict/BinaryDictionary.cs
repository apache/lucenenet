using J2N;
using J2N.IO;
using J2N.Numerics;
using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Reflection;
using System.Security;

namespace Lucene.Net.Analysis.Ko.Dict
{
    public abstract class BinaryDictionary : IDictionary
    {
        public enum ResourceScheme {
            CLASSPATH, FILE
        }

        public static readonly string DICT_FILENAME_SUFFIX = "$buffer.dat";
        public static readonly string TARGETMAP_FILENAME_SUFFIX = "$targetMap.dat";
        public static readonly string POSDICT_FILENAME_SUFFIX = "$posDict.dat";

        public static readonly string DICT_HEADER = "ko_dict";
        public static readonly string TARGETMAP_HEADER = "ko_dict_map";
        public static readonly string POSDICT_HEADER = "ko_dict_pos";
        public static readonly int VERSION = 1;

        private readonly ResourceScheme resourceScheme;
        private readonly string resourcePath;
        private readonly ByteBuffer buffer;
        private readonly int[] targetMapOffsets, targetMap;
        private readonly POS.Tag[] posDict;

        // LUCENENET specific - variable to hold the name of the data directory (or empty string to load embedded resources)
        private static readonly string DATA_DIR = LoadDataDir();

        // LUCENENET specific - name of the subdirectory inside of the directory where the Kuromoji dictionary files reside.
        private const string DATA_SUBDIR = "ko-data";

        private static string LoadDataDir()
        {
            // LUCENENET specific - reformatted with :, renamed from "analysis.data.dir"
            string currentPath = SystemProperties.GetProperty("ko:data:dir", AppDomain.CurrentDomain.BaseDirectory);

            // If a matching directory path is found, set our DATA_DIR static
            // variable. If it is null or empty after this process, we need to
            // load the embedded files.
            string candidatePath = System.IO.Path.Combine(currentPath, DATA_SUBDIR);
            if (System.IO.Directory.Exists(candidatePath))
            {
                return candidatePath;
            }

            while (new DirectoryInfo(currentPath).Parent != null)
            {
                try
                {
                    candidatePath = System.IO.Path.Combine(new DirectoryInfo(currentPath).Parent.FullName, DATA_SUBDIR);
                    if (System.IO.Directory.Exists(candidatePath))
                    {
                        return candidatePath;
                    }

                    currentPath = new DirectoryInfo(currentPath).Parent.FullName;
                }
                catch (SecurityException)
                {
                    // ignore security errors
                }
            }

            return null; // This is the signal to load from local resources
        }

        protected BinaryDictionary()
        {
            int[] targetMapOffsets = null, targetMap = null;
            POS.Tag[] posDict = null;
            ByteBuffer buffer; // LUCENENET: IDE0059: Remove unnecessary value assignment

            using (Stream mapIS = GetResource(TARGETMAP_FILENAME_SUFFIX))
            {
                DataInput @in = new InputStreamDataInput(mapIS);
                CodecUtil.CheckHeader(@in, TARGETMAP_HEADER, VERSION, VERSION);
                targetMap = new int[@in.ReadVInt32()];
                targetMapOffsets = new int[@in.ReadVInt32()];
                int accum = 0, sourceId = 0;
                for (int ofs = 0; ofs < targetMap.Length; ofs++)
                {
                    int val = @in.ReadVInt32();
                    if ((val & 0x01) != 0)
                    {
                        targetMapOffsets[sourceId] = ofs;
                        sourceId++;
                    }

                    accum += val.TripleShift(1);
                    targetMap[ofs] = accum;
                }

                if (sourceId + 1 != targetMapOffsets.Length)
                    throw new IOException("targetMap file format broken");
                targetMapOffsets[sourceId] = targetMap.Length;
            }

            using (Stream posIS = GetResource(POSDICT_FILENAME_SUFFIX))
            {
                DataInput @in = new InputStreamDataInput(posIS);
                CodecUtil.CheckHeader(@in, POSDICT_HEADER, VERSION, VERSION);
                int posSize = @in.ReadVInt32();
                posDict = new POS.Tag[posSize];
                for (int j = 0; j < posSize; j++)
                {
                    posDict[j] = POS.ResolveTag(@in.ReadByte());
                }
            }

            ByteBuffer tmpBuffer;

            using (Stream dictIS = GetResource(DICT_FILENAME_SUFFIX))
            {
                // no buffering here, as we load in one large buffer
                DataInput @in = new InputStreamDataInput(dictIS);
                CodecUtil.CheckHeader(@in, DICT_HEADER, VERSION, VERSION);
                int size = @in.ReadVInt32();
                tmpBuffer = ByteBuffer.Allocate(size); // AllocateDirect..?
                int read = dictIS.Read(tmpBuffer.Array, 0, size);
                if (read != size)
                {
                    throw EOFException.Create("Cannot read whole dictionary");
                }
            }

            buffer = tmpBuffer.AsReadOnlyBuffer();

            this.targetMap = targetMap;
            this.targetMapOffsets = targetMapOffsets;
            this.posDict = posDict;
            this.buffer = buffer;
        }

        protected Stream GetResource(string suffix)
        {
            return GetTypeResource(GetType(), suffix);
        }

        // util, reused by ConnectionCosts and CharacterDefinition
        public static Stream GetTypeResource(Type clazz, string suffix)
        {
            string fileName = clazz.Name + suffix;

            // LUCENENET specific: Rather than forcing the end user to recompile if they want to use a custom dictionary,
            // we load the data from the kuromoji-data directory (which can be set via the kuromoji.data.dir environment variable).
            if (string.IsNullOrEmpty(DATA_DIR))
            {
                Stream @is = clazz.FindAndGetManifestResourceStream(fileName);
                if (@is is null)
                    throw new FileNotFoundException("Not in assembly: " + clazz.FullName + suffix);
                return @is;
            }

            // We have a data directory, so first check if the file exists
            string path = System.IO.Path.Combine(DATA_DIR, fileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    string.Format(
                        "Expected file '{0}' not found. " +
                        "If the '{1}' directory exists, this file is required. " +
                        "Either remove the '{3}' directory or generate the required dictionary files using the lucene-cli tool.",
                        fileName,
                        DATA_DIR,
                        DATA_SUBDIR));
            }

            // The file exists - open a stream.
            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        public static Stream GetClassResource(Type clazz, string suffix) {
            Stream @is = clazz.FindAndGetManifestResourceStream(clazz.FullName + suffix);
            if (@is == null) {
                throw new FileNotFoundException("Not in classpath: " + clazz.FullName.Replace('.', '/') + suffix);
            }
            return @is;
        }

        private static Stream GetClassResource(string path)
        {
            Stream @is = Assembly.LoadFrom(typeof(BinaryDictionary).ToString()).
                FindAndGetManifestResourceStream(path);
            if (@is == null) {
                throw new FileNotFoundException("Not in classpath: " + path);
            }
            return @is;
        }

        public virtual void LookupWordIds(int sourceId, Int32sRef @ref)
        {
            @ref.Int32s = targetMap;
            @ref.Offset = targetMapOffsets[sourceId];
            // targetMapOffsets always has one more entry pointing behind last:
            @ref.Length = targetMapOffsets[sourceId + 1] - @ref.Offset;
        }

        public virtual int GetLeftId(int wordId)
        {
            return buffer.GetInt16(wordId).
                TripleShift(2);
        }

        public virtual int GetRightId(int wordId)
        {
            return buffer.GetInt16(wordId + 2). // Skip left id
                TripleShift(2);
        }

        public virtual int GetWordCost(int wordId)
        {
            return buffer.GetInt16(wordId + 4); // Skip left and right id
        }

        public virtual POS.Type GetPOSType(int wordId) {
            byte value = (byte) (buffer.GetInt16(wordId) & 3);
            return POS.ResolveType(value);
        }

        public virtual POS.Tag GetLeftPOS(int wordId) {
            return posDict[GetLeftId(wordId)];
        }

        public virtual POS.Tag GetRightPOS(int wordId) {
            POS.Type type = GetPOSType(wordId);
            if (type == POS.Type.MORPHEME || type == POS.Type.COMPOUND || HasSinglePOS(wordId)) {
                return GetLeftPOS(wordId);
            } else {
                byte value = buffer.Get(wordId + 6);
                return POS.ResolveTag(value);
            }
        }

        public virtual string GetReading(int wordId)
        {
            if (HasReadingData(wordId))
            {
                int offset = wordId + 6;
                return ReadString(offset);
            }

            return null;
        }

        public virtual IDictionary.Morpheme[] GetMorphemes(int wordId, char[] surfaceForm, int off, int len) {
            POS.Type posType = GetPOSType(wordId);
            if (posType == POS.Type.MORPHEME) {
                return null;
            }
            int offset = wordId + 6;
            bool hasSinglePos = HasSinglePOS(wordId);
            if (hasSinglePos == false) {
                offset++; // skip rightPOS
            }
            int length = buffer.GetInt16(offset++);
            if (length == 0) {
                return null;
            }
            IDictionary.Morpheme[] morphemes = new IDictionary.Morpheme[length];
            int surfaceOffset = 0;
            POS.Tag leftPOS = GetLeftPOS(wordId);
            for (int i = 0; i < length; i++) {
                char[] form;
                POS.Tag tag = hasSinglePos ? leftPOS : POS.ResolveTag(buffer.Get(offset++));
                if (posType == POS.Type.INFLECT) {
                    form = ReadString(offset).ToCharArray();
                    offset += form.Length * 2 + 1;
                } else {
                    int formLen = buffer.GetInt16(offset++);
                    form = new string(surfaceForm, off+surfaceOffset, formLen).ToCharArray();
                    surfaceOffset += formLen;
                }
                morphemes[i] = new IDictionary.Morpheme(tag, form);
            }
            return morphemes;
        }


        private string ReadString(int offset)
        {
            int strOffset = offset;
            int len = buffer.Get(strOffset + 1);
            char[] text = new char[len];
            for (int i = 0; i < len; i++)
            {
                text[i] = buffer.GetChar(offset + (i << 1));
            }

            return new string(text);
        }

        private bool HasSinglePOS(int wordId) {
            return (buffer.GetInt16(wordId+2) & HAS_SINGLE_POS) != 0;
        }

        private bool HasReadingData(int wordId)
        {
            return (buffer.GetInt16(wordId + 2) & HAS_READING) != 0;
        }

        /// <summary>flag that the entry has baseform data. otherwise its not inflected (same as surface form)</summary>
        public static readonly int HAS_SINGLE_POS = 1;

        /// <summary>flag that the entry has reading data. otherwise reading is surface form converted to katakana</summary>
        public static readonly int HAS_READING = 2;
    }
}