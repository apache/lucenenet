using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    internal class IsolatedStorageLock : Lucene.Net.Store.Lock
    {

        System.IO.IsolatedStorage.IsolatedStorageFile _dir;
        System.IO.IsolatedStorage.IsolatedStorageFileStream _file;
        string _name;

        public IsolatedStorageLock(System.IO.IsolatedStorage.IsolatedStorageFile dir, string name)
        {
            _dir = dir;
            _name = name;

        }

        public override bool Obtain()
        {
            try
            {
                _file = _dir.CreateFile(_name);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override void Release()
        {
            _file.Close();
            _dir.DeleteFile(_name);

        }

        public override bool IsLocked()
        {
            return _dir.FileExists(_name);
        }
    }
}
