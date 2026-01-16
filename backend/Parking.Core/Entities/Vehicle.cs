namespace Parking.Core.Entities
{
    // [OOP - Abstraction]: Lớp trừu tượng định nghĩa khung sườn cho mọi xe
    public abstract class Vehicle
    {
        public string LicensePlate { get; set; } = string.Empty;

        protected Vehicle() { }

        protected Vehicle(string licensePlate)
        {
            LicensePlate = licensePlate;
        }

        // [OOP - Polymorphism]: Mỗi loại xe sẽ tự định nghĩa hệ số phí riêng
        public abstract double GetFeeFactor();
    }

    // --- Các lớp con cụ thể (Concrete Classes) ---

    public class Car : Vehicle
    {
        public Car() { }
        public Car(string plate) : base(plate) { }
        public override double GetFeeFactor() => 1.0; // Hệ số chuẩn
    }

    public class ElectricCar : Car
    {
        public ElectricCar() { }
        public ElectricCar(string plate) : base(plate) { }
        // Ưu đãi cho xe điện: giảm phí (ví dụ hệ số 0.8)
        public override double GetFeeFactor() => 0.8;
    }

    public class Motorbike : Vehicle
    {
        public Motorbike() { }
        public Motorbike(string plate) : base(plate) { }
        public override double GetFeeFactor() => 0.5; // Xe máy rẻ hơn
    }

    public class ElectricMotorbike : Motorbike
    {
        public ElectricMotorbike() { }
        public ElectricMotorbike(string plate) : base(plate) { }
        public override double GetFeeFactor() => 0.4;
    }

    public class Bicycle : Vehicle
    {
        public Bicycle() { }
        public Bicycle(string plate) : base(plate) { }
        public override double GetFeeFactor() => 0.2; // Xe đạp phí thấp nhất
    }

}