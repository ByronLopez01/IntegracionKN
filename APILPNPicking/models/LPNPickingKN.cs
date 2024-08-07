using Newtonsoft.Json;

namespace APILPNPicking.models
{
    public class LPNPickingKN
    {
        [JsonProperty("SORT_INDUCTION")]
        public SortInduction SORT_INDUCTION { get; set; }
    }
}
