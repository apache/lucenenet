using System.Diagnostics;
using System.IO;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Charfilter
{
    public abstract class BaseCharFilter : CharFilter
    {
        private int[] _offsets;
        private int[] _diffs;
        private int _size;

        protected BaseCharFilter(TextReader input) : base(input)
        {
        }

        protected override int Correct(int currentOff)
        {
            if (_offsets == null || currentOff < _offsets[0])
            {
                return currentOff;
            }

            var hi = _size - 1;
            if (currentOff >= _offsets[hi])
            {
                return currentOff + _diffs[hi];
            }

            var lo = 0;
            var mid = -1;

            while (hi >= lo)
            {
                mid = Number.URShift((lo + hi), 1);
                if (currentOff < _offsets[mid])
                    hi = mid - 1;
                else if (currentOff > _offsets[mid])
                    lo = mid + 1;
                else
                    return currentOff + _diffs[mid];
            }

            if (currentOff < _offsets[mid])
                return mid == 0 ? currentOff : currentOff + _diffs[mid - 1];
            else
                return currentOff + _diffs[mid];
        }

        protected int LastCumulativeDiff
        {
            get { return _offsets == null ? 0 : _diffs[_size - 1]; }
        }

        protected void AddOffCorrectMap(int off, int cumulativeDiff)
        {
            if (_offsets == null)
            {
                _offsets = new int[64];
                _diffs = new int[64];
            }
            else if (_size == _offsets.Length)
            {
                _offsets = ArrayUtil.Grow(_offsets);
                _diffs = ArrayUtil.Grow(_diffs);
            }

            Debug.Assert(_size == 0 || off >= _offsets[_size - 1],
                         string.Format("Offset #{0}({1}) is less than the last recorded offset {2}\n{3}\n{4}",
                         _size, off, _offsets[_size - 1], Arrays.ToString(_offsets), Arrays.ToString(_diffs)));

            if (_size == 0 || off != _offsets[_size - 1])
            {
                _offsets[_size] = off;
                _diffs[_size++] = cumulativeDiff;
            }
            else
            {
                _diffs[_size - 1] = cumulativeDiff;
            }
        }
    }
}
