#if UNITY_EDITOR
using UnityEditor;
using System.Diagnostics;

public class MessagePackGenerator
{
    [MenuItem("Tools/MessagePack 코드 생성")]
    public static void Generate()
    {
        // mpc 실행 명령어를 여기서 자동으로 날려줍니다.
        var process = new Process();
        process.StartInfo.FileName = "mpc";
        process.StartInfo.Arguments = "-i ../ -o ./Assets/Scripts/Generated/GeneratedResolver.cs";
        process.Start();

        UnityEngine.Debug.Log("MessagePack 직렬화 코드를 모두 생성!");
    }
}
#endif