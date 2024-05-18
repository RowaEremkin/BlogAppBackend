namespace BlogAppBackend.Controllers.Data
{
    public class BlogElementData
    {
        public int id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string dateTime { get; set; }
        public int authorId { get; set; }
        public int roomId { get; set; }
        public int likeCount { get; set; }
        public bool liked { get; set; }
        public bool myBlog { get; set; }
    }
}
