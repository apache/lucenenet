namespace Lucene.Net.Util.Fst
{
    public sealed class BytesRefFSTEnum<T> : FSTEnum<T>
        where T : class
    {
        private readonly BytesRef current = new BytesRef(10);
        private readonly InputOutput<T> result = new InputOutput<T>();
        private BytesRef target;

        public class InputOutput<T>
            where T : class
        {
            public BytesRef Input { get; set; }
            public T Output { get; set; }
        }

        public BytesRefFSTEnum(FST<T> fst)
            : base(fst)
        {
            result.Input = current;
            current.offset = 1;
        }

        public InputOutput<T> Current()
        {
            return result;
        }

        public InputOutput<T> Next()
        {
            DoNext();
            return SetResult();
        }

        public InputOutput<T> SeekCeil(BytesRef target)
        {
            this.target = target;
            targetLength = target.length;
            base.DoSeekCeil();
            return SetResult();
        }

        public InputOutput<T> SeekFloor(BytesRef target)
        {
            this.target = target;
            targetLength = target.length;
            base.DoSeekFloor();
            return SetResult();
        }

        public InputOutput<T> SeekExact(BytesRef target)
        {
            this.target = target;
            targetLength = target.length;
            if (base.DoSeekExact())
            {
                return SetResult();
            }
            else
            {
                return null;
            }
        }

        protected override int GetTargetLabel()
        {
            if (upto - 1 == target.length)
            {
                return FST<T>.END_LABEL;
            }
            else
            {
                return target.bytes[target.offset + upto - 1] & 0xFF;
            }
        }

        protected override int GetCurrentLabel()
        {
            return current.bytes[upto] & 0xFF;
        }

        protected override void SetCurrentLabel(int label)
        {
            current.bytes[upto] = (sbyte)label;
        }

        protected override void Grow()
        {
            current.bytes = ArrayUtil.Grow(current.bytes, upto + 1);
        }

        private InputOutput<T> SetResult()
        {
            if (upto == 0)
            {
                return null;
            }
            else
            {
                current.length = upto - 1;
                result.Output = output[upto];
                return result;
            }
        }

    }
}
