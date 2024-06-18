using BlogAppBackend.Controllers.Data;
using BlogAppBackend.Tools.Console;
using BlogAppBackend.Tools.Devices;
using BlogAppBackend.Tools.Sql;
using BlogAppBackend.Tools.Sql.Data;
using BlogAppBackend.Tools.Tokens;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;

namespace BlogAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]

    public class BlogController : ControllerBase
    {
        #region Fields
        private readonly ISqlAccess _sqlAccess;
        private readonly IDebugConsole _debugConsole;
        private readonly IDeviceStorage _deviceStorage;
        private readonly ITokenStorage _tokenStorage;
        #endregion
        #region Init
        public BlogController(
            ISqlAccess sqlAccess,
            IDebugConsole debugConsole,
            IDeviceStorage deviceStorage,
            ITokenStorage tokenStorage
            )
        {
            _sqlAccess = sqlAccess;
            _debugConsole = debugConsole;
            _deviceStorage = deviceStorage;
            _tokenStorage = tokenStorage;
        }
        #endregion
        #region Create\Delete

        [HttpPost("Create/{deviceId}")]
        [SwaggerResponse(401, "Unauthorized", typeof(void))]
        public async Task<IActionResult> PostCreate(string deviceId, [FromBody] PostBlogData postBlogData)
        {
            if (!_tokenStorage.TestToken(Request.Headers, deviceId)) return Unauthorized();
            if (postBlogData != null)
            {
                DataSet dataSet = await _sqlAccess.SelectWhere(
                    fromTable: "devices",
                    whereFilters: [$"deviceId = '{deviceId}'"]);

                DataTable dataTable = null;
                if (dataSet.Tables.Count > 0) dataTable = dataSet.Tables[0];
                if (dataTable == null || dataTable.Rows.Count < 1) return NoContent();
                DataRow dataRow = dataTable.Rows[0];
                int authorId = Convert.ToInt32(dataRow["authorId"].ToString());

                int blogId = await _sqlAccess.InsertInto(
                    intoTable: "blogs",
                    columns: ["blogTitle", "blogDescription", "authorId", "roomId"],
                    values: [$"'{postBlogData.Title}'", $"'{postBlogData.Description}'", authorId.ToString(), postBlogData.RoomId.ToString()]
                    );
                if (blogId >= 0)
                {
                    return Ok();
                }
            }
            return BadRequest();
        }
        [HttpDelete("Delete/{deviceId}/{blogId}")]
        [SwaggerResponse(401, "Unauthorized", typeof(void))]
        public async Task<IActionResult> Delete(string deviceId, int blogId)
        {
            if (!_tokenStorage.TestToken(Request.Headers, deviceId)) return Unauthorized();
            int authorId = await _deviceStorage.GetUserByDeviceId(deviceId);
            if (authorId < 0) return Unauthorized();
            int deletedCount = await _sqlAccess.DeleteFrom(
                fromTable: "blogs",
                whereFilters: [$"blogId = {blogId}", $"authorId = '{authorId}'"]);
            if (deletedCount > 0)
            {
                return Ok();
            }
            else
            {
                return NoContent();
            }
        }
        #endregion
        #region Update
        [HttpPut("Edit/{deviceId}")]
        [SwaggerResponse(401, "Unauthorized", typeof(void))]
        public async Task<IActionResult> Edit(string deviceId, [FromBody] PutBlogEditData deleteBlogData)
        {
            if (!_tokenStorage.TestToken(Request.Headers, deviceId)) return Unauthorized();
            if (deleteBlogData != null)
            {
                int authorId = await _deviceStorage.GetUserByDeviceId(deviceId);
                if (authorId < 0)
                {
                    _debugConsole.Log($"PutBlogEdit no user with deviceId: {deviceId}");
                    return Unauthorized();
                }
                int updateCount = await _sqlAccess.UpdateWhere(
                    fromTable: "blogs",
                    columns: ["blogDescription"],
                    values: ["'" + deleteBlogData.BlogDescription + "'"],
                    whereFilters: [$"blogId = {deleteBlogData.BlogId}", $"authorId = {authorId}"],
                    useTransaction: true);
                if (updateCount > 0)
                {
                    return Ok();
                }
                else
                {
                    _debugConsole.Log($"PutBlogEdit no content");
                    return NoContent();
                }
            }
            return BadRequest();
        }
        #endregion
        #region Get

        [HttpGet("Get/{deviceId}")]
        [SwaggerResponse(204, "No Content", typeof(void))]
        [SwaggerResponse(401, "Unauthorized", typeof(void))]
        public async Task<IActionResult> Get(string deviceId, [FromQuery] int page = 0, [FromQuery] int limit = 10, [FromQuery] int roomId = -1, [FromQuery] int authorId = -1)
        {
            if (!_tokenStorage.TestToken(Request.Headers, deviceId)) return Unauthorized();
            List<string> whereFilters = new List<string>();
            if(roomId >= 0) whereFilters.Add($"roomId={roomId}");
            if (authorId >= 0) whereFilters.Add($"authorId={authorId}");
            if (limit < 1) limit = 1;
            List<SqlSelectJoinData> joins = new List<SqlSelectJoinData>();
            List<string> selectItems = new List<string>();
            string groupBy = "blogs.blogId";
            if (roomId == -1)
            {
                groupBy += ", rooms.roomId";
                selectItems.Add("rooms.*");
                joins.Add(new Tools.Sql.Data.SqlSelectJoinData()
                {
                    JoinTable = "rooms",
                    FromName = "roomName",
                    AsName = "title",
                    FromJoinToTable = new Dictionary<string, string>
                        {
                            {"roomId", "roomId" }
                        }
                });
            }
            if (authorId == -1)
            {
                groupBy += ", authors.authorId";
                selectItems.Add("authors.*");
                joins.Add(new Tools.Sql.Data.SqlSelectJoinData()
                {
                    JoinTable = "authors",
                    FromName = "login",
                    AsName = "title",
                    FromJoinToTable = new Dictionary<string, string>
                        {
                            {"authorId", "authorId" }
                        }
                });
            }
            joins.Add(new Tools.Sql.Data.SqlSelectJoinData()
            {
                JoinTable = "likes",
                FromName = "login",
                LeftJoin = true,
                FromJoinToTable = new Dictionary<string, string>
                    {
                        {"blogId", "blogId" }
                    }
            });
            int myAuthorId = await GetAuthorIdByDeviceID(deviceId);
            selectItems.AddRange(["blogs.*",
                    "COUNT(likes.authorId) AS likeCount",
                    $"COUNT(CASE WHEN likes.authorId = {myAuthorId} THEN 1 ELSE NULL END) > 0 AS liked"
                    ]);
            DataSet dataSet = await _sqlAccess.SelectWhere(
                fromTable: "blogs",
                selectItems: selectItems.ToArray(),
                whereFilters: whereFilters.ToArray(),
                groupBy: groupBy,
                orderBy: "blogDateTime DESC",
                limit: limit,
                page: page,
                joins: joins
                );
            if(dataSet==null) return NoContent();
            _debugConsole.Log($"GetBlogs dataset table count: {dataSet.Tables.Count}");
            DataTable dataTable = null;
            if (dataSet.Tables.Count > 0) dataTable = dataSet.Tables[0];
            if (dataTable == null) return NoContent();
            _debugConsole.Log($"GetBlogs dataTable rows count: {dataTable.Rows.Count}");
            GetBlogData getBlogData = new GetBlogData();
            getBlogData.items = new List<BlogElementData>();
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                DataRow dataRow = dataTable.Rows[i];
                BlogElementData blogElementData = new BlogElementData();
                blogElementData.id = Convert.ToInt32(dataRow["blogId"].ToString());
                if (dataRow.Table.Columns.Contains("roomName"))
                {
                    _debugConsole.Log($"GetBlogs row contains roomName");
                    blogElementData.title = dataRow["roomName"].ToString();
                }
                else if (dataRow.Table.Columns.Contains("authorLogin"))
                {
                    _debugConsole.Log($"GetBlogs row contains authorLogin");
                    blogElementData.title = dataRow["authorLogin"].ToString();
                }
                else
                {
                    blogElementData.title = dataRow["blogTitle"].ToString();
                }
                blogElementData.description = dataRow["blogDescription"].ToString();
                blogElementData.dateTime = ((DateTime)dataRow["blogDateTime"]).ToString("dd-MM-yyyy HH:mm:ss");
                blogElementData.authorId = Convert.ToInt32(dataRow["authorId"].ToString());
                blogElementData.roomId = Convert.ToInt32(dataRow["roomId"].ToString());
                blogElementData.likeCount = Convert.ToInt32(dataRow["likeCount"].ToString());
                blogElementData.liked = Convert.ToInt32(dataRow["liked"].ToString())==1;
                blogElementData.myBlog = blogElementData.authorId == myAuthorId;
                getBlogData.items.Add(blogElementData);
            }
            if (getBlogData.items.Count > 0) return Ok(getBlogData);
            return NoContent();
        }
        #endregion


        [HttpPut("Like/{deviceId}")]
        [SwaggerResponse(401, "Unauthorized", typeof(void))]
        public async Task<IActionResult> PutLike(string deviceId, [FromBody] PutBlogLikeData putBlogLikeData)
        {
            if (!_tokenStorage.TestToken(Request.Headers, deviceId)) return Unauthorized();
            if (putBlogLikeData != null)
            {
                int authorId = await GetAuthorIdByDeviceID(deviceId);
                bool exist = false;
                {
                    DataSet dataSet = await _sqlAccess.SelectWhere(
                        fromTable: "likes",
                        whereFilters: [$"blogId = {putBlogLikeData.BlogId}", $"authorId = {authorId}"]);

                    _debugConsole.Log($"PutLike dataset table count: {dataSet.Tables.Count}");
                    DataTable dataTable = null;
                    if (dataSet.Tables.Count > 0) dataTable = dataSet.Tables[0];
                    if (dataTable == null || dataTable.Rows.Count < 1) exist = false; else exist = true;
                }
                int ok = -1;
                if (exist)
                {
                    _debugConsole.Log($"PutLike DeleteFrom blogId: {putBlogLikeData.BlogId} authorId: {authorId}");
                    ok = await _sqlAccess.DeleteFrom(
                        fromTable: "likes",
                        whereFilters: [$"blogId = {putBlogLikeData.BlogId}", $"authorId = {authorId}"]
                        );
                }
                else
                {
                    _debugConsole.Log($"PutLike InsertInto blogId: {putBlogLikeData.BlogId} authorId: {authorId}");
                    ok = await _sqlAccess.InsertInto(
                        intoTable: "likes",
                        columns: ["blogId", "authorId"],
                        values: [putBlogLikeData.BlogId.ToString(), authorId.ToString()]
                        );
                }
                if (ok >= 0)
                {
                    return Ok();
                }
            }
            return BadRequest();
        }
        private async Task<int> GetAuthorIdByDeviceID(string deviceId)
        {
            int myAuthorId = -1;
            {
                DataSet dataSet = await _sqlAccess.SelectWhere(
                    fromTable: "devices",
                    whereFilters: [$"deviceId = '{deviceId}'"]);
                DataTable dataTable = null;
                if (dataSet.Tables.Count > 0) dataTable = dataSet.Tables[0];
                if (dataTable == null || dataTable.Rows.Count < 1) return -1;
                DataRow dataRow = dataTable.Rows[0];
                myAuthorId = Convert.ToInt32(dataRow["authorId"].ToString());
                _debugConsole.Log($"PutLike authorId: {myAuthorId}");
            }
            return myAuthorId;
        }
    }
}
