using Newtonsoft.Json;
using System;

namespace AddnPrintHD.Models
{
    public class DeviceModel
    {
        [JsonProperty("cra87_is_inventory_device_modelid")]
        public Guid DeviceModelId { get; set; }

        [JsonProperty("cra87_id")]
        public string Cra87Id { get; set; }
    }
}
