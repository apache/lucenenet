using System.Reflection;

namespace Lucene.Net.Support
{
    public class SharpZipLib
    {
        static System.Reflection.Assembly asm = null;

        static SharpZipLib()
        {
            try
            {
                asm = Assembly.Load("ICSharpCode.SharpZipLib");
            }
            catch { }
        }

        public static Deflater CreateDeflater()
        {
            if (asm == null) throw new System.IO.FileNotFoundException("Can not load ICSharpCode.SharpZipLib.dll");
            return new Deflater(asm.CreateInstance("ICSharpCode.SharpZipLib.Zip.Compression.Deflater"));
        }

        public static Inflater CreateInflater()
        {
            if (asm == null) throw new System.IO.FileNotFoundException("Can not load ICSharpCode.SharpZipLib.dll");
            return new Inflater(asm.CreateInstance("ICSharpCode.SharpZipLib.Zip.Compression.Inflater"));
        }
    }
}