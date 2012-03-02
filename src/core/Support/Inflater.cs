using System;

namespace Lucene.Net.Support
{
    public class Inflater
    {
        delegate void SetInputDelegate(byte[] buffer);
        delegate bool GetIsFinishedDelegate();
        delegate int InflateDelegate(byte[] buffer);

        SetInputDelegate setInputMethod;
        GetIsFinishedDelegate getIsFinishedMethod;
        InflateDelegate inflateMethod;

        internal Inflater(object inflaterInstance)
        {
            Type type = inflaterInstance.GetType();

            setInputMethod = (SetInputDelegate)Delegate.CreateDelegate(
                typeof(SetInputDelegate),
                inflaterInstance,
                type.GetMethod("SetInput", new Type[] { typeof(byte[]) }));

            getIsFinishedMethod = (GetIsFinishedDelegate)Delegate.CreateDelegate(
                typeof(GetIsFinishedDelegate),
                inflaterInstance,
                type.GetMethod("get_IsFinished", Type.EmptyTypes));

            inflateMethod = (InflateDelegate)Delegate.CreateDelegate(
                typeof(InflateDelegate),
                inflaterInstance,
                type.GetMethod("Inflate", new Type[] { typeof(byte[]) }));
        }

        public void SetInput(byte[] buffer)
        {
            setInputMethod(buffer);
        }

        public bool IsFinished
        {
            get { return getIsFinishedMethod(); }
        }

        public int Inflate(byte[] buffer)
        {
            return inflateMethod(buffer);
        }
    }
}