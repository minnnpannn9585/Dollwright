using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Dollwright.Graybox
{
    [Serializable] public sealed class DollMaterialStack { public string id; public int count; }
    [Serializable] public sealed class DollCraftingData
    {
        public int version = 1;
        public List<DollMaterialStack> materials = new List<DollMaterialStack>();
        public List<CraftedDoll> dolls = new List<CraftedDoll>();
        public DollRecipe draft;
        public string lastBankedJourney;
    }

    // Pure inventory transactions. File writes succeed before the visible state is replaced.
    public sealed class DollCraftingInventory
    {
        DollCraftingData data;
        readonly string path;
        public event Action Changed;
        public string LastError { get; private set; }
        public IReadOnlyList<CraftedDoll> Dolls => data.dolls.AsReadOnly();
        public DollRecipe Draft => data.draft;
        public DollCraftingInventory(string savePath = null)
        {
            path = savePath;
            data = Read(savePath) ?? Fresh();
        }
        static DollCraftingData Fresh()
        {
            var value = new DollCraftingData();
            foreach (var id in DollMaterialCatalog.Ids)
                value.materials.Add(new DollMaterialStack { id = id, count = id == "R1" || id == "F01" ? 1 : 0 });
            return value;
        }
        static bool Valid(DollCraftingData value)
        {
            return value != null && value.version == 1 && value.draft.IsValid
                && value.materials != null && value.dolls != null
                && value.materials.Count == DollMaterialCatalog.Ids.Length
                && value.materials.All(x => x != null && DollMaterialCatalog.Contains(x.id) && x.count >= 0)
                && value.materials.Select(x => x.id).Distinct().Count() == value.materials.Count
                && value.dolls.All(x => x != null && !string.IsNullOrEmpty(x.instanceId) && x.recipe.IsValid)
                && value.dolls.Select(x => x.instanceId).Distinct().Count() == value.dolls.Count;
        }
        static DollCraftingData Read(string savePath)
        {
            if (string.IsNullOrEmpty(savePath)) return null;
            foreach (var candidate in new[] { savePath, savePath + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    var value = JsonUtility.FromJson<DollCraftingData>(File.ReadAllText(candidate));
                    if (Valid(value)) return value;
                }
                catch (Exception exception) when (exception is IOException || exception is ArgumentException || exception is UnauthorizedAccessException) { }
            }
            return null;
        }
        public int Count(string id) => data.materials.Find(x => x.id == id)?.count ?? 0;
        public bool HasBankedJourney(string id) => !string.IsNullOrEmpty(id) && data.lastBankedJourney == id;
        public bool CanCraft(DollRecipe recipe) => recipe.IsValid && recipe.Ingredients.All(x => Count(x) > 0);
        public CraftedDoll FindDoll(string id) => data.dolls.Find(x => x.instanceId == id);
        DollCraftingData Copy() => JsonUtility.FromJson<DollCraftingData>(JsonUtility.ToJson(data));
        public bool HasSavedGame => Read(path) != null;
        public bool StartNewGame() => Commit(Fresh(), true);
        public bool ReloadSavedGame()
        {
            var saved = Read(path);
            if (saved == null) { LastError = "没有可读取的存档"; return false; }
            LastError = null; data = saved; Changed?.Invoke(); return true;
        }
        bool Commit(DollCraftingData next, bool resetBackup = false)
        {
            if (!Valid(next)) return false;
            LastError = null;
            try
            {
                if (!string.IsNullOrEmpty(path))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                    string temporary = path + ".tmp";
                    File.WriteAllText(temporary, JsonUtility.ToJson(next, true));
                    // A completed new game must not recover dolls from the previous run's backup.
                    // Prepare the fresh backup before replacing the primary; a failed write leaves
                    // the primary and the in-memory inventory unchanged.
                    if (resetBackup) File.Copy(temporary, path + ".bak", true);
                    if (File.Exists(path)) File.Replace(temporary, path, resetBackup ? null : path + ".bak");
                    else File.Move(temporary, path);
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                LastError = exception.Message;
                Debug.LogWarning("[Dollwright] Cannot save customization: " + exception.Message);
                return false;
            }
            data = next;
            Changed?.Invoke();
            return true;
        }
        public bool SetDraft(DollRecipe recipe)
        {
            if (!recipe.IsValid) return false;
            if (JsonUtility.ToJson(recipe) == JsonUtility.ToJson(data.draft)) return true;
            var next = Copy(); next.draft = recipe; return Commit(next);
        }
        public bool TryCraft(DollRecipe recipe, out CraftedDoll created)
        {
            created = null;
            if (!CanCraft(recipe)) return false;
            var next = Copy();
            foreach (var id in recipe.Ingredients) next.materials.Find(x => x.id == id).count--;
            var doll = new CraftedDoll { instanceId = Guid.NewGuid().ToString("N"), recipe = recipe };
            next.draft = recipe;
            next.dolls.Add(doll);
            if (!Commit(next)) return false;
            created = doll;
            return true;
        }
        public bool AddMaterials(IEnumerable<string> ids)
        {
            var values = ids?.ToArray();
            if (values == null || values.Any(x => !DollMaterialCatalog.Contains(x))) return false;
            var next = Copy();
            foreach (var id in values) next.materials.Find(x => x.id == id).count++;
            return Commit(next);
        }
        public bool BankJourney(string journeyId, IEnumerable<string> ids)
        {
            if (string.IsNullOrEmpty(journeyId) || data.lastBankedJourney == journeyId) return false;
            var values = ids?.ToArray();
            if (values == null || values.Any(x => !DollMaterialCatalog.Contains(x))) return false;
            var next = Copy();
            foreach (var id in values) next.materials.Find(x => x.id == id).count++;
            next.lastBankedJourney = journeyId;
            return Commit(next);
        }
        public string ExportJson() => JsonUtility.ToJson(data, true);
    }

    public static class DollCraftingStore
    {
        static DollCraftingInventory current;
        public static string SavePath => Path.Combine(Application.persistentDataPath, "dollwright-customization-v1.json");
        public static DollCraftingInventory Current => current ?? (current = new DollCraftingInventory(SavePath));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { current = null; }
        // Also allows integration tests to use an isolated inventory without touching the player's save.
        public static IDisposable UseTemporary(DollCraftingInventory inventory)
        {
            var previous = current; current = inventory;
            return new Scope(() => current = previous);
        }
        sealed class Scope : IDisposable
        {
            Action restore;
            public Scope(Action action) { restore = action; }
            public void Dispose() { restore?.Invoke(); restore = null; }
        }
    }
}

