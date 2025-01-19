using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroManager.Utility {
    public class CreditsStore {
        public class CreditsItem {
            public string Author { get; set; }
            public string? AuthorURL { get; set; }
            public string Description { get; set; }

            public CreditsItem(string author, string description) {
                Author = author;
                Description = description;
            }

            public CreditsItem(string author, string authorURL, string description) {
                Author = author;
                AuthorURL = authorURL;
                Description = description;
            }
        }

        public Dictionary<string, List<CreditsItem>> Sections = [];

        public void Add(string section, CreditsItem item) {
            if (!Sections.ContainsKey(section)) {
                Sections[section] = [];
            }

            Sections[section].Add(item);
        }
    }
}
