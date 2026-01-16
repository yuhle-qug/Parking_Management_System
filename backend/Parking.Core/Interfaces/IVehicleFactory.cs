using Parking.Core.Entities;

namespace Parking.Core.Interfaces
{
    public interface IVehicleFactory
    {
        Vehicle CreateVehicle(string type, string plate);
    }
}
