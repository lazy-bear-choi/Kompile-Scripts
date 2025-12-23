namespace Script.Manager
{
    using Script.IngameMessage;
    using Script.Interface;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

#if UNITY_EDITOR
    using UnityEngine;
#endif

    public static class MessageManager
    {
        private static readonly HashSet<IMessageReceiver> ingameReceivers = new HashSet<IMessageReceiver>();

        // 메시지 발행 시 반복할, 캐시된 리시버 배열. 리시버 목록이 변경될 때에만 갱신
        private static IMessageReceiver[] _cachedReceivers;

        // 리시버 컬렉션, 캐시된 배열에 접근하는 것을 동기화하기 위한 잠금 개체
        private static readonly object _receiverLock = new object();

        // 비동기적으로 처리될 메세지를 위한 큐
        private static readonly Queue<Func<Task>> messageQueue = new Queue<Func<Task>>();
        private static bool isProcessingQueue = false;

        /// <summary> 메시지 리시버를 매니저에 추가합니다. (스레드 안전)
        /// </summary>
        /// <param name="receiver"> 추가할 IMessageReceiver</param>
        public static void AddReceiver(IMessageReceiver receiver)
        {
            if (null == receiver)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[MessageManager] try to add null receiver.");
#endif
                return;
            }

            lock (_receiverLock)
            {
                if (false == ingameReceivers.Add(receiver))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[MessageManager] already has message receiver. ({receiver})");
#endif
                }

                RebuildCachedReceivers();
            }
        }

        /// <summary> ingameReceivers 컬렉션이 변경될 때마다 _cachedReceivers 배열을 다시 빌드
        /// </summary>
        private static void RebuildCachedReceivers()
        {
            // 방어 코드 + 필요한 배열 크기 결정
            int validReceiverCount = 0;
            foreach (IMessageReceiver receiver in ingameReceivers)
            {
                if (null != receiver)
                {
                    ++validReceiverCount;
                }
            }

            // System.Linq 쓰기 싫어서 foreach를 한 번 더 돌림;
            IMessageReceiver[] newCachedReceivers = new IMessageReceiver[validReceiverCount];
            int index = 0;
            foreach (IMessageReceiver receiver in ingameReceivers)
            {
                if (null != receiver)
                {
                    newCachedReceivers[index++] = receiver;
                }
            }

            _cachedReceivers = newCachedReceivers;
        }

        /// <summary> 등록된 모든 리시버에게 메시지를 비동기적으로 발행
        /// </summary>
        /// <param name="data"> 메시지 데이터</param>
        public static void Publish<T>(T data) where T : struct
        {
            // message가 같은 내용이 중복으로 등록될 여지가 있음 => 추후에 보고 UID를 넣던가 그럽시다...
            messageQueue.Enqueue(async() => 
            {
                IMessageReceiver[] currentReceiversSnapShot;

                lock (_receiverLock)
                {
                    currentReceiversSnapShot = _cachedReceivers;
                }

                if (null == currentReceiversSnapShot
                   || 0 == currentReceiversSnapShot.Length)
                {
                    return;
                }

                // 하나의 메시지에 대하여 + 현재 걸려있는 모든 Receiver에게 뿌린다.
                // 단, 가장 최근에 추가한 receiver부터 호출
                for (int i = currentReceiversSnapShot.Length - 1; i >= 0; --i)
                {
                    await currentReceiversSnapShot[i].ReceiveIngameMessage(data);
                }
            });

            // 함수 들어가고 난 다음에 비동기 호출. 의도한 바이다.
            ProcessQueueAsync();
        }

        /// <summary> 큐에서 메시지를 비동기적으로 처리. 단일 처리기만 활성화되도록 보장한다.
        /// </summary>
        private static async void ProcessQueueAsync()
        {
            if (true == isProcessingQueue)
            {
                return;
            }
            isProcessingQueue = true;

            while (messageQueue.Count > 0)
            {
                Func<Task> messageProcessor = messageQueue.Dequeue();

#if UNITY_EDITOR
                try
                {
                    await messageProcessor();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MessageManager] 메시지 처리 오류 ({ex.Message})");
                }
#else
                    await messageProcessor();
#endif
                await Task.Yield();
            }

            isProcessingQueue = false;
        }

        /// <summary> 메시지 리시버를 매니저에서 제거 및 해제
        /// </summary>
        public static void Dispose(IMessageReceiver receiver)
        {
#if UNITY_EDITOR
            if (true == ingameReceivers.Remove(receiver))
            {
                Debug.Log($"[MessageManager] {receiver.GetType().Name}.Dispose();");
            }
#else
            ingameReceivers.Remove(receiver);
#endif
        }
    }
}