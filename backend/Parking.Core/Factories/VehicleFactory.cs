using System;
using System.Collections.Generic;
using Parking.Core.Constants;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Core.Factories
{
    public class VehicleFactory : IVehicleFactory
    {
        private readonly Dictionary<string, Func<string, Vehicle>> _creators;

        public VehicleFactory()
        {
            _creators = new Dictionary<string, Func<string, Vehicle>>(StringComparer.OrdinalIgnoreCase)
            {
                { ParkingConstants.VehicleType.Car, plate => new Car(plate) },
                { ParkingConstants.VehicleType.ElectricCar, plate => new ElectricCar(plate) },
                { ParkingConstants.VehicleType.Motorbike, plate => new Motorbike(plate) },
                { ParkingConstants.VehicleType.ElectricMotorbike, plate => new ElectricMotorbike(plate) },
                { ParkingConstants.VehicleType.Bicycle, plate => new Bicycle(plate) }
            };
        }

        public Vehicle CreateVehicle(string type, string plate)
        {
            var normalizedType = (type ?? string.Empty).Trim().ToUpperInvariant();
            
            if (_creators.TryGetValue(normalizedType, out var creator))
            {
                return creator(plate);
            }

            // Fallback default
            return new Car(plate);
        }
    }
}
