using Gameloop.Vdf.Linq;
using Gameloop.Vdf;
using System.Xml.Linq;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using CUE4Parse.GameTypes.NetEase.MAR.Encryption.Aes;
using CUE4Parse.UE4.VirtualFileSystem;
using Microsoft.Win32;
using CUE4Parse.Utils;
using System.Text.Json;

namespace ZeroManager.Utility {
	public class Game {
		private static readonly string SteamAppID = "2767030";
		private static readonly string SteamAppFolder = "MarvelRivals";

		private static readonly string EpicGamesAppID = "27556e7cd968479daee8cc7bd77aebdd";

        public static string? FindSteamDirectory() {
			string foldersVdfPath = @"C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf";
			if (File.Exists(foldersVdfPath)) {
				VProperty property = VdfConvert.Deserialize(File.ReadAllText(foldersVdfPath));
				foreach (VProperty library in property.Value.ToList()) {
					VToken? path = library.Value["path"];
					VToken? apps = library.Value["apps"];
					if (path == null || apps == null) {
						continue;
					}

					string gameDirectory = path.Value<string>();
					gameDirectory += $"{(gameDirectory.EndsWith("\\") ? "" : "\\")}steamapps\\common\\{SteamAppFolder}";
					foreach (VProperty installedApp in apps.ToList()) {
						if (installedApp.Key == SteamAppID && Directory.Exists(gameDirectory)) {
                            return gameDirectory;
						}
					}
				}
			}
			return null;
		}

		public static string? FindEpicGamesDirectory() {
			string regPath = @"SOFTWARE\WOW6432Node\Epic Games\EpicGamesLauncher";
			string regItem = @"AppDataPath";

            try {
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(regPath)) {
                    if (key != null) {
                        string? manifestDir = key.GetValue(regItem) as string;
						if (manifestDir != null) {
							manifestDir = manifestDir.Replace('/', '\\');
							manifestDir += manifestDir.EndsWith('\\') ? "" : "\\";
							manifestDir += "Manifests";
							if (Directory.Exists(manifestDir)) {
								foreach (var file in Directory.GetFiles(manifestDir)) {
									try {
										string json = File.ReadAllText(file);
										JsonDocument doc = JsonDocument.Parse(json);

										if (doc.RootElement.TryGetProperty("CatalogItemId", out JsonElement itemIdProperty)) {
											string? itemId = itemIdProperty.GetString();
											if (itemId == null || itemId.ToLower() != EpicGamesAppID) {
												continue;
											}

											doc.RootElement.TryGetProperty("InstallLocation", out JsonElement locProperty);
											return locProperty.GetString()?.Replace('/', '\\');
										}
									}
									catch (Exception ex) {
										Console.WriteLine($"Exception caught whilst trying to parse: {ex.Message}");
									}
                                }
							}
                        }
                    }
                }
            }
            catch (Exception ex) {
				Console.WriteLine($"Exception caught whilst trying to read registry: {ex.Message}");
            }

            return null;
        }

		public static List<string> FindGameDirectories() {
			List<string> viableDirectories = [];
			string? steamDir = FindSteamDirectory();
			if (steamDir != null) {
				viableDirectories.Add(steamDir);
			}
			string? epicGamesDir = FindEpicGamesDirectory();
			if (epicGamesDir != null) {
				viableDirectories.Add(epicGamesDir);
			}
			return viableDirectories;
		}
	}
}
