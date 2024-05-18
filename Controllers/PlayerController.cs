using BlogAppBackend.Controllers.Data;
using BlogAppBackend.DebugConsole;
using BlogAppBackend.Devices;
using BlogAppBackend.Sql;
using BlogAppBackend.Tokens;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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

    public class PlayerController : ControllerBase
    {
        #region Fields
        private readonly ISqlAccess _sqlAccess;
        private readonly IDebugConsole _debugConsole;
        private readonly IDeviceStorage _deviceStorage;
        private readonly ITokenStorage _tokenStorage;
        #endregion
        #region Init
        public PlayerController(
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
        #region Login\Register
        [HttpPut("Login/{deviceId}")]
        [SwaggerResponse(200, "Ok + token", typeof(string))]
        [SwaggerResponse(204, "No user with login and password", typeof(void))]
        public async Task<IActionResult> PutLogin(string deviceId, [FromBody] PutPlayerLoginData putPlayerLoginData)
        {
            //_debugConsole.Log($"PutLogin deviceId: {deviceId} body: {jsonElement.ToString()}");
            if (string.IsNullOrEmpty(deviceId)) return UnprocessableEntity("DeviceId is empty");
           // PutPlayerLoginData putPlayerLoginData = JsonSerializer.Deserialize<PutPlayerLoginData>(jsonElement.GetRawText());
            if (string.IsNullOrEmpty(putPlayerLoginData.Login)) return BadRequest("Login is empty");
            if (string.IsNullOrEmpty(putPlayerLoginData.Password)) return BadRequest("Password is empty");

            DataSet dataSet = await _sqlAccess.SelectWhere(
                fromTable: "authors",
                selectItems: [],
                whereFilters: [$"authorLogin = '{putPlayerLoginData.Login}'", $"authorPassword = '{putPlayerLoginData.Password}'"]
                );
            DataTable dataTable = null;
            if(dataSet == null) return NoContent();
            if (dataSet.Tables.Count > 0) dataTable = dataSet.Tables[0];
            if(dataTable == null) return NoContent();
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                DataRow dataRow = dataTable.Rows[i];
                int authorId = await _deviceStorage.GetUserByDeviceId(deviceId);
                if (authorId < 0)
                {
                    if(int.TryParse(dataRow["authorId"].ToString(), out int authorIdNew))
                    {
                        await _deviceStorage.CreateDeviceId(deviceId, authorIdNew);
                    }
                }
                string token = _tokenStorage.GenerateToken(deviceId);
                return Ok(token);
            }
            return NoContent();
        }
        [HttpPut("Register/{deviceId}")]
        [SwaggerResponse(200, "Ok + token", typeof(string))]
        [SwaggerResponse(409, "Login is unavailable", typeof(void))]
        public async Task<IActionResult> PutRegister(string deviceId, [FromBody] PutPlayerRegisterData putPlayerRegisterData)
        {
            //_debugConsole.Log($"PutRegister deviceId: {deviceId} body: {jsonElement.ToString()}");
            if (string.IsNullOrEmpty(deviceId)) return UnprocessableEntity("DeviceId is empty");
            //PutPlayerRegisterData putPlayerRegisterData = JsonSerializer.Deserialize<PutPlayerRegisterData>(jsonElement.GetRawText());
            if (string.IsNullOrEmpty(putPlayerRegisterData.Login)) return BadRequest("Login is empty");
            if (string.IsNullOrEmpty(putPlayerRegisterData.Password)) return BadRequest("Password is empty");

            DataSet dataSet = await _sqlAccess.SelectWhere(
                fromTable: "authors",
                selectItems: [],
                whereFilters: [$"authorLogin = '{putPlayerRegisterData.Login}'"]
                );
            DataTable dataTable = null;
            if (dataSet.Tables.Count > 0) dataTable = dataSet.Tables[0];
            if (dataTable != null)
            {
                if (dataTable.Rows.Count > 0) return Conflict("Login is unavailable");
            }
            int authorId = await _sqlAccess.InsertInto(
                intoTable: "authors",
                columns: ["authorLogin", "authorPassword"],
                values: ["'" + putPlayerRegisterData.Login + "'", "'" + putPlayerRegisterData.Password + "'"]
                );
            if (authorId >= 0)
            {
                _debugConsole.Log("PutRegister deviceId: " + deviceId);
                await _deviceStorage.CreateDeviceId(deviceId, authorId);
                string token = _tokenStorage.GenerateToken(deviceId);
                return Ok(token);
            }
            return NoContent();
        }
        #endregion
        #region Logout

        [HttpDelete("Logout/{deviceId}")]
        [SwaggerResponse(401, "Unauthorized", typeof(void))]
        [SwaggerResponse(204, "No device id in devices", typeof(void))]
        public async Task<IActionResult> DeleteLogout(string deviceId)
        {
            if (!_tokenStorage.TestToken(Request.Headers, deviceId)) return Unauthorized();
            if (string.IsNullOrEmpty(deviceId)) return UnprocessableEntity("DeviceId is empty");
            int deletedRows = await _deviceStorage.DeleteDeviceId(deviceId);
            if(deletedRows > 0)
            {
                return Ok();
            }
            return NoContent();
        }
        #endregion
    }
}
