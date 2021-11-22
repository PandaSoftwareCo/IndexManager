using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace IndexManager.Models
{
    public class LogModel<TAdditionalProperties>
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string MessageTemplate { get; set; }
        public string RenderedMessage { get; set; }

        public string Exception { get; set; }

        public Dictionary<string, object> Properties { get; set; }

        private TAdditionalProperties _typedAdditionalProperties;
        public TAdditionalProperties TypedAdditionalProperties
        {
            get
            {
                if (_typedAdditionalProperties == null && Properties?.ContainsKey("AdditionalProperties") == true)
                    _typedAdditionalProperties =
                        JsonConvert.DeserializeObject<TAdditionalProperties>(Properties["AdditionalProperties"]
                            .ToString());

                return _typedAdditionalProperties;
            }
        }

        private Dictionary<string, object> _additionalProperties;
        public Dictionary<string, object> AdditionalProperties
        {
            get
            {
                if (_additionalProperties == null && Properties?.ContainsKey("AdditionalProperties") == true)
                    _additionalProperties =
                        JsonConvert.DeserializeObject<Dictionary<string, object>>(Properties["AdditionalProperties"]
                            .ToString());

                return _additionalProperties;
            }
        }
    }
}
