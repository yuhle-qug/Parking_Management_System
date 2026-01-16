namespace Parking.Core.Constants
{
    public static class ParkingConstants
    {
        public static class TicketType
        {
            public const string Daily = "Daily";
            public const string Monthly = "Monthly";
        }

        public static class ParkingSessionStatus
        {
            public const string Active = "Active";
            public const string Completed = "Completed";
            public const string PendingPayment = "PendingPayment";
            public const string LostTicket = "LostTicket";
        }
        
        public static class PaymentStatus
        {
            public const string Pending = "Pending";
            public const string Completed = "Completed";
            public const string Failed = "Failed";
            public const string PendingExternal = "PendingExternal";
        }

        public static class VehicleType
        {
            public const string Car = "CAR";
            public const string ElectricCar = "ELECTRIC_CAR";
            public const string Motorbike = "MOTORBIKE";
            public const string ElectricMotorbike = "ELECTRIC_MOTORBIKE";
            public const string Bicycle = "BICYCLE";
        }
    }
}
