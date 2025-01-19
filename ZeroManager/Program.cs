using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ImGuiNET;

using static ImGuiNET.ImGuiNative;
using System.Runtime.InteropServices;
using CUE4Parse.FileProvider.Objects;
using ZeroManager.Utility;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using System.Security.Cryptography;

namespace ZeroManager {
	internal class Program {
        private static Sdl2Window? Window;
        private static GraphicsDevice? GraphicsDevice;
        private static CommandList? CmdList;
        private static ImGuiController? Controller;

        private static Vector3 WindowBackgroundColor = new Vector3(.1f, .1f, .1f);

        private static List<string> GameDirectories = [];
        private static string? ChosenGameDirectory;

        static async Task Main(string[] args) {
            Utility.DpiAwareness.EnableDpiAwareness();
            GameDirectories = Utility.Game.FindGameDirectories();

            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ZeroManager"),
                new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
                out Window,
                out GraphicsDevice);
            Window.Resized += () => {
                GraphicsDevice.MainSwapchain.Resize((uint)Window.Width, (uint)Window.Height);
                Controller?.WindowResized(Window.Width, Window.Height);
            };
            CmdList = GraphicsDevice.ResourceFactory.CreateCommandList();
            Controller = new ImGuiController(GraphicsDevice, GraphicsDevice.MainSwapchain.Framebuffer.OutputDescription, Window);

            Credits.Add("Assisting", new CreditsStore.CreditsItem("ZENIgamer9", "testing, info for epic installation"));
            Credits.Add("Open source projects", new CreditsStore.CreditsItem("CUE4Parse", "https://github.com/FabianFG/CUE4Parse",
                "entire game file interfacing api"));
            Credits.Add("Open source projects", new CreditsStore.CreditsItem("FModel", "https://github.com/4sval/FModel",
                "code for initialising decompression methods"));
            Credits.Add("Open source projects", new CreditsStore.CreditsItem("ImGui.NET", "https://github.com/ImGuiNET/ImGui.NET/",
                "ui framework"));
            Credits.Add("Open source projects", new CreditsStore.CreditsItem("Gameloop.Vdf", "https://github.com/shravan2x/Gameloop.Vdf",
                "base of steam install detection method (reading \"libraryfolders.vdf\")"));

            Settings.Load();
            if (Settings.Instance.GameDirectory.Length > 0) {
                ChosenGameDirectory = Settings.Instance.GameDirectory;
                InitFileStructure(ChosenGameDirectory);
            }
            UI_Settings_Theme = Settings.Instance.Theme;
            ApplyTheme(UI_Settings_Theme);
            await Libraries.InitOodle();
            await Libraries.InitZlib();

            var stopwatch = Stopwatch.StartNew();
            float deltaTime = 0f;
            // Main application loop
            while (Window.Exists) {
                deltaTime = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
                stopwatch.Restart();
                InputSnapshot snapshot = Window.PumpEvents();
                if (!Window.Exists) {
                    break;
                }
                Controller.Update(deltaTime, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                Window.Title = $"ZeroManager [{GraphicsDevice.BackendType.ToString()}] [{GraphicsDevice.DeviceName}] [{ImGui.GetIO().DisplaySize.X}x{ImGui.GetIO().DisplaySize.Y}] [{(int)ImGui.GetIO().Framerate} FPS]";
                ImGui.PushFont(Fonts.FontRegistry.Stratum2Medium_20px.Get());
                SubmitUI();
                ImGui.PopFont();

                CmdList.Begin();
                CmdList.SetFramebuffer(GraphicsDevice.MainSwapchain.Framebuffer);
                CmdList.ClearColorTarget(0, new RgbaFloat(WindowBackgroundColor.X, WindowBackgroundColor.Y, WindowBackgroundColor.Z, 1f));
                Controller.Render(GraphicsDevice, CmdList);
                CmdList.End();
                GraphicsDevice.SubmitCommands(CmdList);
                GraphicsDevice.SwapBuffers(GraphicsDevice.MainSwapchain);
            }

            // Clean up Veldrid resources
            GraphicsDevice.WaitForIdle();
            Controller.Dispose();
            CmdList.Dispose();
            GraphicsDevice.Dispose();
        }

        private static int UI_Settings_Theme = 0;

