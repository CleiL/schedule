namespace Schedule.Core.Interfaces
{
    public interface ISchemaInitializer
    {
        Task EnsureCreatedAsync(CancellationToken ct = default);
    }
}
