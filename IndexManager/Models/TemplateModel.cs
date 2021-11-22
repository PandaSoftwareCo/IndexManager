using IndexManager.Framework;
using Newtonsoft.Json;

namespace IndexManager.Models
{
    public class TemplateModel : BaseModel
    {
        public string Name { get; set; }
        [JsonProperty("index_patterns")]
        public string IndexPatterns { get; set; }
        public string Order { get; set; }
        public string Version { get; set; }
        [JsonProperty("composed_of")]
        public string ComposedOf { get; set; }

        private string _definition;
        public string Definition
        {
            get
            {
                return _definition;
            }
            set
            {
                if (_definition != value)
                {
                    _definition = value;
                    OnPropertyChanged(nameof(Definition));
                }
            }
        }
    }
}
