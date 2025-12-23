namespace Script.Util
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public class x_SortedList<TKey, TValue> where TKey : IComparable<TKey>
    {
        private readonly List<TKey>           keys = new List<TKey>();
        private readonly List<TValue>         values = new List<TValue>();
        private readonly ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim();

        // 데이터 추가 (동시성 제어 포함)
        public void Add(TKey key, TValue value)
        {
            lockSlim.EnterWriteLock();
            try
            {
                var index = keys.BinarySearch(key);
                if (index >= 0)
                {
                    throw new ArgumentException("An item with the same key already exists.");
                }

                index = ~index; // 삽입 위치 계산
                keys.Insert(index, key);
                values.Insert(index, value);
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }

        // 값 가져오기 (읽기 잠금)
        public TValue GetValue(TKey key)
        {
            lockSlim.EnterReadLock();
            try
            {
                int index = keys.BinarySearch(key);
                if (index < 0)
                {
                    throw new KeyNotFoundException("The given key was not found.");
                }

                return values[index];
            }
            finally
            {
                lockSlim.ExitReadLock();
            }
        }

        // 키 존재 여부 확인 (읽기 잠금)
        public bool ContainsKey(TKey key)
        {
            lockSlim.EnterReadLock();
            try
            {
                return keys.BinarySearch(key) >= 0;
            }
            finally
            {
                lockSlim.ExitReadLock();
            }
        }

        // 데이터 삭제 (쓰기 잠금)
        public bool Remove(TKey key)
        {
            lockSlim.EnterWriteLock();
            try
            {
                int index = keys.BinarySearch(key);
                if (index < 0) return false;

                keys.RemoveAt(index);
                values.RemoveAt(index);
                return true;
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }

        // 키 및 값 접근 (읽기 전용)
        public IReadOnlyList<TKey> Keys
        {
            get
            {
                lockSlim.EnterReadLock();
                try
                {
                    return keys.AsReadOnly();
                }
                finally
                {
                    lockSlim.ExitReadLock();
                }
            }
        }

        public IReadOnlyList<TValue> Values
        {
            get
            {
                lockSlim.EnterReadLock();
                try
                {
                    return values.AsReadOnly();
                }
                finally
                {
                    lockSlim.ExitReadLock();
                }
            }
        }

        // 데이터 초기화 (쓰기 잠금)
        public void Clear()
        {
            lockSlim.EnterWriteLock();
            try
            {
                keys.Clear();
                values.Clear();
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
        }
    }
}