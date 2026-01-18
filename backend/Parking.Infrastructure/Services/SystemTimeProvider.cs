using System;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Services
{
    public class SystemTimeProvider : ITimeProvider
    {
        public DateTime Now => DateTime.Now;
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime Today => DateTime.Today;
    }
}
