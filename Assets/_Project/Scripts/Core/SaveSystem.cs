using System;
using System.IO;
using UnityEngine;

namespace CowboyHunter.Core
{
    // 런 저장 파일 하나(run.json)를 다룬다.
    public static class SaveSystem
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "run.json");

        public static bool HasSave => File.Exists(FilePath);

        public static string Serialize(RunState run) => JsonUtility.ToJson(run.ToSaveData(), true);

        public static RunState Deserialize(RunConfig config, string json) =>
            RunState.FromSaveData(config, JsonUtility.FromJson<RunSaveData>(json), new System.Random());

        public static void Save(RunState run) => File.WriteAllText(FilePath, Serialize(run));

        // 파일이 없거나 읽을 수 없으면 null
        public static RunState Load(RunConfig config)
        {
            if (!HasSave) return null;
            try
            {
                return Deserialize(config, File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"저장 파일을 불러오지 못했습니다: {e.Message}");
                return null;
            }
        }

        public static void Delete()
        {
            if (HasSave) File.Delete(FilePath);
        }
    }
}
