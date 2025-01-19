using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid.Sdl2;

namespace ZeroManager.Fonts {
    public class FontRegistry {
        public class RegisteredFont {
            private readonly Dictionary<float, ImFontPtr> FontPtrs = [];

            public void InitFonts(string base85Data, float size) {
                if (FontPtrs.Count > 0) {
                    return;
                }

                float scale = 0f;
                while (scale < 3f) {
                    if (scale == 0f) {
                        scale = 1f;
                    }
                    else {
                        scale += .25f;
                    }

                    FontPtrs[scale] = ImGui.GetIO().Fonts.AddFontFromMemoryCompressedBase85TTF(base85Data, size * scale);
                }
            }

            public ImFontPtr Get() {
                float scale = Utility.DpiAwareness.GetWindowScale(Window);
                return FontPtrs[Math.Min((float)(Math.Round(scale * 4) / 4), 3f)];
            }
        }

        public static Sdl2Window? Window;
        public static readonly RegisteredFont Stratum2Medium_20px = new RegisteredFont();

        public static void InitFonts(Sdl2Window window) {
            Window = window;
            Stratum2Medium_20px.InitFonts(Stratum2Medium.Data, 20f);
        }
    }
}
