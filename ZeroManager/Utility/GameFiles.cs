using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.GameTypes.NetEase.MAR.Encryption.Aes;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using SkiaSharp;
using System.Linq;
using Veldrid;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace ZeroManager.Utility {
    public class GameFiles {
		private static string? GameDirectory;
		private static readonly string AesKey = "0x0C263D8C22DCB085894899C3A3796383E9BF9DE0CBFB08C9BF2DEF2E84F29D74";

        public class ModData {
            public string Key = "";
            public AbstractAesVfsReader? Reader;
            public string? FolderPath;
            public bool IsPakFile => Reader != null;
            public bool IsFolder => FolderPath != null;
            public float PercentageInstalled = -1f;
            public List<string> DirFiles = [];

            public void Install() {
                ProgressBar? progress = null;
                Install(ref progress);
            }

            public void Install(ref ProgressBar? progress) {
                if (progress != null) {
                    progress.Done = false;
                    progress.Title = $"Installing {Key}";
                    progress.Message = $"Please wait...";
                    progress.Progress = 0f;
                }

                if (Reader != null) {
                    ExtractPakFile(Reader, ref progress);
                }
                else if (FolderPath != null) {
                    long totalSize = 0;
                    long totalSizeComplete = 0;
                    foreach (var file in DirFiles) {
                        if (progress != null) {
                            string gameRelativePath = file.Substring($"{GameDirectory}\\__zm_mods\\{Key}\\".Length);
                            progress.Message = $"Calculating size of \"{gameRelativePath}\"";
                        }
                        totalSize += new FileInfo(file).Length;
                    }
                    long idx = 0;
                    foreach (var file in DirFiles) {
                        idx++;
                        string gameRelativePath = file.Substring($"{GameDirectory}\\__zm_mods\\{Key}\\".Length);
                        if (progress != null) {
                            progress.Message = $"Copying \"{gameRelativePath}\" ({idx}/{DirFiles.Count})";
                            progress.Progress = totalSizeComplete / (float)totalSize;
                        }
                        Console.WriteLine($"{file} -> {GameDirectory}\\MarvelGame\\{gameRelativePath}");
                        File.Copy(file, $"{GameDirectory}\\MarvelGame\\{gameRelativePath}", true);
                        totalSizeComplete += new FileInfo(file).Length;
                    }
                }

                if (progress != null) {
                    progress.Done = true;
                }
            }

            public void Uninstall() {
                ProgressBar? progress = null;
                Uninstall(ref progress);
            }

            public void Uninstall(ref ProgressBar? progress) {
                if (progress != null) {
                    progress.Done = false;
                    progress.Title = $"Uninstalling {Key}";
                    progress.Message = $"Please wait...";
                    progress.Progress = 0f;
                }

                long totalSize = 0;
                long totalSizeComplete = 0;
                long idx = 0;

                if (Reader != null) {
                    foreach (var file in Reader.Files) {
                        totalSize += file.Value.Size;
                    }
                    foreach (var file in Reader.Files) {
                        idx++;
                        var gf = FindGameFile(file.Key);
                        if (gf == null) {
                            continue;
                        }
                        try {
                            string filePath = $"{GameDirectory}\\MarvelGame\\{file.Key}";

                            string? directoryPath = Path.GetDirectoryName(filePath);
                            if (directoryPath != null) {
                                Directory.CreateDirectory(directoryPath);
                            }

                            if (progress != null) {
                                progress.Message = $"Reinstating \"{file.Key}\" ({idx}/{Reader.Files.Count})";
                                progress.Progress = totalSizeComplete / (float)totalSize;
                            }

                            File.WriteAllBytes(filePath, gf.Item2.Read());
                            totalSizeComplete += file.Value.Size;
                            Console.WriteLine($"Data written to {filePath} ({(float)(Math.Round((totalSizeComplete / (float)totalSize) * 100f * 10) / 10)}%)");
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"An error occurred whilst trying to reinstate file [{file.Key}]: {ex.Message}");
                        }
                    }
                }
                else if (FolderPath != null) {
                    foreach (var file in DirFiles) {
                        if (progress != null) {
                            string gameRelativePath = file.Substring($"{GameDirectory}\\__zm_mods\\{Key}\\".Length);
                            progress.Message = $"Calculating size of \"{gameRelativePath}\"";
                        }
                        totalSize += new FileInfo(file).Length;
                    }
                    foreach (var file in DirFiles) {
                        idx++;
                        string gameRelativePath = file.Substring($"{GameDirectory}\\__zm_mods\\{Key}\\".Length);
                        var gf = FindGameFile(gameRelativePath.Replace('\\', '/'));
                        if (gf == null) {
                            continue;
                        }
                        try {
                            string filePath = $"{GameDirectory}\\MarvelGame\\{gameRelativePath}";

                            string? directoryPath = Path.GetDirectoryName(filePath);
                            if (directoryPath != null) {
                                Directory.CreateDirectory(directoryPath);
                            }

                            if (progress != null) {
                                progress.Message = $"Reinstating \"{gameRelativePath}\" ({idx}/{DirFiles.Count})";
                                progress.Progress = totalSizeComplete / (float)totalSize;
                            }

                            File.WriteAllBytes(filePath, gf.Item2.Read());
                            totalSizeComplete += new FileInfo(file).Length;
                            Console.WriteLine($"Data written to {filePath} ({(float)(Math.Round((totalSizeComplete / (float)totalSize) * 100f * 10) / 10)}%)");
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"An error occurred whilst trying to reinstate file [{gameRelativePath}]: {ex.Message}");
                        }
                    }
                }

                if (progress != null) {
                    progress.Done = true;
                }
            }

            public void Verify() {
                ProgressBar? progress = null;
                Verify(ref progress);
            }

            public void Verify(ref ProgressBar? progress) {
                if (progress != null) {
                    progress.Done = false;
                    progress.Title = $"Verifying {Key}";
                    progress.Message = $"Please wait...";
                    progress.Progress = 0f;
                }

                long totalSize = 0;
                long totalSizeComplete = 0;
                bool nulled = true;
                bool nullingFile = false;
                long matchingSize = 0;

                if (Reader != null) {
                    foreach (var file in Reader.Files) {
                        totalSize += file.Value.Size;
                    }

                    if (totalSize == 0) {
                        nullingFile = true;
                    }

                    long idx = 0;
                    long matchingFiles = 0;
                    foreach (var file in Reader.Files) {
                        idx++;
                        string path = $"{GameDirectory}\\MarvelGame\\{file.Key}";
                        if (!File.Exists(path)) {
                            totalSizeComplete += file.Value.Size;
                            continue;
                        }
                        if (nullingFile) {
                            if (new FileInfo(path).Length > 0) {
                                nulled = false;
                                break;
                            }
                            continue;
                        }
                        if (progress != null) {
                            progress.Message = $"Verifying \"{file.Key}\" ({idx}/{DirFiles.Count})";
                            progress.Progress = totalSizeComplete / (float)totalSize;
                        }
                        if (Utility.System.ComputeSHA256Hash(File.ReadAllBytes(path)) == Utility.System.ComputeSHA256Hash(file.Value.Read())) {
                            matchingFiles++;
                            matchingSize += file.Value.Size;
                        }
                        totalSizeComplete += file.Value.Size;
                    }
                }
                else if (FolderPath != null) {
                    foreach (var file in DirFiles) {
                        if (progress != null) {
                            string gameRelativePath = file.Substring($"{GameDirectory}\\__zm_mods\\{Key}\\".Length);
                            progress.Message = $"Calculating size of \"{gameRelativePath}\"";
                        }
                        totalSize += new FileInfo(file).Length;
                    }

                    if (totalSize == 0) {
                        nullingFile = true;
                    }

                    long idx = 0;
                    long matchingFiles = 0;
                    foreach (var file in DirFiles) {
                        idx++;
                        string gameRelativePath = file.Substring($"{GameDirectory}\\__zm_mods\\{Key}\\".Length);
                        string path = $"{GameDirectory}\\MarvelGame\\{gameRelativePath}";
                        if (!File.Exists(path)) {
                            totalSizeComplete += new FileInfo(file).Length;
                            nulled = false;
                            continue;
                        }
                        if (nullingFile) {
                            if (new FileInfo(path).Length > 0) {
                                nulled = false;
                                break;
                            }
                            continue;
                        }
                        if (progress != null) {
                            progress.Message = $"Verifying \"{gameRelativePath}\" ({idx}/{DirFiles.Count})";
                            progress.Progress = totalSizeComplete / (float)totalSize;
                        }
                        if (Utility.System.ComputeSHA256Hash(File.ReadAllBytes(path)) == Utility.System.ComputeSHA256Hash(File.ReadAllBytes(file))) {
                            matchingFiles++;
                            matchingSize += new FileInfo(file).Length;
                        }
                        totalSizeComplete += new FileInfo(file).Length;
                    }
                }

                if (nullingFile) {
                    PercentageInstalled = nulled ? 1f : 0f;
                }
                else {
                    PercentageInstalled = matchingSize / (float)totalSize;
                }

                if (progress != null) {
                    progress.Done = true;
                }
            }
        }
        
        public static Dictionary<string, AbstractAesVfsReader> ProtectedReaders = [];
        public static Dictionary<string, AbstractAesVfsReader> OriginalReaders = [];
        public static Dictionary<string, ModData> Mods = [];

        public static Dictionary<string, List<AbstractAesVfsReader>> ModsRequired = [];

        public static Dictionary<string, string> CurrentlyLoading = [];

        public static Dictionary<string, AbstractAesVfsReader> GetAllGameReaders() {
            Dictionary<string, AbstractAesVfsReader> all = [];
            foreach (var item in ProtectedReaders) {
                all.Add(item.Key, item.Value);
            }
            foreach (var item in OriginalReaders) {
                all.Add(item.Key, item.Value);
            }
            return all;
        }

        private static void OnVfsRegisteredBase(object? readerIn, int count, string readerType, ref Dictionary<string, AbstractAesVfsReader> readerList) {
            AbstractAesVfsReader? reader = (AbstractAesVfsReader?)readerIn;
            if (reader == null) {
                return;
            }
            CurrentlyLoading[readerType] = reader.Name;
            reader.AesKey = new CUE4Parse.Encryption.Aes.FAesKey(AesKey);
            Console.WriteLine($"[{readerType}] {reader.Name} => Vfs #{count} ({reader.Mount().Count} items)");

            readerList.Add(reader.Name, reader);
            CurrentlyLoading[readerType] = "";
        }

        private static void OnProtectedVfsRegistered(object? readerIn, int count) {
            OnVfsRegisteredBase(readerIn, count, "protected", ref ProtectedReaders);
        }

        private static void OnOriginalVfsRegistered(object? readerIn, int count) {
            OnVfsRegisteredBase(readerIn, count, "original", ref OriginalReaders);
        }

        private static void OnModVfsRegistered(object? readerIn, int count) {
            AbstractAesVfsReader? reader = (AbstractAesVfsReader?)readerIn;
            if (reader == null) {
                return;
            }

            reader.AesKey = new CUE4Parse.Encryption.Aes.FAesKey(AesKey);
            Console.WriteLine($"[mod] {reader.Name} => Vfs #{count} ({reader.Mount().Count} items)");

            Mods.Add(reader.Name, new ModData() {
                Key = reader.Name,
                Reader = reader
            });

            Task.Run(() => {
                CurrentlyLoading["mod"] = reader.Name;
                ModsRequired.Add(reader.Name, []);
                foreach (var file in reader.Files) {
                    var gameFile = FindGameFile(file.Key, ProtectedReaders);
                    if (gameFile == null || !IsPakFileProtected(gameFile.Item1.Name)) {
                        continue;
                    }
                    string readerName = reader.Name;
                    var req = ModsRequired[readerName];
                    bool isFound = false;
                    foreach (var item in req) {
                        if (item.Name == gameFile.Item1.Name) {
                            isFound = true;
                        }
                    }
                    if (!isFound) {
                        req.Add(gameFile.Item1);
                    }
                }
                CurrentlyLoading["mod"] = "";
                Console.WriteLine($"[{reader.Name}] [{count}] Finished searching for mod dependencies");

                Mods[reader.Name].Verify();
            });
        }

        private static string[] GetFilesRecursively(string directory) {
            string[] files = Directory.GetFiles(directory);
            string[] subdirectories = Directory.GetDirectories(directory);

            foreach (var subdirectory in subdirectories) {
                files = files.Concat(GetFilesRecursively(subdirectory)).ToArray();
            }

            return files;
        }

        public static void InitFileStructure(string? gameDirectory) {
            ProgressBar? progress = null;
            InitFileStructure(gameDirectory, ref progress);
        }

		public static void InitFileStructure(string? gameDirectory, ref ProgressBar? progress) {
            if (progress != null) {
                progress.Done = false;
                progress.Title = "Refreshing file structure";
                progress.Message = "Please wait...";
                progress.Progress = 0f;
            }
            GameDirectory = gameDirectory;
            ModsRequired.Clear();

            string path;
            DefaultFileProvider provider;

            path = $"{gameDirectory}\\MarvelGame\\Marvel\\Content\\Paks\\";
            if (Directory.Exists(path)) {
                foreach (var item in ProtectedReaders) {
                    item.Value.Dispose();
                }
                ProtectedReaders.Clear();
                provider = new DefaultFileProvider(path, SearchOption.TopDirectoryOnly, false,
                    new VersionContainer(EGame.GAME_MarvelRivals));
                provider.CustomEncryption = MarvelAes.MarvelDecrypt;
                provider.VfsRegistered += OnProtectedVfsRegistered;
                provider.Initialize();
            }

            path = $"{gameDirectory}\\__zm_original\\";
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            foreach (var item in OriginalReaders) {
                item.Value.Dispose();
            }
            OriginalReaders.Clear();
            provider = new DefaultFileProvider(path, SearchOption.TopDirectoryOnly, false,
                new VersionContainer(EGame.GAME_MarvelRivals));
            provider.CustomEncryption = MarvelAes.MarvelDecrypt;
            provider.VfsRegistered += OnOriginalVfsRegistered;
            provider.Initialize();

            path = $"{gameDirectory}\\__zm_mods\\";
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            foreach (var item in Mods) {
                if (item.Value.Reader == null) {
                    continue;
                }
                item.Value.Reader.Dispose();
            }
            Mods.Clear();
            provider = new DefaultFileProvider(path, SearchOption.TopDirectoryOnly, false,
                new VersionContainer(EGame.GAME_MarvelRivals));
            provider.CustomEncryption = MarvelAes.MarvelDecrypt;
            provider.VfsRegistered += OnModVfsRegistered;
            foreach (var dir in Directory.GetDirectories(path)) {
                string modKey = dir.Replace('\\', '/').SubstringAfterLast('/');
                Mods.Add(modKey, new ModData() {
                    Key = modKey,
                    FolderPath = dir,
                    DirFiles = GetFilesRecursively(dir).ToList()
                });

                Task.Run(() => {
                    CurrentlyLoading["mod"] = modKey;
                    ModsRequired.Add(modKey, []);
                    foreach (var file in Mods[modKey].DirFiles) {
                        string gameRelativePath = file.Substring($"{GameDirectory}\\__zm_mods\\{modKey}\\".Length);
                        var gameFile = FindGameFile(gameRelativePath.Replace('\\', '/'), ProtectedReaders);
                        if (gameFile == null || !IsPakFileProtected(gameFile.Item1.Name)) {
                            continue;
                        }
                        string readerName = modKey;
                        var req = ModsRequired[readerName];
                        bool isFound = false;
                        foreach (var item in req) {
                            if (item.Name == gameFile.Item1.Name) {
                                isFound = true;
                            }
                        }
                        if (!isFound) {
                            req.Add(gameFile.Item1);
                        }
                    }
                    CurrentlyLoading["mod"] = "";
                    Console.WriteLine($"[{modKey}] Finished searching for mod dependencies");

                    Mods[modKey].Verify();
                });
            }
            Task.Run(provider.Initialize);
            if (progress != null) {
                progress.Done = true;
            }
        }

        public static Tuple<AbstractAesVfsReader, GameFile>? FindGameFile(string name, Dictionary<string, AbstractAesVfsReader>? readers = null) {
            foreach (var item in readers == null ? GetAllGameReaders() : readers) {
                foreach (var file in item.Value.Files) {
                    if (file.Key.ToLower() == name.ToLower()) {
                        return new Tuple<AbstractAesVfsReader, GameFile>(item.Value, file.Value);
                    }
                }
            }
            return null;
        }

        public static bool IsGameFileExtracted(string name) {
            foreach (var item in ProtectedReaders) {
                foreach (var file in item.Value.Files) {
                    if (file.Key.ToLower() == name.ToLower()) {
                        return false;
                    }
                }
            }
            return File.Exists($"{GameDirectory}\\MarvelGame\\Marvel\\Content\\{name}");
        }

        public static void ExtractPakFile(AbstractAesVfsReader reader) {
            ProgressBar? progress = null;
            ExtractPakFile(reader, ref progress);
        }

        public static void ExtractPakFile(AbstractAesVfsReader reader, ref ProgressBar? progress) {
            if (progress != null) {
                progress.Done = false;
            }
            long totalSize = 0;
            long totalSizeComplete = 0;
            foreach (var item in reader.Files) {
                totalSize += item.Value.Size;
            }
            long idx = 0;
            foreach (var item in reader.Files) {
                idx++;
                try {
                    string filePath = $"{GameDirectory}\\MarvelGame\\{item.Key}";

                    string? directoryPath = Path.GetDirectoryName(filePath);
                    if (directoryPath != null) {
                        Directory.CreateDirectory(directoryPath);
                    }

                    if (progress != null) {
                        progress.Message = $"Writing \"{item.Key}\" ({idx}/{reader.Files.Count})";
                        progress.Progress = totalSizeComplete / (float)totalSize;
                    }

                    File.WriteAllBytes(filePath, item.Value.Read());
                    totalSizeComplete += item.Value.Size;
                    Console.WriteLine($"Data written to {filePath} ({(float)(Math.Round((totalSizeComplete / (float)totalSize) * 100f * 10) / 10)}%)");
                }
                catch (Exception ex) {
                    Console.WriteLine($"An error occurred whilst trying to write to file [{item.Key}]: {ex.Message}");
                }
            }
            if (progress != null) {
                progress.Done = true;
            }
        }

        public static void DeleteExtractedPakFileData(AbstractAesVfsReader reader) {
            ProgressBar? progress = null;
            DeleteExtractedPakFileData(reader, ref progress);
        }

        public static void DeleteExtractedPakFileData(AbstractAesVfsReader reader, ref ProgressBar? progress) {
            if (progress != null) {
                progress.Done = false;
            }
            long totalSize = 0;
            long totalSizeComplete = 0;
            foreach (var item in reader.Files) {
                totalSize += item.Value.Size;
            }
            long idx = 0;
            foreach (var item in reader.Files) {
                idx++;
                try {
                    string filePath = $"{GameDirectory}\\MarvelGame\\{item.Key}";

                    if (progress != null) {
                        progress.Message = $"Deleting \"{item.Key}\" ({idx}/{reader.Files.Count})";
                        progress.Progress = totalSizeComplete / (float)totalSize;
                    }

                    if (File.Exists(filePath)) {
                        File.Delete(filePath);
                    }

                    totalSizeComplete += item.Value.Size;
                    Console.WriteLine($"Deleted {filePath} ({(float)(Math.Round((totalSizeComplete / (float)totalSize) * 100f * 10) / 10)}%)");
                }
                catch (Exception ex) {
                    Console.WriteLine($"An error occurred whilst trying to delete file [{item.Key}]: {ex.Message}");
                }
            }
            if (progress != null) {
                progress.Message = "Deleting unused directories, please wait...";
            }
            DeleteEmptyDirectories($"{GameDirectory}\\MarvelGame\\Marvel\\Content");
            if (progress != null) {
                progress.Done = true;
            }
        }

        public static void ProtectPakFile(string fileName) {
            ProgressBar? progress = null;
            ProtectPakFile(fileName, ref progress);
        }

        public static void ProtectPakFile(string fileName, ref ProgressBar? progress) {
            File.Move($"{GameDirectory}\\__zm_original\\{fileName}", $"{GameDirectory}\\MarvelGame\\Marvel\\Content\\Paks\\{fileName}");
            if (progress != null) {
                progress.Done = false;
            }
            InitFileStructure(GameDirectory, ref progress);
            if (progress != null) {
                progress.Done = true;
            }
        }

        public static void UnprotectPakFile(string fileName) {
            ProgressBar? progress = null;
            UnprotectPakFile(fileName, ref progress);
        }

        public static void UnprotectPakFile(string fileName, ref ProgressBar? progress) {
            File.Move($"{GameDirectory}\\MarvelGame\\Marvel\\Content\\Paks\\{fileName}", $"{GameDirectory}\\__zm_original\\{fileName}");
            if (progress != null) {
                progress.Done = false;
            }
            InitFileStructure(GameDirectory, ref progress);
            if (progress != null) {
                progress.Done = true;
            }
        }

        public static bool IsPakFileProtected(string fileName) {
            return File.Exists($"{GameDirectory}\\MarvelGame\\Marvel\\Content\\Paks\\{fileName}");
        }

        public static void DeleteEmptyDirectories(string path) {
            foreach (var dir in Directory.GetDirectories(path)) {
                DeleteEmptyDirectories(dir);
            }

            try {
                if (Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0) {
                    Directory.Delete(path);
                    Console.WriteLine($"Deleted empty directory: {path}");
                }
            }
            catch (UnauthorizedAccessException) {
                Console.WriteLine($"Access denied to directory: {path}");
            }
            catch (Exception ex) {
                Console.WriteLine($"Error deleting directory {path}: {ex.Message}");
            }
        }
    }
}
