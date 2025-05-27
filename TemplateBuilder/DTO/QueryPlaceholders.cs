using Microsoft.Xrm.Sdk;

namespace TemplateBuilder.DTO
{
    public class QueryPlaceholders
    {
        public OptionSetValue dataType { get; set; }
        public OptionSetValue guidType { get; set; }
        public string name { get; set; }
        public string value { get; set; }
        public int sequence { get; set; }
    }
}
