using BlogAppBackend.DebugConsole;
using BlogAppBackend.Sql;
using MySqlConnector;
using System.Data;

namespace BlogAppBackend.Devices
{
    public class DeviceStorage : IDeviceStorage
    {
        private readonly ISqlAccess _sqlAccess;
        private readonly IDebugConsole _debugConsole;
        public DeviceStorage(ISqlAccess sqlAccess, IDebugConsole debugConsole)
        {
            _sqlAccess = sqlAccess;
            _debugConsole = debugConsole;
        }
        public async Task<bool> CreateDeviceId(string deviceId, int authorId)
        {
            DataSet dataSet = await _sqlAccess.SelectWhere(
                fromTable: "devices",
                whereFilters: [$"deviceId = '{deviceId}'"]
                ); 
            _debugConsole.Log($"CreateDeviceId dataset table count: {dataSet.Tables.Count}");
            DataTable dataTable = null;
            if (dataSet.Tables.Count > 0) dataTable = dataSet.Tables[0];
            if (dataTable != null)
            {
                _debugConsole.Log($"CreateDeviceId dataTable rows count: {dataTable.Rows.Count}");
                if (dataTable.Rows.Count > 0) return false;
            }
            _debugConsole.Log($"CreateDeviceId deviceId: {deviceId} authorId: {authorId}");
            if (await _sqlAccess.InsertInto(
                intoTable: "devices",
                columns: ["deviceId", "authorId"],
                values: [$"'{deviceId}'", authorId.ToString()]
                )>=0)
            {
                return true;
            }
            return false;
        }
        public async Task<int> GetUserByDeviceId(string deviceId)
        {
            {
                DataSet dataSet = await _sqlAccess.ExecuteQuery($"CALL GetAuthorIdByDeviceId('{deviceId}')");
                if(dataSet == null)
                {
                    _debugConsole.Log($"GetUserByDeviceId dataRow is null");
                    return -1;
                }
                DataTable dataTable = null;
                if (dataSet.Tables.Count > 0) dataTable = dataSet.Tables[0];
                if (dataTable == null)
                {
                    _debugConsole.Log($"GetUserByDeviceId dataTable is null");
                    return -1;
                }
                if (dataTable.Rows.Count == 0)
                {
                    _debugConsole.Log($"GetUserByDeviceId dataTable.Rows.Count is 0");
                    return -1;
                }
                DataRow dataRow = dataTable.Rows[0];
                if (dataRow == null)
                {
                    _debugConsole.Log($"GetUserByDeviceId dataRow is null");
                    return -1;
                }
                string authorIdStr = dataRow["authorId"].ToString();
                if (int.TryParse(authorIdStr, out int authorId))
                {
                    return authorId;
                }
                return -1;
            }
            {
                MySqlDataReader reader = await _sqlAccess.ExecureProcedure("GetAuthorIdByDeviceId", "@p_deviceId", deviceId);
                int authorId = -1;
                if (reader == null)
                {
                    _debugConsole.Log($"GetUserByDeviceId reader is null");
                    return -1;
                }
                if (reader.IsClosed)
                {
                    _debugConsole.Log($"GetUserByDeviceId reader.IsClosed");
                    return -1;
                }
                if (!reader.HasRows)
                {
                    _debugConsole.Log($"GetUserByDeviceId reader.HasRows == false");
                }
                while (reader.Read())
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("authorId")))
                    {
                        authorId = reader.GetInt32("authorId");
                        _debugConsole.Log($"AuthorId: {authorId}");
                        break;
                    }
                    else
                    {
                        _debugConsole.Log("authorId is NULL.");
                    }
                }
                if (authorId >= 0)
                {
                    return authorId;
                }
                else
                {
                    return -1;
                }
            }
        }
        public async Task<int> DeleteDeviceId(string deviceId)
        {
            return await _sqlAccess.DeleteFrom(
                fromTable: "devices",
                whereFilters: [$"deviceId = '{deviceId}'"]);
        }
    }
}
