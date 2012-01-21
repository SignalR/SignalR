using System;
using System.Collections.Generic;
using System.Threading;

namespace SignalR.Infrastructure
{
    internal class LockedList<T>
    {
        private readonly List<T> _list = new List<T>();
        private readonly ReaderWriterLockSlim _listLock = new ReaderWriterLockSlim();

        public void Add(T item)
        {
            try
            {
                _listLock.EnterWriteLock();
                _list.Add(item);
            }
            finally
            {
                _listLock.ExitWriteLock();
            }
        }

        public void Remove(T item)
        {
            try
            {
                _listLock.EnterWriteLock();
                _list.Remove(item);
            }
            finally
            {
                _listLock.ExitWriteLock();
            }
        }

        public List<T> Copy()
        {
            try
            {
                _listLock.EnterReadLock();
                return _list.GetRange(0, _list.Count);
            }
            finally
            {
                _listLock.ExitReadLock();
            }
        }

        public void CopyTo(T[] array)
        {
            try
            {
                _listLock.EnterReadLock();
                _list.CopyTo(array);
            }
            finally
            {
                _listLock.ExitReadLock();
            }
        }

        public int FindLastIndex(Predicate<T> match)
        {
            try
            {
                _listLock.EnterReadLock();
                return _list.FindLastIndex(match);
            }
            finally
            {
                _listLock.ExitReadLock();
            }
        }

        public int FindLastIndexLockFree(Predicate<T> match)
        {
            return _list.FindLastIndex(match);
        }

        public List<T> GetRange(int index, int count)
        {
            try
            {
                _listLock.EnterReadLock();
                return _list.GetRange(index, count);
            }
            finally
            {
                _listLock.ExitReadLock();
            }
        }

        public List<T> GetRangeLockFree(int index, int count)
        {
            return _list.GetRange(index, count);
        }

        public int Count
        {
            get
            {
                try
                {
                    _listLock.EnterReadLock();
                    return _list.Count;
                }
                finally
                {
                    _listLock.ExitReadLock();
                }
            }
        }

        public List<T> List
        {
            get { return _list; }
        }

        public T this[int index]
        {
            get
            {
                try
                {
                    _listLock.EnterReadLock();
                    return _list[index];
                }
                finally
                {
                    _listLock.ExitReadLock();
                }
            }
            set
            {
                try
                {
                    _listLock.EnterWriteLock();
                    _list[index] = value;
                }
                finally
                {
                    _listLock.ExitWriteLock();
                }
            }
        }
    }
}
