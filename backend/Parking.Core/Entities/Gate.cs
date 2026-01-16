using System.Collections.Generic;

namespace Parking.Core.Entities
{
    public class Gate
    {
        public string GateId { get; set; }
        public string Name { get; set; }
        public string Direction { get; set; } // "In" or "Out"
        public string Status { get; set; } // "Active", "Maintenance", "Closed"
        
        // List of vehicle categories this gate accepts (e.g., "CAR", "MOTORBIKE")
        public List<string> AllowedVehicleCategories { get; set; } = new List<string>();

        public bool CanAccept(string vehicleCategory)
        {
            if (Status != "Active") return false;
            // If empty, assume all? No, safer to assume none if empty for "In" gates.
            if (AllowedVehicleCategories == null || AllowedVehicleCategories.Count == 0) return false;
            
            return AllowedVehicleCategories.Contains(vehicleCategory?.ToUpper());
        }
    }
}
