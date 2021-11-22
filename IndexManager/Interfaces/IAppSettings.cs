namespace IndexManager.Interfaces
{
    public interface IAppSettings
    {
        string Url { get; set; }
        string IndexName { get; set; }
        int Size { get; set; }
        int Count { get; set; }
    }
}
