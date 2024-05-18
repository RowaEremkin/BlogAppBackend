namespace BlogAppBackend.Controllers.Data
{
    [System.Serializable]
    public class PutPlayerLoginData
    {
        private string login;
        private string password;
        public string Login => login;
        public string Password => password;

        public PutPlayerLoginData(string login, string password)
        {
            this.login = login;
            this.password = password;
        }
    }
}
