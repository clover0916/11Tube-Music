using System.Collections.Generic;

namespace ElevenTube_Music.Types
{
    public class PluginSetting
    {
        public bool Enable { get; set; }
        public List<PluginOption> Options { get; set; } = new List<PluginOption>();
    }

    public class PluginOption
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

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
        public string? placeholder { get; set; }
#nullable disable
        public string description { get; set; }
#nullable enable
        public string? default_value { get; set; }
        public string[]? values { get; set; }
    }
}