        //private static string UI_FileFinder_PathInput = "Marvel/Content/Marvel/Environment/Tokyo/TokyoE01/CustomProps/Building/SM_TokyoE01Building014Ab/SM_TokyoE01Building014Abb.uasset";
        private static string UI_FileFinder_PathInput = "Marvel/Content/Marvel/Movies_HeroSkill/Windows/En-US/1031/10310010/103193_1052_High.mp4";
        private static Tuple<AbstractAesVfsReader, GameFile>? UI_FileFinder_GameFile = null;

        private static ProgressBar? UI_CurrentProgressBar = null;
        private static Vector2 UI_CurrentProgressBar_Size = new Vector2(50f, 50f);

        private static int UI_ChooseGameDirectory_Selected = 0;
        private static string UI_ChooseGameDirectory_ManualPathInput = "";
        private static Vector2 UI_ChooseGameDirectory_Size = new Vector2(50f, 50f);

        private static bool InitialisedFileStructure = false;

        private static CreditsStore Credits = new CreditsStore();

        private static void ExtractPak(AbstractAesVfsReader reader) {
            Task.Run(() => {
                UI_CurrentProgressBar = new ProgressBar($"Extracting {reader.Name}", "", 0f);
                GameFiles.ExtractPakFile(reader, ref UI_CurrentProgressBar);
                GameFiles.UnprotectPakFile(reader.Name, ref UI_CurrentProgressBar);
            });
        }

        private static void ReinstatePak(AbstractAesVfsReader reader) {
            Task.Run(() => {
                UI_CurrentProgressBar = new ProgressBar($"Repacking {reader.Name}", "", 0f);
                GameFiles.DeleteExtractedPakFileData(reader, ref UI_CurrentProgressBar);
                GameFiles.ProtectPakFile(reader.Name, ref UI_CurrentProgressBar);
            });
        }

        private static void InitFileStructure(string gameDirectory) {
            Task.Run(() => {
                UI_CurrentProgressBar = new ProgressBar("", "", 0f);
                GameFiles.InitFileStructure(gameDirectory, ref UI_CurrentProgressBar);
                InitialisedFileStructure = true;
            });
        }

