namespace Script.GamePlay.Global.Editor
{
    using Script.GamePlay.Global.Asset;
    using Script.GamePlay.Global.Data;
    using Script.GamePlay.Global.Editor.Utility;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// [Editor Only] CSV 텍스트를 파싱하여 SkillData 리스트로 가공하고, MessagePack으로 저장하는 순수 연산 도구
    /// </summary>
    public class EditorSkillDataBakeUtil
    {
        public static void BakeFromCSV(string csvText)
        {
            List<SkillData> skillList = new List<SkillData>();
            string[] lines = csvText.Split(new[] { 'r', '\n'}, System.StringSplitOptions.RemoveEmptyEntries);

            string[] columns;
            for (int i = 1; i < lines.Length; ++i)
            {
                columns = lines[i].Split(',');

                if (7 > columns.Length)
                {
                    continue;
                }

                SkillData data = new SkillData()
                {
                    SkillID = int.Parse(columns[0]),
                    AnimationStateName = columns[1],
                    StartupTicks = int.Parse(columns[2]),
                    ActiveTicks = int.Parse(columns[3]),
                    RecoveryTicks = int.Parse(columns[4]),
                    HitFrameOffset = int.Parse(columns[5]),
                    ComboWindow = int.Parse(columns[6])
                };

                skillList.Add(data);
            }

            EditorAssetUtil.WriteBinaryFile<List<SkillData>>(
                data: skillList,
                relativePath: "Resources/Data",
                fileName: "SkillTable",
                addressableGroup: "Data",
                addressableLabel: "DataTable"
            );
        }
    }
}