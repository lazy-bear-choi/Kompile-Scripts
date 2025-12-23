//using System;
//using System.IO;
//using System.Runtime.Serialization.Formatters.Binary;
//using System.Text;
//using System.Collections.Generic;
//using UnityEngine;
//using DataStruct;
//using Script.Util;

//[Obsolete]
//public static class x_DataTable
//{
//    public static List<SkillData> SkillTable { get; private set; }
//    public static List<ItemData>  ItemTable  { get; private set; }
//    public static List<UnitData>  UnitTable  { get; private set; }
//    public static List<MapData>   MapTable   { get; private set; }

//    public static void LoadTable()
//    {
//        SkillTable = ReadBinary<SkillData>("SkillData.bin");
//        ItemTable  = ReadBinary<ItemData>("ItemData.bin");
//        UnitTable  = ReadBinary<UnitData>("UnitData.bin");
//        MapTable   = ReadBinary<MapData>("MapData.bin");
//    }
//    public static List<T> ReadBinary<T>(string fileName) where T : struct, IDataSetter
//    {
//        string path = Application.dataPath + "/Resources/bin/" + fileName;

//        BinaryFormatter formatter = new BinaryFormatter();
//        FileStream stream = new FileStream(path, FileMode.Open);
//        List<T> table = (List<T>)formatter.Deserialize(stream);
//        stream.Close();

//        return table;
//    }
//    private static void WriteBinary<T>(string path, List<T> table) where T : struct, IDataSetter
//    {
//        BinaryFormatter formatter = new BinaryFormatter();
//        FileStream stream = new FileStream(path, FileMode.Create);
//        formatter.Serialize(stream, table);
//        stream.Close();
//    }
//    public static bool TryGetMapData(int code, out MapData map)
//    {
//        for (int i = 0; i < MapTable.Count; ++i)
//        {
//            map = MapTable[i];
//            if (code == map.Code)
//            {
//                return true;
//            }
//        }

//        map = default(MapData);
//        return false;
//    }
//    public static Dictionary<int, T> LoadMappingData<T>(string fileName) where T : struct
//    {
//        string filePath = Path.Combine(Application.dataPath, "Resources", "bin", "MapTileData", fileName + ".dat");
//        if (File.Exists(filePath))
//        {
//            BinaryFormatter binaryFormatter = new BinaryFormatter();
//            FileStream fileStream = File.Open(filePath, FileMode.Open);

//            // 파일에서 데이터를 역직렬화하여 Dictionary에 로드
//            Dictionary<int, T> map = (Dictionary<int, T>)binaryFormatter.Deserialize(fileStream);

//            fileStream.Close();
//            return map;
//        }
//        else
//        {
//            Debug.LogError("파일이 존재하지 않습니다.");
//        }

//        return null;
//    }


//#if UNITY_EDITOR || UNITY_EDITOR_64 || UNITY_EDITOR_WIN

//    // (for 기획자) custom editor 사용하여 csv 파일을 .bin 파일로 변환
//    public static void LoadCSVTable()
//    {
//        SkillTable = LoadTable<SkillData>("SkillData");
//        ItemTable  = LoadTable<ItemData>("ItemData");
//        UnitTable  = LoadTable<UnitData>("UnitData");
//        MapTable   = LoadTable<MapData>("MapData");
//    }
//    private static List<T> LoadTable<T>(string fileName) where T : IDataSetter, new()
//    {
//        List<Dictionary<string, string>> table = new List<Dictionary<string, string>>();
//        TextAsset csv = Resources.Load<TextAsset>("CSV/" + fileName);
//        StringReader reader = new StringReader(csv.text);
//        StringBuilder sb = new StringBuilder();

//        //Setting
//        string[] columns;   //칼럼명
//        int index;          //칼럼명[] 인덱스
//        string line;        //각 줄
//        char[] chars;       //각 줄을 char 형태로 쪼갬 (중간 ,를 발라내기 위함)
//        bool isSplit;       //분류 여부 (대사 등 본문의 ,와 CSV 구분쉼표를 구분하기 위함)

//        //Column Index
//        line = reader.ReadLine(); //첫줄 날리기
//        columns = line.Split(',');

//        //Content
//        while (true)
//        {
//            line = reader.ReadLine();
//            if (line == null)
//                break;

//            Dictionary<string, string> data = new Dictionary<string, string>();
//            chars = line.ToCharArray();
//            isSplit = true;
//            index = -1;

//            for (int i = 0; i < chars.Length; ++i)
//            {
//                //데이터 중간의 ,로 나누지 않기 위해 판별 조건 추가
//                if (chars[i] == '\u0022') //큰따옴표(")의 유니코드
//                {
//                    isSplit = !isSplit;
//                    continue;
//                }

//                if (isSplit
//                    && chars[i] == '\u002C') //쉼표(,) 유니코드
//                {
//                    data.Add(columns[++index], sb.ToString());
//                    sb.Clear();
//                    continue;
//                }

//                sb.Append(chars[i]);
//            }

//            //마지막 데이터 추가 (,가 없어서 위에서 안걸림)
//            data.Add(columns[++index], sb.ToString());
//            table.Add(data);
//            sb.Clear();
//        }

//        List<T> list = new List<T>();
//        for (int i = 0; i < table.Count; ++i)
//        {
//            T tData = new T();
//            tData.Set(table[i]);
//            list.Add(tData);
//        }

//        return list;
//    }
//    public static void WriteBinaryFiles()
//    {
//        string path = Application.dataPath + "/Resources/bin/";

//        WriteBinary(path + "SkillData.bin", SkillTable);
//        WriteBinary(path + "ItemData.bin", ItemTable);
//        WriteBinary(path + "UnitData.bin", UnitTable);
//        WriteBinary(path + "MapData.bin", MapTable);
//    }

//    // map sampling
//    public static void WriteBinaryMappingData<T>(ConcurrentDictionary<ulong, T> data, string fileName) where T : struct
//    {
//        BinaryFormatter binaryFormatter = new BinaryFormatter();
//        string filePath = Path.Combine(Application.dataPath, "Resources", "bin", "MapNavData", fileName + ".dat");
//        FileStream fileStream = File.Create(filePath);

//        // Dictionary 직렬화
//        binaryFormatter.Serialize(fileStream, data);
//        fileStream.Close();
//    }
//    public static void WriteBinaryMappingData<T>(Dictionary<long, T> data, string fileName) where T : struct
//    {
//        var binaryFormatter = new BinaryFormatter();
//        var filePath = Path.Combine(Application.dataPath, "Rcs", "bin", fileName + ".dat");
//        var fileStream = File.Create(filePath);

//        // Dictionary 직렬화
//        binaryFormatter.Serialize(fileStream, data);
//        fileStream.Close();
//    }
//#endif
//}
