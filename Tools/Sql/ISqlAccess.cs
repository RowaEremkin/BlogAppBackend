using BlogAppBackend.Tools.Sql.Data;
using MySqlConnector;
using System.Data;

namespace BlogAppBackend.Tools.Sql
{
    public interface ISqlAccess
    {
        Task<DataSet> ExecuteQuery(string sqlString);
        Task<DataSet> SelectWhere(string fromTable, string[] selectItems = null, string[] whereFilters = null, string groupBy = null, string orderBy = null, int limit = 0, int page = 0, List<SqlSelectJoinData> joins = null);
        Task<int> InsertInto(string intoTable, string[] columns, string[] values);
        Task<int> UpdateWhere(string fromTable, string[] columns, string[] values, string[] whereFilters = null, bool useTransaction = true);
        Task<int> DeleteFrom(string fromTable, string[] whereFilters);
        Task<MySqlDataReader> ExecureProcedure(string procedure, string valueKey, string value);
        string ProtectString(string unsafeString);
    }
}