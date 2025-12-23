using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;

namespace Script.Manager
{
    public class InstanceEntry
    {
        public readonly AsyncOperationHandle<GameObject> Handle;
        public readonly Queue<GameObject> Pool;
        public readonly bool UsePooling;
        private int _refCount;

        public InstanceEntry(AsyncOperationHandle<GameObject> handle, bool usePooling)
        {
            Handle = handle;
            UsePooling = usePooling;
            _refCount = 0;
            Pool = (true == usePooling) ? new Queue<GameObject>() : null;
        }

        public void AddReference()
        {
            ++_refCount;
        }
        public void RemoveReference()
        {
            _refCount = System.Math.Max(0, _refCount - 1);
        }
        public bool HasPooledInstance()
        {
            if (false == UsePooling)
            {
                return false;
            }

            return Pool != null && Pool.Count > 0;
        }
        public bool ShouldRelease()
        {
            // 참조 카운트가 0보다 크면 언제나 false;
            if (_refCount > 0)
            {
                return false;
            }

            // 여기서부터 참조 카운트 == 0;
            // 풀링을 사용하지 않는다면 return true;
            if (false == UsePooling)
            {
                return true;
            }

            // 풀링을 사용한다면 Count 확인 (null 체크는 방어 코드)
            return Pool == null || Pool.Count == 0;
        }
    }
}