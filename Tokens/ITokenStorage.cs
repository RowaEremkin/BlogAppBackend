namespace BlogAppBackend.Tokens
{
    public interface ITokenStorage
    {
        public bool TestToken(IHeaderDictionary header, string deviceId);
        public string GenerateToken(string deviceId);
    }
}