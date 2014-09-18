using System.Data.SqlClient;

namespace RemakeDatabase
{
    public class RemakeConfiguration
    {
        public RemakeConfiguration()
        {
            Server = @".\sqlexpress";
        }

        [ArrayListConverterProperty("database")]
        public string Database { get; set; }

        [ArrayListConverterProperty("server")]
        public string Server { get; set; }

        [ArrayListConverterProperty("user")]
        public string User { get; set; }

        [ArrayListConverterProperty("password")]
        public string Password { get; set; }

        [ArrayListConverterProperty("script")]
        public string Script { get; set; }

        public string ServerConnectionString
        {
            get
            {
                var connStringBuilder = new SqlConnectionStringBuilder { DataSource = Server, InitialCatalog = "master" };
                if (string.IsNullOrEmpty(User) && string.IsNullOrEmpty(Password))
                    connStringBuilder.IntegratedSecurity = true;
                else
                {
                    connStringBuilder.UserID = User;
                    connStringBuilder.Password = Password;
                }

                return connStringBuilder.ConnectionString;
            }
        }
    }
}