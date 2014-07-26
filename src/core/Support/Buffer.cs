namespace Lucene.Net.Support
{
    public abstract class Buffer
    {
        private int _mark = -1;
        private int _position;
        private int _capacity;
        private int _limit;

        public Buffer(int mark, int pos, int lim, int cap)
        {
            this._mark = mark;
            this._position = pos;
            this._limit = lim;
            this._capacity = cap;
        }

        public abstract object Array { get; }

        public abstract int ArrayOffset { get; }

        public virtual int Capacity
        {
            get { return _capacity; }
            set
            {
                _capacity = value;
            }
        }

        public virtual int Limit
        {
            get { return _limit; }
            set
            {
                _limit = value;

                if (_position > _limit)
                    _position = _limit;

                if (_mark > 0 && _mark > _limit)
                    _mark = -1;
            }
        }

        public virtual int Position
        {
            get { return _position; }
            set
            {
                _position = value;

                if (_mark >= 0 && _mark > _position)
                    _mark = -1;
            }
        }

        public virtual int Remaining
        {
            get { return _limit - _position; }
        }

        public abstract bool HasArray { get; }

        public abstract bool IsDirect { get; }

        public abstract bool IsReadOnly { get; }

        public virtual bool HasRemaining
        {
            get { return _limit == _position; }
        }

        public virtual int Mark
        {
            get { return _mark; }
            set
            {
                _mark = value;
            }
        }

        public Buffer Reset()
        {
            if (_mark >= 0)
                _position = _mark;

            return this;
        }

        public Buffer Clear()
        {
            _position = 0;
            _limit = _capacity;
            _mark = -1;
            return this;
        }

        public Buffer Flip()
        {
            _limit = _position;
            _position = 0;
            if (_mark >= 0)
                _mark = -1;

            return this;
        }

        public Buffer Rewind()
        {
            _position = 0;
            _mark = -1;

            return this;
        }
    }
}