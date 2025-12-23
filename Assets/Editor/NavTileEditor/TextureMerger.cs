using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Script.Index;

public class TextureMerger : MonoBehaviour
{
    [MenuItem("Tools/Asset/Map/Merge Sprites into Texture2D", priority = 1)]
    public static void MergeSprites()
    {
        // 스프라이트 폴더 경로
        string inputPath = "Assets/Editor/Texture";
        string outputPath = "Assets/Editor/Texture/MergedTexture.png";

        // 타겟 텍스처 크기 및 각 스프라이트 크기
        int targetTextureWidth = 2048;
        int targetTextureHeight = 2048;
        int spriteWidth = 256;
        int spriteHeight = 256;

        // 텍스처 생성
        Texture2D texture2D = new Texture2D(targetTextureWidth, targetTextureHeight, TextureFormat.RGBA32, false);

        // 텍스처를 투명으로 초기화
        Color[] clearPixels = new Color[targetTextureWidth * targetTextureHeight];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = new Color(0, 0, 0, 0);
        }
        texture2D.SetPixels(clearPixels);
        texture2D.Apply();

        // 폴더에서 모든 스프라이트 로드
        string[] files = Directory.GetFiles(inputPath, "*.png");

        int columns = targetTextureWidth / spriteWidth;
        int rows = targetTextureHeight / spriteHeight;

        if (files.Length > columns * rows)
        {
            Debug.LogError("Sprites exceed the target texture size. Please adjust the target texture or number of sprites.");
            return;
        }

        // 스프라이트를 순차적으로 텍스처에 배치
        string fileName;
        for (int i = 0; i < files.Length; i++)
        {
            // 파일 경로에서 텍스처 읽기
            string filePath = files[i];
            Texture2D spriteTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);

            if (spriteTexture == null)
            {
                Debug.LogError($"Failed to load texture: {filePath}");
                continue;
            }

            // 순서 조정 (enum에 맞춘다)
            fileName = Path.GetFileName(files[i]).Replace(".png", "");
            int index = (int)Enum.Parse(typeof(TextureIndex), fileName);

            int xIndex = index % columns;
            int yIndex = index / columns;

            // 스프라이트 데이터를 읽어와 타겟 텍스처에 삽입
            Color[] pixels = spriteTexture.GetPixels();
            texture2D.SetPixels(xIndex * spriteWidth, targetTextureHeight - ((yIndex + 1) * spriteHeight), spriteWidth, spriteHeight, pixels);
        }

        // 텍스처 저장
        texture2D.Apply();
        byte[] pngData = texture2D.EncodeToPNG();
        if (pngData != null)
        {
            File.WriteAllBytes(outputPath, pngData);
            Debug.Log($"Merged texture saved to {outputPath}");
        }

        // AssetDatabase 갱신
        AssetDatabase.Refresh();
    }
}
