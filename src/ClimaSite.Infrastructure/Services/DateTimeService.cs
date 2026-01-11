using ClimaSite.Application.Common.Interfaces;

namespace ClimaSite.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}
