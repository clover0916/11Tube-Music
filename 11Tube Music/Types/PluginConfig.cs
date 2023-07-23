using System.Collections.Generic;

namespace ElevenTube_Music.Settings.Types
{
    public class PluginConfig
    {
        public string type { get; set; }
        public string name { get; set; }
        #nullable enable
        public string? display_name { get; set; }
        #nullable disable
        public string description { get; set; }
        public string version { get; set; }
        public Author author { get; set; }
        #nullable enable
        public Option[]? option { get; set; }
        #nullable disable
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

    public class Option
    {
        public string type { get; set; }
        public string name { get; set; }
        #nullable enable
        public string? display_name { get; set; }
        #nullable disable
        public string description { get; set; }
    }
}
