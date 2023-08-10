using System.Net;
using System.Threading.Tasks;

namespace USM.Devices {
    public interface IDevice {
        DeviceType DeviceType { get; }
        IPEndPoint EndPoint { get; }
        int Id { get; }
        int ProtocolVersion { get; }

        Task InitAsync();

    }
}