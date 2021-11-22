using IndexManager.Interfaces;

namespace IndexManager.Models
{
    public class AppSettings : IAppSettings
    {
        public string Url { get; set; }
        public string IndexName { get; set; }
        public int Size { get; set; }
        public int Count { get; set; }

    }
}
