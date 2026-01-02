using System;
using System.Threading.Tasks;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.External
{
    public class MockGateDevice : IGateDevice
    {
        public async Task OpenGateAsync(string gateId)
        {
            // Gi? l?p delay m? c?ng 1 giây
            await Task.Delay(500);
            Console.WriteLine($"[HARDWARE] Gate {gateId} is OPENING...");
            Console.WriteLine($"[HARDWARE] Gate {gateId} is OPEN.");
        }

        public Task<string> ReadPlateAsync()
        {
            // Gi? l?p ??c bi?n s? ng?u nhiên
            return Task.FromResult("30A-123.45");
        }
    }
}