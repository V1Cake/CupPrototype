#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CupPrototype.Flair
{
    // Editor 专用保存工具：把 Play 模式录制结果保存成可提交的 ScriptableObject 资产。
    public static class GestureTemplateAssetSaver
    {
        public static GestureTemplateAsset SaveTemplateAsset(GestureTemplateData data, string folderPath = "Assets/Data/GestureTemplates")
        {
            if (data == null || data.normalizedPoints == null || data.normalizedPoints.Count == 0)
            {
                Debug.LogWarning("[GestureTemplateAssetSaver] Cannot save empty gesture template.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                folderPath = "Assets/Data/GestureTemplates";
            }

            EnsureFolder(folderPath);

            string safeFileName = MakeSafeFileName(data.templateId);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{safeFileName}.asset");
            GestureTemplateAsset asset = ScriptableObject.CreateInstance<GestureTemplateAsset>();
            asset.templateId = data.templateId;
            asset.gestureType = data.gestureType;
            asset.normalizedPoints = new List<Vector2>(data.normalizedPoints);

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GestureTemplateAssetSaver] Saved gesture template asset: {assetPath}");
            return asset;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        private static string MakeSafeFileName(string value)
        {
            string fileName = string.IsNullOrWhiteSpace(value) ? "GestureTemplate" : value;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName;
        }
    }
}
#endif
