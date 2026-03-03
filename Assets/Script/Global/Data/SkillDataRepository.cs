namespace Script.GamePlay.Global.Data
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary> 스킬 데이터(.bytes) 비동기 로드 및 파싱, 보관을 전담 </summary>
    public class SkillDataRepository
    {
        private readonly Dictionary<int, SkillData> _cache = new Dictionary<int, SkillData>();

        public async Awaitable InitializeAsync()
        {
            ResourceRequest request = Resources.LoadAsync<TextAsset>("Data/SkillData");
            await request;

            TextAsset bytesFile = request.asset as TextAsset;
            if (null == bytesFile)
            {
                return;
            }

            byte[] rawBytes = bytesFile.bytes;

            // 백그라운드 스레드에서 파싱 연산 수행 (프리징 방지)
            await Awaitable.BackgroundThreadAsync();
            Dictionary<int, SkillData> tempDic = ParseBytes(rawBytes);

            // 메인 스레드로 복귀하여 (안전하게) 캐시에 적재
            await Awaitable.MainThreadAsync();

            _cache.Clear();
            foreach (var kvp in tempDic)
            {
                _cache.Add(kvp.Key, kvp.Value);
            }

            Resources.UnloadAsset(bytesFile);
        }
        private Dictionary<int, SkillData> ParseBytes(byte[] rawBytes)
        {
            //var result = new Dictionary<int, SkillData>();

            //using (MemoryStream stream = new MemoryStream())
            //using (BinaryReader reader = new BinaryReader(stream))
            //{
            //    try
            //    {
            //        int dataCount = reader.ReadInt32();
            //    }
            //    catch
            //    { 

            //    }
            //}
            return null;
        }
    }
}