using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AddnPrintHD.Models
{
    public class ApiResponse
    {
        [JsonPropertyName("value")]
        public List<AvailabilityOption> Value { get; set; }
        public string DeviceId { get; set; }

        [JsonProperty("value")]
        public List<DeviceModel> Devices { get; set; }
    }
}
