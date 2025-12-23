#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class GenerateAssetMapEditor
{
    private static readonly string[] TargetEnumIDs = new string[]
    {
        "PrefabID"
    };

    private const string ScriptOutputPath = "Assets/Script/Data/AssetID/";
    private const string SessionStateKey = "AssetMapGenerator_PendingConfigs";
    private const string AssetRootPath = "Assets/Rcs/";

    private static readonly Dictionary<string, List<EntryToProcess>> pendingMappings = new Dictionary<string, List<EntryToProcess>>();
    private static readonly StringBuilder stringBuilder = new StringBuilder();


    [MenuItem("Tools/AssetMap/Generate All Asset Maps (Generate Code & Call Mapping)")]
    public static void GenerateAllMap()
    {
        pendingMappings.Clear();
        List<MappingConfigData> configsToSave = new List<MappingConfigData>();

        string typeName, assetDirectory;
        foreach (string enumID in TargetEnumIDs)
        {  
            typeName = enumID.Replace("ID","");
            assetDirectory = AssetRootPath + typeName;

            List<EntryToProcess> entries = GetEntriesFromAssets(assetDirectory);
            if (true == entries.Any())
            {
                // 같은 항목은 그룹화하여, 가장 첫 번째 항목만 선택 (중복으로 추가하지 않기 위함)
                entries.GroupBy(entry => entry.EnumName)
                        .Select(group => group.First())
                        .OrderBy(entry => entry.EnumName)
                        .ToList();

                GenerateEnumFile(enumID, entries);
                GenerateAssetMapFile(enumID, typeName);

                pendingMappings.Add(enumID, entries);
                configsToSave.Add(new MappingConfigData()
                {
                    EnumID = enumID,
                    AssetDirectory = assetDirectory
                });
            }
            else
            {
                Debug.LogWarning($"경고: '{assetDirectory}'에서 '{enumID}' 에셋을 찾을 수 없습니다");
            }
        }

        if (true == pendingMappings.Any())
        {
            string json = JsonUtility.ToJson(new { configs = configsToSave });
            SessionState.SetString(SessionStateKey, json);

            AssetDatabase.Refresh();
            Debug.Log($"모든 파일 생성 완료. 컴파일 및 매핑 시작을 위해 delayCall 예약. (SessionState 저장)");
            EditorApplication.delayCall += OnScriptsCompiled;
        }
        else
        {
            Debug.Log("생성할 Asset Map이 없습니다.");
            SessionState.SetString(SessionStateKey, string.Empty);
        }
    }


    private static List<EntryToProcess> GetEntriesFromAssets(string directoryPath)
    {
        List<EntryToProcess> entries = new List<EntryToProcess>();

        // 유효산 에셋 파일만 필터링
        string[] files = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(path => false == path.EndsWith(".meta"))
                        .ToArray();

        // 파일명 추출하여 entry 데이터 생성
        foreach (string filePath in files)
        {
            string unityPath = filePath.Replace('\\', '/');
            string fileName = Path.GetFileNameWithoutExtension(unityPath);
            string enumName = NormalizedFileName(fileName);

            entries.Add(new EntryToProcess()
            {
                EnumName = enumName,
                UnityAssetPath = unityPath
            });
        }

        return entries;
    }
    public static Type GetAssetType(string name)
    {
        Type enumType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asset => asset.GetTypes())
            .FirstOrDefault(type => type.Name == name);

        return enumType;
    }


    private static string NormalizedFileName(string text)
    {
        if (true == string.IsNullOrEmpty(text))
        {
            return "EMPTY";
        }

        string normalized;

        // 1. 카멜 케이스를 스네이크 케이스로 변환 (예: myAssetPrefab -> my_Asset_Prefab)
        normalized = Regex.Replace(text, "(?<=[a-z])([A-Z])", "_$1");

        // 2. 숫자 앞에 '_' 삽입 (예: Prefab1 -> Prefab_1)
        normalized = Regex.Replace(normalized, "(?<=[A-Za-z])([0-9])", "_$1");

        // 3. 텍스트를 대문자로 변환
        normalized = normalized.ToUpperInvariant();

        // 4. 허용되지 않는 문자(A-Z, 0-9, 밑줄을 제외한 모든 것)를 밑줄로 대체
        normalized = Regex.Replace(normalized, @"[^A-Z0-9_]", "_");

        // 5. 연속된 밑줄을 하나의 밑줄로 축소
        normalized = Regex.Replace(normalized, @"_+", "_");

        // 6. 이름의 시작/끝에 있는 밑줄 제거
        normalized = normalized.Trim('_');

        // 7. 숫자로 시작하는지 확인하고, 그렇다면 접두사 '_' 추가 (C# enum 규칙 준수)
        if (char.IsDigit(normalized.FirstOrDefault()))
        {
            normalized = "_" + normalized;
        }

        // 정규화 후 텍스트가 비어 있으면 기본값 반환
        if (string.IsNullOrEmpty(normalized))
        {
            return "INVALID_NAME";
        }

        return normalized;
    }


    private static void GenerateEnumFile(string enumID, List<EntryToProcess> entries)
    {
        stringBuilder.Clear();

        stringBuilder.AppendLine("// 이 파일은 EnumGenerator.cs에 의해 자동 생성되었습니다. 수동으로 편집하지 마세요.\n");
        stringBuilder.AppendLine($"public enum {enumID}");
        stringBuilder.AppendLine("{");
        stringBuilder.AppendLine("\tNONE = 0,");

        foreach (EntryToProcess entry in entries.OrderBy(e => e.EnumName))
        {
            stringBuilder.AppendLine($"\n  {entry.EnumName},");
        }

        stringBuilder.AppendLine("}");

        string fullPath = ScriptOutputPath + enumID + ".cs";
        Directory.CreateDirectory(ScriptOutputPath);

        string script = stringBuilder.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
        File.WriteAllText(fullPath, script);

        Debug.Log($"Enum 파일 생성 완료: {fullPath}");
    }
    private static void GenerateAssetMapFile(string enumID, string typeName)
    {
        string mapClassName = typeName + "AssetMap";

        stringBuilder.Clear();

        stringBuilder.AppendLine("// 이 파일은 EnumGenerator.cs에 의해 자동 생성되었습니다. 수동으로 편집하지 마세요.");
        stringBuilder.AppendLine($"// 이 클래스는 {enumID} 타입의 AssetMap 데이터를 저장하는 ScriptableObject 개체입니다.\n");
        stringBuilder.AppendLine("using UnityEngine;");
        stringBuilder.AppendLine($"public class {mapClassName} : AssetMapBase<{enumID}>");
        stringBuilder.AppendLine("{");
        stringBuilder.AppendLine("\t");
        stringBuilder.AppendLine("}");

        string fullPath = ScriptOutputPath + mapClassName + ".cs";
        Directory.CreateDirectory(ScriptOutputPath);

        string script = stringBuilder.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
        File.WriteAllText(fullPath, script);

        Debug.Log($"AssetMap 클래스 파일 생성 완료 : {fullPath}");
    }
    private static void OnScriptsCompiled()
    {
        // 도메인 리로드 후 정적 필드(PendingMappings)가 비어있다면 데이터를 복구
        if (false == pendingMappings.Any())
        {
            string json = SessionState.GetString(SessionStateKey, string.Empty);
            if (true == string.IsNullOrEmpty(json))
            {
                // SessionState에 저장된 데이터 없으면 종료
                return;
            }

            // json 데이터 복구하여 pending mappings 재구성
            Wrapper<MappingConfigData> wrapper = JsonUtility.FromJson<Wrapper<MappingConfigData>>(json);
            if (null != wrapper
                && null != wrapper.configs)
            {
                foreach (MappingConfigData config in wrapper.configs)
                {
                    // 다시 스캔하여 EntryToProcess 리스트를 만듭니다.
                    pendingMappings.Add(config.EnumID, GetEntriesFromAssets(config.AssetDirectory));
                }
            }

            // SessionState는 복구 후 초기화
            SessionState.SetString(SessionStateKey, string.Empty);
        }

        if (false == pendingMappings.Any())
        {
            return;
        }

        bool allTypesLoaded = true;
        List<string> enumIDs = pendingMappings.Keys.ToList();

        Type mapType;
        string mapClassName;
        foreach (string enumID in enumIDs)
        {
            mapClassName = enumID.Replace("ID", "") + "AssetMap";

            mapType = GetAssetType(mapClassName);
            if (null == mapType)
            {
                allTypesLoaded = false;
                break;
            }
        }

        if (true == allTypesLoaded)
        {
            Debug.Log("모든 AssetMap 클래스가 성공적으로 컴파일되었습니다. 매핑을 시작합니다.");

            foreach (var mapping in pendingMappings)
            {
                AssetMapGenerator.GenerateMap(enumID: mapping.Key, entries: mapping.Value);
            }

            pendingMappings.Clear();
            Debug.Log("--- 다중 Asset Map 생성 완료 ---");
        }
        else
        {
            // 컴파일이 완료되지 않았으므로 0.1초 뒤에 다시 시도 (재귀호출)
            EditorApplication.delayCall += OnScriptsCompiled;
        }
    }


    [Serializable]
internal struct MappingConfigData
{
    public string EnumID;
    public string AssetDirectory;  // Assets/GameData/Prefabs/Items 등
}

[Serializable]
internal class Wrapper<T>
{
    public List<T> configs;
}
}
#endif