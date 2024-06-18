using BlogAppBackend.Controllers.Data;
using BlogAppBackend.Tools.Console;
using BlogAppBackend.Tools.Devices;
using BlogAppBackend.Tools.Sql;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BlogAppBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]

    public class RoomController : ControllerBase
    {
        #region Fields
        private readonly ISqlAccess _sqlAccess;
        private readonly IDebugConsole _debugConsole;
        private readonly IDeviceStorage _deviceStorage;
        #endregion
        #region Init
        public RoomController(
            ISqlAccess sqlAccess,
            IDebugConsole debugConsole,
            IDeviceStorage deviceStorage
            )
        {
            _sqlAccess = sqlAccess;
            _debugConsole = debugConsole;
            _deviceStorage = deviceStorage;
        }
        #endregion
        #region Create\Delete

        [HttpPost("Create")]
        public async Task<IActionResult> PostCreate([FromBody] PostRoomCreateData postRoomCreateData)
        {
            if(postRoomCreateData != null)
            {
                int roomId = await _sqlAccess.InsertInto(
                    intoTable: "rooms",
                    columns: ["roomName"],
                    values: [$"'{postRoomCreateData.Name}'"]
                    );
                if(roomId >= 0)
                {
                    return Ok();
                }
            }
            return BadRequest();
        }

        [HttpDelete("Delete/{roomId}")]
        public async Task<IActionResult> DeleteRoom(string roomId)
        {
            _debugConsole.Log($"DeleteRoom roomId: {roomId}");
            int deletedRooms = await _sqlAccess.DeleteFrom(
                fromTable: "rooms",
                whereFilters: [$"roomId = {roomId}"]
                );
            if(deletedRooms > 0)
            {
                await _sqlAccess.DeleteFrom(
                    fromTable: "blogs",
                    whereFilters: [$"roomId = {roomId}"]
                    );
                return Ok();
            }
            return NoContent();
        }
        #endregion
        #region Get

        [HttpGet("Get/{page}")]
        public async Task<IActionResult> Get(int page)
        {
            DataSet dataSet = await _sqlAccess.SelectWhere(
                fromTable: "rooms",
                selectItems: [],
                orderBy: "roomId",
                limit: 5,
                page: page); 
            DataTable dataTable = null;
            if (dataSet.Tables.Count > 0) dataTable = dataSet.Tables[0];
            if (dataTable == null) return NoContent();
            GetRoomsData getRoomsData = new GetRoomsData();
            getRoomsData.items = new List<RoomElementData>();
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                DataRow dataRow = dataTable.Rows[i];
                RoomElementData roomElementData = new RoomElementData();
                roomElementData.roomId = Convert.ToInt32(dataRow["roomId"].ToString());
                roomElementData.roomName = dataRow["roomName"].ToString();
                getRoomsData.items.Add(roomElementData);
            }
            if(getRoomsData.items.Count > 0) return Ok(getRoomsData);
            return NoContent();
        }
        #endregion
    }
}