        private static void RunCommand(string command, string? dir = null) {
            try {
                ProcessStartInfo processStartInfo = new ProcessStartInfo {
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                if (dir != null) {
                    processStartInfo.WorkingDirectory = dir;
                }

                Process? process = Process.Start(processStartInfo);

                if (process == null) {
                    return;
                }

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception ex) {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static void UIHeading() {
            ImGui.Text($"Located game directory: {ChosenGameDirectory}");
            ImGui.SameLine();
            if (ImGui.SmallButton("Open directory")) {
                Task.Run(() => RunCommand("explorer.exe .", ChosenGameDirectory));
            }
            ImGui.SameLine();
            if (ChosenGameDirectory != null && ImGui.SmallButton("Refresh files")) {
                InitFileStructure(ChosenGameDirectory);
            }
            ImGui.SameLine();
            if (ChosenGameDirectory != null && Window != null && ImGui.SmallButton("Add .pak mod")) {
                string? filePath = Utility.System.OpenFilePicker(Window, "Add .pak mod", "Unreal Pak (*.pak)\0*.pak\0");
                if (filePath != null) {
                    File.Copy(filePath, $"{ChosenGameDirectory}\\__zm_mods\\{filePath.Replace('\\', '/').SubstringAfterLast('/')}");
                    InitFileStructure(ChosenGameDirectory);
                }
            }
            ImGui.SameLine();
            if (ImGui.SmallButton("Launch")) {
                Task.Run(() => RunCommand("MarvelRivals_Launcher.exe", ChosenGameDirectory));
            }
            ImGui.Separator();
        }

        private static void ApplyTheme(int theme) {
            if (theme == 0) {
                if (Utility.System.IsDarkMode()) {
                    ImGui.StyleColorsDark();
                }
                else {
                    ImGui.StyleColorsLight();
                }
            }
            else if (theme == 1) {
                ImGui.StyleColorsDark();
            }
            else if (theme == 2) {
                ImGui.StyleColorsLight();
            }
        }

        private static void CreateTab(string title, Action content) {
            if (ImGui.BeginTabItem(title)) {
                UIHeading();
                ImGui.BeginChild($"scrolling_child_{title}");
                content.Invoke();
                ImGui.EndChild();
                ImGui.EndTabItem();
            }
        }

        private static void SubmitUI() {
            if (ChosenGameDirectory != null && InitialisedFileStructure) {
                ImGui.SetNextWindowPos(new Vector2(0f, 0f));
                ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
                if (ImGui.Begin("ZeroManager Main Window", ImGuiWindowFlags.NoDecoration)) {
                    ImGui.BeginTabBar("##zm-main-tab-bar");
                    CreateTab("Mods", () => {
                        foreach (var mod in GameFiles.Mods.ToList()) {
                            string percInstalled = $" ({(float)(Math.Round(mod.Value.PercentageInstalled * 100f * 10) / 10)}% installed)";
                            if (ImGui.CollapsingHeader($"{mod.Key}{(mod.Value.PercentageInstalled != -1f ? (mod.Value.PercentageInstalled > 0f ? percInstalled : " (not installed)") : "")}")) {
                                if (ImGui.TreeNode($"File list##{mod.Key}")) {
                                    if (mod.Value.FolderPath != null) {
                                        foreach (var file in mod.Value.DirFiles) {
                                            string gameRelativePath = file.Substring($"{ChosenGameDirectory}\\__zm_mods\\{mod.Key}\\".Length);
                                            ImGui.BulletText(gameRelativePath);
                                        }
                                    }
                                    else if (mod.Value.Reader != null) {
                                        foreach (var file in mod.Value.Reader.Files) {
                                            ImGui.BulletText(file.Key);
                                        }
                                    }

                                    ImGui.TreePop();
                                    ImGui.Spacing();
                                }

                                if (GameFiles.ModsRequired.ContainsKey(mod.Key)) {
                                    foreach (var item in GameFiles.ModsRequired[mod.Key]) {
                                        ImGui.BulletText($"Requires {item.Name} to be unpacked");
                                        ImGui.SameLine();
                                        if (ImGui.SmallButton($"Unpack##{mod.Key}-{item.Name}")) {
                                            ExtractPak(item);
                                        }
                                    }
                                }

                                if (ImGui.Button($"Install##{mod.Key}")) {
                                    Task.Run(() => {
                                        UI_CurrentProgressBar = new ProgressBar("", "", 0f);
                                        mod.Value.Install(ref UI_CurrentProgressBar);
                                        mod.Value.Verify(ref UI_CurrentProgressBar);
                                    });
                                }
                                ImGui.SameLine();
                                if (ImGui.Button($"Uninstall##{mod.Key}")) {
                                    Task.Run(() => {
                                        UI_CurrentProgressBar = new ProgressBar("", "", 0f);
                                        mod.Value.Uninstall(ref UI_CurrentProgressBar);
                                        mod.Value.Verify(ref UI_CurrentProgressBar);
                                    });
                                }
                                ImGui.SameLine();
                                if (ImGui.Button("Verify")) {
                                    Task.Run(() => {
                                        UI_CurrentProgressBar = new ProgressBar("", "", 0f);
                                        mod.Value.Verify(ref UI_CurrentProgressBar);
                                            
                                        if (mod.Value.PercentageInstalled > 0f) {
                                            Utility.System.MessageBoxA(IntPtr.Zero, $"{mod.Key} is {(float)(Math.Round(mod.Value.PercentageInstalled * 100f * 10) / 10)}% installed.", "ZeroManager", Utility.System.MB_OK | Utility.System.MB_ICONINFORMATION);
                                        }
                                        else {
                                            Utility.System.MessageBoxA(IntPtr.Zero, $"{mod.Key} is not installed.", "ZeroManager", Utility.System.MB_OK | Utility.System.MB_ICONINFORMATION);
                                        }
                                    });
                                }
                            }
                        }

                        string? botInfoText = GameFiles.CurrentlyLoading.ContainsKey("mod") ? GameFiles.CurrentlyLoading["mod"] : null;
                        if (botInfoText != null && botInfoText.Length > 0) {
                            botInfoText = "Currently loading mod: " + botInfoText;
                            Vector2 botInfoTextSize = ImGui.CalcTextSize(botInfoText);
                            Vector2 cursorPos = ImGui.GetCursorPos();
                            Vector2 botInfoTextPos = new Vector2((ImGui.GetIO().DisplaySize.X - botInfoTextSize.X) / 2f, cursorPos.Y);
                            ImGui.GetWindowDrawList().AddText(botInfoTextPos, ImGui.GetColorU32(ImGuiCol.TextDisabled), botInfoText);
                        }
                    });
                    CreateTab("Game files", () => {
                        ImGui.Text("Info: Repacking does not compile added assets into .pak files, but just reinstates the original .pak files.");
                        ImGui.Separator();
                        foreach (var reader in GameFiles.ProtectedReaders.ToList()) {
                            ImGui.Text($"{reader.Value.Name}");
                            ImGui.SameLine();
                            if (ImGui.SmallButton($"Unpack##{reader.Value.Name}")) {
                                ExtractPak(reader.Value);
                            }
                        }
                        foreach (var reader in GameFiles.OriginalReaders.ToList()) {
                            ImGui.Text($"{reader.Value.Name}");
                            ImGui.SameLine();
                            if (ImGui.SmallButton($"Repack##{reader.Value.Name}")) {
                                ReinstatePak(reader.Value);
                            }
                        }
                    });
                    CreateTab("Settings", () => {
                        ImGui.TextDisabled("Theme");
                        bool themeRefresh = false;
                        themeRefresh = ImGui.RadioButton("System", ref UI_Settings_Theme, 0) || themeRefresh;
                        themeRefresh = ImGui.RadioButton("Dark", ref UI_Settings_Theme, 1) || themeRefresh;
                        themeRefresh = ImGui.RadioButton("Light", ref UI_Settings_Theme, 2) || themeRefresh;
                        if (themeRefresh) {
                            ApplyTheme(UI_Settings_Theme);
                            Settings.Instance.Theme = UI_Settings_Theme;
                            Settings.Instance.Save();
                        }
                        ImGui.Separator();
                        if (ImGui.Button("Reselect game directory")) {
                            InitialisedFileStructure = false;
                        }
                    });
                    CreateTab("File finder", () => {
                        ImGui.InputText("File path", ref UI_FileFinder_PathInput, 4096);
                        if (ImGui.Button("Find")) {
                            UI_FileFinder_GameFile = GameFiles.FindGameFile(UI_FileFinder_PathInput);
                        }

                        if (UI_FileFinder_GameFile != null) {
                            ImGui.TextDisabled("Found file!");
                            ImGui.Text($"File path: {UI_FileFinder_GameFile.Item2.Path}");
                            ImGui.Text($"File package source: {UI_FileFinder_GameFile.Item1.Path}");
                            ImGui.Text($"Size: {UI_FileFinder_GameFile.Item2.Size}");
                            if (ImGui.Button("Extract entire package")) {
                                Task.Run(() => GameFiles.ExtractPakFile(UI_FileFinder_GameFile.Item1));
                            }
                            if (ImGui.Button("Delete extracted package")) {
                                Task.Run(() => GameFiles.DeleteExtractedPakFileData(UI_FileFinder_GameFile.Item1));
                            }
                        }
                        else {
                            ImGui.TextDisabled("No file found.");
                        }
                    });
                    CreateTab("Info", () => {
                        ImGui.Text("Author: Lifix");
                        ImGui.Separator();

                        foreach (var section in Credits.Sections) {
                            ImGui.Text($"{section.Key}:");
                            foreach (var credit in section.Value) {
                                if (credit.AuthorURL == null) {
                                    ImGui.Text($"\t- {credit.Author} - {credit.Description}");
                                }
                                else {
                                    ImGui.Text("\t- ");
                                    ImGui.SameLine(0f, 0f);
                                    ImGui.TextLinkOpenURL(credit.Author, credit.AuthorURL);
                                    ImGui.SameLine(0f, 0f);
                                    ImGui.Text($" - {credit.Description}");
                                }
                            }
                        }

                        /*
                        ImGui.Text("Assisting:");
                        ImGui.Text("\t- ZENIgamer9 - entire game file interfacing api");
                        ImGui.Text("Open source projects:");
                        ImGui.Text("\t- CUE4Parse - entire game file interfacing api");
                        ImGui.Text("\t- FModel - code for initialising decompression methods");
                        ImGui.Text("\t- ImGui.NET - ui framework");
                        ImGui.Text("\t- Gameloop.VDF - base of steam install detection method (reading \"libraryfolders.vdf\")");
                        */
                    });
                    ImGui.EndTabBar();
                }
                ImGui.End();
                ImGui.PopStyleVar();
            }
            else if (UI_CurrentProgressBar == null || UI_CurrentProgressBar.Done) {
                ImGui.OpenPopup("Choose game directory##p");

                UI_ChooseGameDirectory_Size = ImGui.GetIO().DisplaySize - new Vector2(30f, 30f);
                ImGui.SetNextWindowPos((ImGui.GetIO().DisplaySize - UI_ChooseGameDirectory_Size) / 2f);
                //if (UI_ChooseGameDirectory_Size.X > 50f) {
                    ImGui.SetNextWindowSize(UI_ChooseGameDirectory_Size);
                //}
                if (ImGui.BeginPopupModal("Choose game directory##p", /*ImGuiWindowFlags.AlwaysAutoResize |*/ ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove)) {
                    ImGui.BeginTabBar("##zm-choose-game-dir-tab-bar");
                    if (ImGui.BeginTabItem("Detect")) {
                        for (var x = 0; x < GameDirectories.Count; x++) {
                            ImGui.RadioButton(GameDirectories[x], ref UI_ChooseGameDirectory_Selected, x);
                        }

                        if (GameDirectories.Count > 0) {
                            if (ImGui.Button("Select")) {
                                ImGui.CloseCurrentPopup();
                                Settings.Instance.GameDirectory = ChosenGameDirectory = GameDirectories[UI_ChooseGameDirectory_Selected];
                                Settings.Instance.Save();
                                InitFileStructure(ChosenGameDirectory);
                            }
                        }
                        else {
                            ImGui.TextDisabled("Could not detect any game directories.");
                        }

                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Manual")) {
                        ImGui.InputText("File path", ref UI_ChooseGameDirectory_ManualPathInput, 4096);
                        ImGui.SameLine();
                        if (Window != null && ImGui.Button("...")) {
                            string? proposedFolder = Utility.System.OpenFilePicker(Window, "Find MarvelRivals_Launcher.exe", "Marvel Rivals Launcher (MarvelRivals_Launcher.exe)\0MarvelRivals_Launcher.exe\0");
                            if (proposedFolder != null) {
                                UI_ChooseGameDirectory_ManualPathInput = proposedFolder.SubstringBeforeLast('\\');
                            }
                        }

                        if (UI_ChooseGameDirectory_ManualPathInput.Length < 1) {}
                        else if (!Directory.Exists(UI_ChooseGameDirectory_ManualPathInput)) {
                            ImGui.TextDisabled("Couldn't find directory.");
                        }
                        else if (!File.Exists($"{UI_ChooseGameDirectory_ManualPathInput}\\MarvelRivals_Launcher.exe")) {
                            ImGui.TextDisabled("Ensure the directory contains MarvelRivals_Launcher.exe.");
                        }
                        else if (ImGui.Button("Select")) {
                            ImGui.CloseCurrentPopup();
                            Settings.Instance.GameDirectory = ChosenGameDirectory = UI_ChooseGameDirectory_ManualPathInput;
                            Settings.Instance.Save();
                            InitFileStructure(ChosenGameDirectory);
                        }
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();

                    //UI_ChooseGameDirectory_Size = ImGui.GetWindowSize();
                    ImGui.EndPopup();
                }
            }

            if (UI_CurrentProgressBar != null && !UI_CurrentProgressBar.Done) {
                string wndTitle = $"{UI_CurrentProgressBar.Title}##progressbar";
                bool popupMode = true;
                ImGuiWindowFlags wndFlags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;

                if (popupMode) {
                    ImGui.OpenPopup(wndTitle);
                }

                UI_CurrentProgressBar_Size.X = ImGui.GetIO().DisplaySize.X - 30f;
                UI_CurrentProgressBar_Size.Y = 70f * DpiAwareness.GetWindowScale(Window);
                ImGui.SetNextWindowPos((ImGui.GetIO().DisplaySize - UI_CurrentProgressBar_Size) / 2f);
                ImGui.SetNextWindowSize(UI_CurrentProgressBar_Size);
                if (popupMode ? ImGui.BeginPopupModal(wndTitle, wndFlags) : ImGui.Begin(wndTitle, wndFlags)) {
                    ImGui.Text(UI_CurrentProgressBar.Message);

                    Vector2 winPos = ImGui.GetWindowPos();
                    Vector2 winSize = ImGui.GetWindowSize();
                    float progressBarHeight = 5f * DpiAwareness.GetWindowScale(Window);
                    ImGui.GetWindowDrawList().AddRectFilled(new Vector2(winPos.X, winPos.Y + winSize.Y - progressBarHeight),
                        new Vector2(winPos.X + winSize.X * UI_CurrentProgressBar.Progress, winPos.Y + winSize.Y), UInt32.MaxValue);

                    if (popupMode) {
                        ImGui.EndPopup();
                    }
                }
                if (!popupMode) {
                    ImGui.End();
                }
            }
        }
    }
}
