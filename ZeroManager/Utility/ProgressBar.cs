using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroManager.Utility {
    public class ProgressBar {
        public string Title;
        public string Message;
        public float Progress;
        public bool Done = false;

        public ProgressBar(string title, string message, float progress) {
            Title = title;
            Message = message;
            Progress = progress;
        }
    }
}
