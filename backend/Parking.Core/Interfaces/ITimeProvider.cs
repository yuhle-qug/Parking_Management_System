using System;

namespace Parking.Core.Interfaces
{
    public interface ITimeProvider
    {
        DateTime Now { get; }
        DateTime UtcNow { get; }
        DateTime Today { get; }
    }
}
