using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevenTube_Music.Types
{
    public class ThumbnailData
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Thumbnail
    {
        public List<ThumbnailData> thumbnails { get; set; }
    }

    public class VideoDetail
    {
        public string videoId { get; set; }
        public string title { get; set; }
        public string lengthSeconds { get; set; }
        public string channelId { get; set; }
        public bool isOwnerViewing { get; set; }
        public bool isCrawlable { get; set; }
        public Thumbnail thumbnail { get; set; }
        public bool allowRatings { get; set; }
        public string viewCount { get; set; }
        public string author { get; set; }
        public bool isPrivate { get; set; }
        public bool isUnpluggedCorpus { get; set; }
        public string musicVideoType { get; set; }
        public bool isLiveContent { get; set; }
    }
}
