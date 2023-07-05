using System.Collections.Generic;

namespace ElevenTube_Music.Settings.Types
{
    public class PluginConfig
    {
        public string type { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string version { get; set; }
        public Author author { get; set; }
    }

    public class Author
    {
        public string name { get; set; }
        public List<ContactInfo> contact { get; set; }
    }

    public class ContactInfo
    {
        public string name { get; set; }
        public string url { get; set; }
    }
}
