namespace Script.GamePlay.Global.Data
{
    using MessagePack;
    using MessagePack.Resolvers;
    using System;
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
            var result = new Dictionary<int, SkillData>();

            try
            {
                var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);

                // List<SkillData> 형식으로 역직렬화
                List<SkillData> skillList = MessagePackSerializer.Deserialize<List<SkillData>>(rawBytes, options);

                foreach (var skill in skillList)
                {
                    result.TryAdd(skill.SkillID, skill);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkillDataRepository] Deserialize Error: {e.Message}");
            }

            return result;
        }
    }
}