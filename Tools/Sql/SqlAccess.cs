using System.Data;
using BlogAppBackend.Tools.Console;
using BlogAppBackend.Tools.Sql.Data;
using MySqlConnector;

namespace BlogAppBackend.Tools.Sql
{

    public class SqlAccess : ISqlAccess
    {
        #region Fields
        private string _connectionString;
        private readonly SqlAddressData _sqlAddressData;
        private readonly SqlLoginData _sqlLoginData;
        private static IDebugConsole _debugConsole;
        #endregion
        #region Init
        public SqlAccess(SqlAddressData sqlAddressData, SqlLoginData sqlLoginData, IDebugConsole debugConsole)
        {
            _sqlAddressData = sqlAddressData;
            _sqlLoginData = sqlLoginData;
            _debugConsole = debugConsole;
        }
        #endregion
        #region Query
        public async Task<DataSet> SelectWhere(string fromTable, string[] selectItems = null, string[] whereFilters = null, string groupBy = null, string orderBy = null, int limit = 0, int page = 0, List<SqlSelectJoinData> joins = null)
        {
            string query = "";
            query += "SELECT ";
            if (selectItems != null && selectItems.Length > 0)
            {
                for (int i = 0; i < selectItems.Length; ++i)
                {
                    if (i > 0) query += ", ";
                    query += selectItems[i];
                }
            }
            else
            {
                query += "*";
            }
            query += " FROM ";
            query += fromTable;
            if (joins != null && joins.Count > 0)
            {
                foreach (var join in joins)
                {
                    if (join.FromJoinToTable == null || join.FromJoinToTable.Count == 0) continue;
                    query += $"{(join.LeftJoin ? " LEFT" : " ")} JOIN {join.JoinTable} ON ";
                    bool first = true;
                    foreach (KeyValuePair<string, string> pair in join.FromJoinToTable)
                    {
                        if (!first) { query += " AND "; } else { first = false; }
                        query += $"{join.JoinTable}.{pair.Key}={fromTable}.{pair.Value}";
                    }
                }
            }
            if (whereFilters != null && whereFilters.Length > 0)
            {
                query += " WHERE ";
                for (int i = 0; i < whereFilters.Length; ++i)
                {
                    if (i > 0) query += " AND ";
                    query += $"{fromTable}.{whereFilters[i]}";
                }
            }
            if (!string.IsNullOrEmpty(groupBy))
            {
                query += " GROUP BY ";
                query += groupBy;
            }
            if (!string.IsNullOrEmpty(orderBy))
            {
                query += " ORDER BY ";
                query += orderBy;
            }
            if (limit > 0)
            {
                query += " LIMIT ";
                query += limit.ToString();
                if (page > 0)
                {
                    query += " OFFSET ";
                    query += ((page - 1) * limit).ToString();
                }
            }
            query += ";";
            _debugConsole.Log(query);
            return await ExecuteQuery(query);
        }
        public async Task<int> InsertInto(string intoTable, string[] columns, string[] values)
        {
            if (columns.Length != values.Length || columns.Length == 0) return -1;
            string query = "";
            query += $"INSERT INTO {intoTable} (";
            for (int i = 0; i < columns.Length; i++)
            {
                if (i > 0) query += ", ";
                query += columns[i];
            }
            query += ") VALUES (";
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) query += ", ";
                if (values[i].Contains("'"))
                {
                    string temp = values[i].Replace("'", "");
                    query += $"'{ProtectString(temp)}'";
                }
                else
                {
                    query += ProtectString(values[i]);
                }
            }
            query += ");";
            _debugConsole.Log(query);
            return await ExecuteInsert(query);

        }
        public async Task<int> UpdateWhere(string fromTable, string[] columns, string[] values, string[] whereFilters = null, bool useTransaction = false)
        {
            if (columns.Length != values.Length || columns.Length == 0) return -1;
            string query = "";
            if (useTransaction) query += "START TRANSACTION; \n";
            query += "UPDATE ";
            query += fromTable;
            query += "\n";
            query += " SET ";
            for (int i = 0; i < columns.Length; i++)
            {
                if (i > 0) query += ", ";
                query += columns[i] + " = ";
                if (values[i].Contains("'"))
                {
                    string temp = values[i].Replace("'", "");
                    query += $"'{ProtectString(temp)}'";
                }
                else
                {
                    query += ProtectString(values[i]);
                }
            }
            query += "\n";
            if (whereFilters != null && whereFilters.Length > 0)
            {
                query += " WHERE ";
                for (int i = 0; i < whereFilters.Length; ++i)
                {
                    if (i > 0) query += " AND ";
                    query += $"{fromTable}.{whereFilters[i]}";
                }
            }
            query += ";";
            query += "\n";
            if (useTransaction) query += " ROLLBACK;";
            _debugConsole.Log(query);
            return await ExecuteUpdate(query);
        }
        public async Task<int> DeleteFrom(string fromTable, string[] whereFilters)
        {
            if (whereFilters.Length == 0) return 0;

            string query = "";
            query += "DELETE FROM ";
            query += fromTable;
            if (whereFilters.Length > 0)
            {
                query += " WHERE ";
                for (int i = 0; i < whereFilters.Length; ++i)
                {
                    if (i > 0) query += " AND ";
                    query += whereFilters[i];
                }
            }
            query += ";";
            _debugConsole.Log(query);
            return await ExecuteDelete(query);
        }
        #endregion
        #region Execute
        public async Task<DataSet> ExecuteQuery(string sqlString)
        {
            using (MySqlConnection dbConnection = await OpenConnection())
            {
                if (dbConnection.State == ConnectionState.Open)
                {
                    DataSet dataSet = new DataSet();
                    try
                    {
                        using (var command = dbConnection.CreateCommand())
                        {
                            command.CommandText = sqlString;

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                do
                                {
                                    DataTable dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    dataSet.Tables.Add(dataTable);

                                } while (!reader.IsClosed && reader.NextResult());
                            }
                        }
                    }
                    catch (Exception ee)
                    {
                        throw new Exception("SQL:" + sqlString + "/n" + ee.Message.ToString());
                    }
                    finally
                    {
                    }

                    return dataSet;
                }
                _debugConsole.Log(sqlString + " - Connection not open");
                return null;
            }
        }
        public async Task<int> ExecuteInsert(string sqlString)
        {
            try
            {
                using (MySqlConnection dbConnection = await OpenConnection())
                {
                    MySqlCommand command = new MySqlCommand(sqlString, dbConnection);
                    await command.ExecuteNonQueryAsync();
                    string sqlIndex = "SELECT LAST_INSERT_ID() AS NewValue;";
                    command = new MySqlCommand(sqlIndex, dbConnection);
                    int id = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return id;
                }
            }
            catch (Exception ee)
            {
                throw new Exception("SQL:" + sqlString + "/n" + ee.Message.ToString());
            }
            finally
            {
            }
            return -1;
        }
        public async Task<int> ExecuteUpdate(string sqlString)
        {
            try
            {
                using (MySqlConnection dbConnection = await OpenConnection())
                {
                    MySqlCommand command = new MySqlCommand(sqlString, dbConnection);
                    int affectedCount = await command.ExecuteNonQueryAsync();
                    return affectedCount;
                }
            }
            catch (Exception ee)
            {
                throw new Exception("SQL:" + sqlString + "/n" + ee.Message.ToString());
            }
            finally
            {
            }
            return -1;
        }
        public async Task<int> ExecuteDelete(string sqlString)
        {
            try
            {
                using (MySqlConnection dbConnection = await OpenConnection())
                {
                    MySqlCommand command = new MySqlCommand(sqlString, dbConnection);
                    int rowsDeleted = await command.ExecuteNonQueryAsync();
                    return rowsDeleted;
                }
            }
            catch (Exception ee)
            {
                throw new Exception("SQL:" + sqlString + "/n" + ee.Message.ToString());
            }
            finally
            {
            }
            return -1;
        }
        public async Task<MySqlDataReader> ExecureProcedure(string procedure, string valueKey, string value)
        {
            using (MySqlConnection dbConnection = await OpenConnection())
            {
                using (MySqlCommand command = new MySqlCommand(procedure, dbConnection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue(valueKey, value);
                    _debugConsole.Log("ExecudeProcedure: " + command.ToReadableString());

                    MySqlDataReader reader = await command.ExecuteReaderAsync();
                    return reader;
                }
            }
            return null;
        }
        #endregion  
        #region Public Methods
        public string ProtectString(string unsafeString)
        {
            return MySqlHelper.EscapeString(unsafeString);
        }
        #endregion
        #region Private
        private async Task<MySqlConnection> OpenConnection()
        {
            try
            {
                _connectionString =
                    string.Format($"server = {_sqlAddressData.host};port={_sqlAddressData.port};database = {_sqlAddressData.database};user = {_sqlLoginData.username};password = {_sqlLoginData.password};");
                MySqlConnection dbConnection = new MySqlConnection(_connectionString);
                await dbConnection.OpenAsync();
                _debugConsole.Log("Connection established ");
                return dbConnection;
            }
            catch (Exception e)
            {
                _debugConsole.Log("Server connection failed, please recheck whether to open MySql service." + e.Message.ToString());
                return null;
            }
        }
        #endregion
    }
    public static class MySqlCommandExtensions
    {
        public static string ToReadableString(this MySqlCommand command)
        {
            string parameters = "";
            foreach (MySqlParameter parameter in command.Parameters)
            {
                parameters += $"{parameter.ParameterName} = {parameter.Value}, ";
            }
            parameters = parameters.TrimEnd(',', ' ');

            return $"CommandText: {command.CommandText}, CommandType: {command.CommandType}, Parameters: [{parameters}]";
        }
    }
}