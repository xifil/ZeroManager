using CUE4Parse.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroManager.Utility {
    public class Libraries {
        public static async ValueTask InitOodle() {
            if (!File.Exists(OodleHelper.OODLE_DLL_NAME)) {
                await OodleHelper.DownloadOodleDllAsync(OodleHelper.OODLE_DLL_NAME);
            }

            OodleHelper.Initialize(OodleHelper.OODLE_DLL_NAME);
        }

        public static async ValueTask InitZlib() {
            if (!File.Exists(ZlibHelper.DLL_NAME)) {
                await ZlibHelper.DownloadDllAsync(ZlibHelper.DLL_NAME);
            }

            ZlibHelper.Initialize(ZlibHelper.DLL_NAME);
        }
    }
}
