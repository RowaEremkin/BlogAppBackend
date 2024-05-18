namespace BlogAppBackend.Sql.Data
{
    public class SqlSelectJoinData
    {
        public string JoinTable { get; set; }
        public string AsName { get; set; }
        public string FromName { get; set; }
        public bool LeftJoin { get; set; }
        public Dictionary<string, string> FromJoinToTable { get; set; }
    }
}
