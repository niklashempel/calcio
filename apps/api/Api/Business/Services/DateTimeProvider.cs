using Api.Business.Interfaces;

namespace Api.Business.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;
}
