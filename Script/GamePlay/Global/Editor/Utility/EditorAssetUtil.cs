#if UNITY_EDITOR
namespace Script.GamePlay.Global.Editor.Utility
{
    using System.IO;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using MessagePack;
    using MessagePack.Resolvers;

    /// <summary>
    /// [Editor Only] C# 데이터를 MessagePack 바이너리(.bytes)로 디스크에 저장하고, Addressables 그룹 및 라벨에 자동 등록하는 순수 연산 도구<br/>
    /// 내부 상태(state)를 가지지 않는 static class;
    /// </summary>
    public static class EditorAssetUtil
    {
        /// <summary>
        /// 데이터를 바이너리 파일로 굽고, 어드레서블에 등록
        /// </summary>
        /// <typeparam name="T">직렬화할 데이터 타입</typeparam>
        /// <param name="data">저장할 실제 데이터 객체</param>
        /// <param name="relativePath">Assets/하위의 저장 경로</param>
        /// <param name="fileName">확장자를 제외한 파일명(ex."SkillTable")</param>
        /// <param name="addressableGroup">등록할 어드레서블 그룹명</param>
        /// <param name="addressableLabel">부여할 어드레서블 라벨명</param>
        public static void WriteBinaryFile<T>(T data, string relativePath, string fileName, string addressableGroup, string addressableLabel)
        {
            // 디렉토리 경로 확보
            string folderPath = Path.Combine(Application.dataPath, relativePath);
            if (false == Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // MessagePack 직렬화 및 파일 쓰기
            string fullFilePath = Path.Combine(folderPath, $"{fileName}.bytes");

            var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
            byte[] bytes = MessagePackSerializer.Serialize(data, options);

            File.WriteAllBytes(fullFilePath, bytes);

            // unity asset database 갱신
            AssetDatabase.Refresh();

            // addressables 자동 등록
            string assetPath = $"Assets/{relativePath}/{fileName}.bytes";
            RegisterToAddressables(assetPath, fileName, addressableGroup, addressableLabel);
        }

        private static void RegisterToAddressables(string assetPath, string addressaName, string groupName, string labelName)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (null == settings)
            {
                Debug.LogError("[EditorAssetUtil] AddressableAssetSettings를 찾을 수 없습니다. 어드레서블 패키지가 설치 및 세팅되었는지 확인하세요.");
                return;
            }

            // 지정한 그룹 찾기 (없으면 default 그룹 사용)
            AddressableAssetGroup targetGroup = settings.FindGroup(groupName);
            if (null == targetGroup)
            {
                Debug.LogWarning($"[EditorAssetUtil] '{groupName}' 그룹을 찾을 수 없어 Default Local Group에 등록합니다.");
                targetGroup = settings.DefaultGroup;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (true == string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[EditorAssetUtil] 에셋 GUID를 찾을 수 없습니다: {assetPath}");
                return;
            }

            // 엔트리 생성 및 이동
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, targetGroup, readOnly: false, postEvent: false);
            if (null != entry)
            {
                entry.SetAddress(addressaName);

                if (false == string.IsNullOrEmpty(labelName))
                {
                    settings.AddLabel(labelName);
                    entry.SetLabel(labelName, true, true, false);
                }

                // 변경 사항 저장
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif