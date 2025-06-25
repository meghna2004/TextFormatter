using System.Collections.Generic;

namespace TemplateBuilder.DTO
{
    public class Queries
    {
        public string name {  get; set; }
        public string queryText { get; set; }
        public int sequence { get; set; }
        public List<RepeatingGroups> repeatingGroups { get; set; }
    }
}
