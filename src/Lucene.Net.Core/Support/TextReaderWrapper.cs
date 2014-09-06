using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public class TextReaderWrapper : TextReader
    {
        private TextReader tr;

        public TextReaderWrapper(TextReader tr)
        {
            this.tr = tr;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            int numRead = tr.Read(buffer, index, count);
            return numRead == 0 ? -1 : numRead;
        }

    }
}
