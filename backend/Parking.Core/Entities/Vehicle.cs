namespace Parking.Core.Entities
{
    // [OOP - Abstraction]: Lớp trừu tượng định nghĩa khung sườn cho mọi xe
    public abstract class Vehicle
    {
        public ValueObjects.LicensePlate LicensePlate { get; private set; }

        // Parameterless constructor for ORM/Serialization frameworks (protected to prevent invalid instantiation)
        protected Vehicle() { }

        protected Vehicle(string licensePlate)
        {
            LicensePlate = ValueObjects.LicensePlate.Create(licensePlate);
        }

        // [OOP - Polymorphism]: Mỗi loại xe sẽ tự định nghĩa hệ số phí riêng
        public abstract double GetFeeFactor();

        // Helper for Serialization/Frontend to know the type
        public string VehicleType => GetType().Name;
    }

    // --- Các lớp con cụ thể (Concrete Classes) ---

    public class Car : Vehicle
    {
        // Removed public parameterless constructor to enforce validation
        protected Car() { } 
        public Car(string plate) : base(plate) { }
        public override double GetFeeFactor() => 1.0; // Hệ số chuẩn
    }

    public class ElectricCar : Car
    {
        protected ElectricCar() { }
        public ElectricCar(string plate) : base(plate) { }
        // Ưu đãi cho xe điện: giảm phí (ví dụ hệ số 0.8)
        public override double GetFeeFactor() => 0.8;
    }

    public class Motorbike : Vehicle
    {
        protected Motorbike() { }
        public Motorbike(string plate) : base(plate) { }
        public override double GetFeeFactor() => 0.5; // Xe máy rẻ hơn
    }

    public class ElectricMotorbike : Motorbike
    {
        protected ElectricMotorbike() { }
        public ElectricMotorbike(string plate) : base(plate) { }
        public override double GetFeeFactor() => 0.4;
    }

    public class Bicycle : Vehicle
    {
        protected Bicycle() { }
        public Bicycle(string plate) : base(plate) { }
        public override double GetFeeFactor() => 0.2; // Xe đạp phí thấp nhất
    }

}