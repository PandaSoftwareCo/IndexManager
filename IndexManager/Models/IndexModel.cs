using System.Collections.ObjectModel;
using IndexManager.Framework;
using Newtonsoft.Json;

namespace IndexManager.Models
{
    public class IndexModel : BaseModel
    {
        private bool _selected;
        [JsonIgnore]
        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    OnPropertyChanged(nameof(Selected));
                }
            }
        }

        private string _name;
        [JsonProperty("index")]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private string _definition;
        [JsonIgnore]
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

        private bool _hasAlias;
        [JsonIgnore]
        public bool HasAlias
        {
            get
            {
                return _hasAlias;
            }
            set
            {
                if (_hasAlias != value)
                {
                    _hasAlias = value;
                    OnPropertyChanged(nameof(HasAlias));
                }
            }
        }

        private ObservableCollection<AliasModel> _aliases;
        [JsonIgnore]
        public ObservableCollection<AliasModel> Aliases
        {
            get
            {
                return _aliases;
            }
            set
            {
                if (_aliases != value)
                {
                    _aliases = value;
                    OnPropertyChanged(nameof(Aliases));
                }
            }
        }

        private string _uuid;
        [JsonProperty("uuid")]
        public string Uuid
        {
            get
            {
                return _uuid;
            }
            set
            {
                if (_uuid != value)
                {
                    _uuid = value;
                    OnPropertyChanged(nameof(Uuid));
                }
            }
        }

        private int? _docsCount;
        [JsonProperty("docs.count")]
        public int? DocsCount
        {
            get
            {
                return _docsCount;
            }
            set
            {
                if (_docsCount != value)
                {
                    _docsCount = value;
                    OnPropertyChanged(nameof(DocsCount));
                }
            }
        }

        private string _storeSize;
        [JsonProperty("store.size")]
        public string StoreSize
        {
            get
            {
                return _storeSize;
            }
            set
            {
                if (_storeSize != value)
                {
                    _storeSize = value;
                    OnPropertyChanged(nameof(StoreSize));
                }
            }
        }

        private string _status;
        [JsonProperty("status")]
        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        private string _health;
        [JsonProperty("health")]
        public string Health
        {
            get
            {
                return _health;
            }
            set
            {
                if (_health != value)
                {
                    _health = value;
                    OnPropertyChanged(nameof(Health));
                }
            }
        }

        private string _pri;
        [JsonProperty("pri")]
        public string Pri
        {
            get
            {
                return _pri;
            }
            set
            {
                if (_pri != value)
                {
                    _pri = value;
                    OnPropertyChanged(nameof(Pri));
                }
            }
        }

        private string _rep;
        [JsonProperty("rep")]
        public string Rep
        {
            get
            {
                return _rep;
            }
            set
            {
                if (_rep != value)
                {
                    _rep = value;
                    OnPropertyChanged(nameof(Rep));
                }
            }
        }

        public IndexModel()
        {
            Aliases = new ObservableCollection<AliasModel>();
        }
    }
}
