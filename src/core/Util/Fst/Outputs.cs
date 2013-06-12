using System;

namespace Lucene.Net.Util.Fst
{
    public abstract class Outputs<T>
    {
        public abstract T Add(T prefix, T output);
        
        public abstract T Common(T output1, T output2);
        
        public abstract T GetNoOutput();
        
        public virtual T Merge(T first, T second) { throw new InvalidOperationException(); }
        
        public abstract T OutputToString(T output);
        
        public abstract T Read(DataInput dataInput);
        
        public virtual T ReadFinalOutput(DataInput dataInput) { return Read(dataInput); }
        
        public abstract T Subtract(T output, T inc);
        
        public abstract void Write(T prefix, DataOutput dataOutput);
        
        public virtual void WriteFinalOutput(T output, DataOutput dataOutput) { Write(output, dataOutput); }
    }
}
