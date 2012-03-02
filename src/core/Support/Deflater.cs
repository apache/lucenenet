using System;

namespace Lucene.Net.Support
{
    public class Deflater
    {
        delegate void SetLevelDelegate(int level);
        delegate void SetInputDelegate(byte[] input, int offset, int count);
        delegate void FinishDelegate();
        delegate bool GetIsFinishedDelegate();
        delegate int DeflateDelegate(byte[] output);

        SetLevelDelegate setLevelMethod;
        SetInputDelegate setInputMethod;
        FinishDelegate finishMethod;
        GetIsFinishedDelegate getIsFinishedMethod;
        DeflateDelegate deflateMethod;

        public const int BEST_COMPRESSION = 9;

        internal Deflater(object deflaterInstance)
        {
            Type type = deflaterInstance.GetType();

            setLevelMethod = (SetLevelDelegate)Delegate.CreateDelegate(
                typeof(SetLevelDelegate),
                deflaterInstance,
                type.GetMethod("SetLevel", new Type[] { typeof(int) }));

            setInputMethod = (SetInputDelegate)Delegate.CreateDelegate(
                typeof(SetInputDelegate),
                deflaterInstance,
                type.GetMethod("SetInput", new Type[] { typeof(byte[]), typeof(int), typeof(int) }));

            finishMethod = (FinishDelegate)Delegate.CreateDelegate(
                typeof(FinishDelegate),
                deflaterInstance,
                type.GetMethod("Finish", Type.EmptyTypes));

            getIsFinishedMethod = (GetIsFinishedDelegate)Delegate.CreateDelegate(
                typeof(GetIsFinishedDelegate),
                deflaterInstance,
                type.GetMethod("get_IsFinished", Type.EmptyTypes));

            deflateMethod = (DeflateDelegate)Delegate.CreateDelegate(
                typeof(DeflateDelegate),
                deflaterInstance,
                type.GetMethod("Deflate", new Type[] { typeof(byte[]) }));
        }

        public void SetLevel(int level)
        {
            setLevelMethod(level);
        }

        public void SetInput(byte[] input, int offset, int count)
        {
            setInputMethod(input, offset, count);
        }

        public void Finish()
        {
            finishMethod();
        }

        public bool IsFinished
        {
            get { return getIsFinishedMethod(); }
        }

        public int Deflate(byte[] output)
        {
            return deflateMethod(output);
        }
    }
}