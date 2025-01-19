using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZeroManager {
    public class Settings {
        public static Settings Instance = new Settings();

        public string GameDirectory { get; set; } = "";
        public int Theme { get; set; } = 0;

        public void Save() {
            try {
                string jsonStr = JsonSerializer.Serialize(this);
                File.WriteAllText("ZeroManager-Settings.json", jsonStr);
                Console.WriteLine($"Saved settings.");
            }
            catch (Exception e) {
                Console.WriteLine($"Caught exception whilst trying to save settings: {e.Message}");
            }
        }

        public static Settings Load() {
            if (!File.Exists("ZeroManager-Settings.json")) {
                Instance.Save();
            }

            try {
                string jsonStr = File.ReadAllText("ZeroManager-Settings.json");
                Settings? inst = JsonSerializer.Deserialize<Settings>(jsonStr);
                if (inst != null) {
                    Console.WriteLine("Loaded settings.");
                    Instance = inst;
                }
            }
            catch (Exception e) {
                Console.WriteLine($"Caught exception whilst trying to load settings: {e.Message}");
            }

            return Instance;
        }
    }
}
