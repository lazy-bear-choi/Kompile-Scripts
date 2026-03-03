namespace Script.GamePlay.Global.Data
{
    using UnityEngine;
    using Script.GamePlay.Global.Asset;

    /// <summary> 데이터 및 에셋 저장소에 전역 접근을 제공 </summary>
    public class Repository
    {
        // -- 정적 데이터 저장소 --
        public static SkillDataRepository Skill { get; private set; }

        // -- 동적 데이터 저장소 --
        public static AssetRepository Asset { get; private set; }

        // --
        private static bool _isInitialized = false;

        public static async Awaitable InitializeAllAsync()
        {
            if (true == _isInitialized)
            {
                return;
            }

            Skill = new SkillDataRepository();
            Asset = new AssetRepository();

            // 순차적으로 비동기 대기를 하여 안전하게 로드
            await Skill.InitializeAsync();

            _isInitialized = true;
        }

        public static bool CheckInitialization()
        {
            return _isInitialized;
        }
    }
}