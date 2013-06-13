namespace Lucene.Net.Util.Fst
{
    public sealed class IntsRefFSTEnum<T> : FSTEnum<T>
        where T : class
    {
        private readonly IntsRef current = new IntsRef(10);
        private readonly InputOutput<T> result = new InputOutput<T>();
        private IntsRef target;

        public class InputOutput<T>
        {
            public IntsRef Input { get; set; }
            public T Output { get; set; }
        }

        public IntsRefFSTEnum(FST<T> fst)
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

        public InputOutput<T> SeekCeil(IntsRef target)
        {
            this.target = target;
            targetLength = target.length;
            base.DoSeekCeil();
            return SetResult();
        }

        public InputOutput<T> SeekFloor(IntsRef target)
        {
            this.target = target;
            targetLength = target.length;
            base.DoSeekFloor();
            return SetResult();
        }

        public InputOutput<T> SeekExact(IntsRef target)
        {
            this.target = target;
            targetLength = target.length;
            if (base.DoSeekExact())
            {
                // Debug.Assert(upto == 1 + target.length);
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
                return target.ints[target.offset + upto - 1];
            }
        }

        protected override int GetCurrentLabel()
        {
            return current.ints[upto];
        }

        protected override void SetCurrentLabel(int label)
        {
            current.ints[upto] = label;
        }

        protected override void Grow()
        {
            current.ints = ArrayUtil.Grow(current.ints, upto + 1);
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
