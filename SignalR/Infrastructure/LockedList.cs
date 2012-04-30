using System;
using System.Collections.Generic;
using System.Threading;

namespace SignalR.Infrastructure
{
    internal class LockedList<T> : List<T>
    {
        private readonly ReaderWriterLockSlim _listLock = new ReaderWriterLockSlim();

        public void AddWithLock(T item)
        {
            try
            {
                _listLock.EnterWriteLock();
                Add(item);
            }
            finally
            {
                _listLock.ExitWriteLock();
            }
        }

        public void RemoveWithLock(T item)
        {
            try
            {
                _listLock.EnterWriteLock();
                Remove(item);
            }
            finally
            {
                _listLock.ExitWriteLock();
            }
        }

        public void RemoveWithLock(Predicate<T> match)
        {
            try
            {
                _listLock.EnterWriteLock();

                // REVIEW: Should we only lock if there's any matches?
                RemoveAll(match);
            }
            finally
            {
                _listLock.ExitWriteLock();
            }
        }

        public List<T> CopyWithLock()
        {
            try
            {
                _listLock.EnterReadLock();
                return GetRange(0, Count);
            }
            finally
            {
                _listLock.ExitReadLock();
            }
        }

        public void CopyToWithLock(T[] array)
        {
            try
            {
                _listLock.EnterReadLock();
                CopyTo(array);
            }
            finally
            {
                _listLock.ExitReadLock();
            }
        }

        public int FindLastIndexWithLock(Predicate<T> match)
        {
            try
            {
                _listLock.EnterReadLock();
                return FindLastIndex(match);
            }
            finally
            {
                _listLock.ExitReadLock();
            }
        }

        public List<T> GetRangeWithLock(int index, int count)
        {
            try
            {
                _listLock.EnterReadLock();
                return GetRange(index, count);
            }
            finally
            {
                _listLock.ExitReadLock();
            }
        }

        public int CountWithLock
        {
            get
            {
                try
                {
                    _listLock.EnterReadLock();
                    return Count;
                }
                finally
                {
                    _listLock.ExitReadLock();
                }
            }
        }

        public T GetWithLock(int index)
        {
            try
            {
                _listLock.EnterReadLock();
                return this[index];
            }
            finally
            {
                _listLock.ExitReadLock();
            }
        }

        public void SetWithLock(int index, T value)
        {
            try
            {
                _listLock.EnterWriteLock();
                this[index] = value;
            }
            finally
            {
                _listLock.EnterWriteLock();
            }
        }
    }
}
