using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

[Serializable]
public class EntryToProcess
{
    public string EnumName;       // enum 자료형 이름
    public string UnityAssetPath; // [Directory] Assets/를 포함한 전체 경로
}

public static class AssetMapGenerator
{
    private const string ASSET_MAP_PATH = "Assets/Resources/AssetMap";
    private const string ENTRIES = "entries";
    public static List<EntryToProcess> EntrieToProcess = new List<EntryToProcess>();

    public static void GenerateMap(string enumID, List<EntryToProcess> entries)
    {
        // 1. AssetMap ScriptableObject 인스턴스를 가져오거나 생성
        string typeName = enumID.Replace("ID", "");
        if (false == TryGetOrNewMap(typeName, out ScriptableObject map))
        {
            Debug.LogError($"Can`t find or create map: {enumID}");
            return;
        }

        // 2. Enum Type 로드 (데이터 파싱에 사용)
        Type enumType = GenerateAssetMapEditor.GetAssetType(enumID);
        if (null == enumType)
        {
            Debug.LogError($"Enum 타입 '{enumID}'를 찾을 수 없습니다.");
            return;
        }

        // 3. SerializedObject를 통하여 데이터 필드에 접근
        SerializedObject serializedObject = new SerializedObject(map);
        SerializedProperty entriesProperty = serializedObject.FindProperty(ENTRIES);

        // 4. 데이터 순회 및 SerializedProperty에 값 할당
        int successCount = 0;
        object enumValue;

        // 빠른 탐색을 위하여 Dictionary 생성
        SerializedProperty element;
        int idValue;

        Dictionary<int, int> existingMap = new Dictionary<int, int>();
        for (int i = 0; i < entriesProperty.arraySize; ++i)
        {
            element = entriesProperty.GetArrayElementAtIndex(i);
            idValue = element.FindPropertyRelative("Id").enumValueIndex;

            if (false == existingMap.ContainsKey(idValue))
            {
                existingMap.Add(idValue, i);
            }
        }

        foreach (var entry in entries.OrderBy(e => e.EnumName))
        {
            // 4-1. Enum 파싱 (파일 이름 -> 실제 Enum 값)
            try
            {
                enumValue = Enum.Parse(enumType, entry.EnumName);
            }
            catch (ArgumentException)
            {
                Debug.LogWarning($"[Warning] Enum '{enumID}'에 '{entry.EnumName}'이 정의되지 않았습니다.");
                continue;
            }

            int targetIdValue = (int)enumValue;

            // 4-2. Addressables 주소 조회
            if (false == TryGetAddress(entry.UnityAssetPath, out string addressKey))
            {
                Debug.LogWarning($"경고: 에셋 '{entry.UnityAssetPath}'는 Addressables에 등록되지 않았거나 GUID가 없습니다. 매핑 제외.");
                continue;
            }

            // 4-3. SerializedProperty 배열에 데이터 추가
            if (true == existingMap.TryGetValue(targetIdValue, out int existingIndex))
            {
                // 중복 발견 -> 기존 항목을 덮어씀
                element = entriesProperty.GetArrayElementAtIndex(existingIndex);
            }
            else
            {
                // 새로운 항목 -> 배열 끝에 추가
                entriesProperty.InsertArrayElementAtIndex(entriesProperty.arraySize);
                element = entriesProperty.GetArrayElementAtIndex(entriesProperty.arraySize - 1);
            }

            // AssetEntry의 필드에 값을 할당
            element.FindPropertyRelative("Id").enumValueIndex = (int)enumValue;
            element.FindPropertyRelative("AddressKey").stringValue = addressKey;

            ++successCount;
        }

        // 5. 변경 사항을 적용 및 저장
        serializedObject.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        Debug.Log($"{enumID} 매핑 : {successCount} / {entries.Count} 성공");
    }
    private static bool TryGetOrNewMap(string typeName, out ScriptableObject map)
    {
        map = null;

        string mapClassName = typeName + "AssetMap";
        string mapAssetPath = $"{ASSET_MAP_PATH}/{mapClassName}.asset";


        Type mapType = GenerateAssetMapEditor.GetAssetType(mapClassName);
        if (null == mapType)
        {
            Debug.LogError($"AssetMap 클래스 타입 '{mapClassName}'을 찾을 수 없습니다. 해당 스크립트가 컴파일되었는지 확인하세요.");
            return false;
        }

        // 로드 시 mapType을 사용하고 ScriptableObject로 캐스팅 (컴파일 오류 방지)
        map = AssetDatabase.LoadAssetAtPath(mapAssetPath, mapType) as ScriptableObject;

        if (null == map)
        {
            // 디렉토리 생성
            string directoryPath = Path.GetDirectoryName(mapAssetPath);
            if (false == Directory.Exists(directoryPath))
            {
                string fullDirectoryPath = Application.dataPath.Replace("Assets", "") + directoryPath;
                Directory.CreateDirectory(fullDirectoryPath);
                AssetDatabase.Refresh();
            }

            // mapType 클래스로 인스턴스 생성
            map = ScriptableObject.CreateInstance(mapType);
            AssetDatabase.CreateAsset(map, mapAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"새로운 Asset Map '{mapClassName}'을 생성 ({mapAssetPath})");
        }

        return true;
    }
    private static bool TryGetAddress(string assetPath, out string address)
    {
        address = null;

        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (true == string.IsNullOrEmpty(guid))
        {
            Debug.LogError($"Addressable Asset를 찾을 수 없습니다 ({assetPath})");
            return false;
        }

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (null == settings)
        {
            Debug.LogError($"Addressable Settings를 찾을 수 없습니다 ({assetPath})");
            return false;
        }

        AddressableAssetEntry entry = settings.FindAssetEntry(guid);
        if (null == entry)
        {
            Debug.LogError($"Addressable Asset Entry를 찾을 수 없습니다 (guid:{guid}, {assetPath})");
            return false;
        }

        address = entry.address;
        return true;
    }
}
