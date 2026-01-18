namespace Parking.Core.Interfaces
{
    public interface IQrPlateValidator
    {
        string ParsePlate(string qrData);
        bool EnsureMatch(string plateFromQr, string plateSelected);
    }
}
