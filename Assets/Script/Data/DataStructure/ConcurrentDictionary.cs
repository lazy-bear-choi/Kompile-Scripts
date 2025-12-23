namespace Script.Data
{
    using MessagePack;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    [MessagePackObject]
    public partial class ConcurrentDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> 
    {
        [Serializable]
        private class ReaderWriterLock
        {
            private int _readers = 0; // 현재 읽는 중인 스레드 수
            private int _writers = 0; // 쓰는 중인 스레드 수
            private readonly object _lock = new object();

            public void EnterReadLock()
            {
                lock (_lock)
                {
                    while (_writers > 0) // 쓰기 중이면 대기
                    {
                        Monitor.Wait(_lock);
                    }
                    _readers++;
                }
            }

            public void ExitReadLock()
            {
                lock (_lock)
                {
                    _readers--;
                    if (_readers == 0)
                    {
                        Monitor.PulseAll(_lock); // 대기 중인 쓰기 스레드 깨우기
                    }
                }
            }

            public void EnterWriteLock()
            {
                lock (_lock)
                {
                    while (_writers > 0 || _readers > 0) // 읽기/쓰기 중이면 대기
                    {
                        Monitor.Wait(_lock);
                    }
                    _writers++;
                }
            }

            public void ExitWriteLock()
            {
                lock (_lock)
                {
                    _writers--;
                    Monitor.PulseAll(_lock); // 대기 중인 읽기/쓰기 스레드 깨우기
                }
            }
        }

        private readonly Dictionary<TKey, TValue> _dictionary;
        private readonly ReaderWriterLock _lock = new ReaderWriterLock();

        public ConcurrentDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }
        public ConcurrentDictionary(Dictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        IEnumerator IEnumerable.GetEnumerator()
        { 
            return GetEnumerator();
        }

        public Dictionary<TKey, TValue>.KeyCollection Keys
        {
            get
            {
                return _dictionary.Keys;
            }
        }
        public Dictionary<TKey, TValue>.ValueCollection Values
        {
            get
            {
                return _dictionary.Values;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            _lock.EnterReadLock();
            try
            {
                return _dictionary.TryGetValue(key, out value);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            _lock.EnterReadLock();
            try
            {
                if (_dictionary.TryGetValue(key, out var value))
                    return value;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            _lock.EnterWriteLock();
            try
            {
                // 다시 확인 (다른 스레드가 추가했을 수 있음)
                if (_dictionary.TryGetValue(key, out var value))
                {
                    return value;
                }

                value = valueFactory(key);
                _dictionary[key] = value;

                return value;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public TValue this[TKey key]
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _dictionary[key];
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            set
            {
                _lock.EnterWriteLock();
                try
                {
                    _dictionary[key] = value;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            _lock.EnterReadLock();
            try
            {
                // 내부 딕셔너리를 복사하여 반복
                foreach (var kvp in _dictionary)
                {
                    yield return kvp;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool TryAdd(TKey key, TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                return _dictionary.TryAdd(key, value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        public bool TryRemove(TKey key, out TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                return _dictionary.Remove(key, out value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        public bool TryRemove(TKey key)
        {
            _lock.EnterWriteLock();
            try
            {
                return _dictionary.Remove(key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _dictionary.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

    }
}