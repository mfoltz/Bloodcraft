using VCreate.Hooks;

namespace VCreate.Systems
{
    public struct PetExperienceProfile
    {
        public int CurrentExperience { get; set; }
        public int Level { get; set; }

        public int Focus { get; set; }

        public bool Active { get; set; }

        public bool Combat { get; set; }

        public bool Unlocked { get; set; }

        public List<float> Stats { get; set; }

        // Add more fields as necessary
    }

    public class Omnitool
    {
        public readonly Dictionary<string, bool> modes = [];

        public readonly Dictionary<string, int> data = [];

        public bool Permissions { get; set; }
        public bool Build { get; set; }

        public bool Emotes { get; set; }
        public bool Binding { get; set; }
        public int Familiar { get; set; }
        public bool EquipSkills { get; set; }

        public bool RemoveNodes { get; set; }

        public bool Trading { get; set; }

        public ulong With { get; set; }
        public bool Shiny { get; set; }
        public Stack<string> LastPlaced { get; set; } = new Stack<string>();

        // Constructor
        public Omnitool()
        {
            // Initialize default values for settings
            SetMode("InspectToggle", false); // lists buffs, name and prefab of hovered unit. also outputs components to server log
            SetMode("SnappingToggle", false); // toggles snapping to grid for spawned structures
            SetMode("ImmortalToggle", false); // toggles immortality for spawned structures
            SetMode("MapIconToggle", false); // toggles map icon for spawned structures
            SetMode("DestroyToggle", false); // toggles DestroyMode (destroy unit, won't work on vampires)
            SetMode("CopyToggle", false); // toggles CopyMode, spawns last unit inspected/set as charmed (need to add check for vampire horses as those will crash server)
            SetMode("DebuffToggle", false); // toggles DebuffMode (debuff unit, clear all buffs)
            SetMode("ConvertToggle", false); // toggles ConvertMode (convert unit, follows and fights)
            SetMode("BuffToggle", false); // toggles BuffMode (buff unit, uses last buff set)
            SetMode("TileToggle", false); // toggles TileMode (spawn tile, uses last tile set)
            SetMode("Trainer", false);

            SetData("Rotation", 0); // rotation for spawned structures
            SetData("Unit", 0); // unit prefab for CopyMode
            SetData("Tile", 0); // tile prefab for TileMode
            SetData("GridSize", 0); // grid size for snapping spawned structures
            SetData("MapIcon", 0); // map icon prefab for spawned structures
            SetData("Buff", 0); // buff prefab for BuffMode
            SetData("Debuff", 0); // debuff prefab for DebuffMode
        }

        // Methods for mode dictionary
        public void SetMode(string key, bool value)
        {
            modes[key] = value; // This automatically handles add or update
        }

        public bool GetMode(string key)
        {
            if (modes.TryGetValue(key, out bool value))
            {
                return value;
            }
            return false; // Consider handling this case more explicitly
        }

        public void SetData(string key, int value)
        {
            data[key] = value; // This automatically handles add or update
        }

        public int GetData(string key)
        {
            if (data.TryGetValue(key, out int value))
            {
                return value;
            }
            return 0; // Consider handling this case more explicitly
        }

        // Methods for undo functionality
        public void AddEntity(string tileRef)
        {
            if (LastPlaced.Count >= 10)
            {
                LastPlaced.Pop(); // Ensure we only keep the last 10
            }
            LastPlaced.Push(tileRef);
        }

        public string PopEntity()
        {
            return LastPlaced.Count > 0 ? LastPlaced.Pop() : null;
        }
    }
}