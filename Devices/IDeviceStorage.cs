using System.Data;

namespace BlogAppBackend.Devices
{
    public interface IDeviceStorage
    {
        public Task<bool> CreateDeviceId(string deviceId, int authorId);
        public Task<int> GetUserByDeviceId(string deviceId);
        public Task<int> DeleteDeviceId(string deviceId);
    }
}