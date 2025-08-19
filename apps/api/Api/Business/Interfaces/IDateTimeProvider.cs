namespace Api.Business.Interfaces;

public interface IDateTimeProvider
{
    public DateTime Now { get; }
}