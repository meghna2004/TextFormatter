using System.Collections.Generic;

namespace TemplateBuilder.DTO
{
    public class Queries
    {
        public string name {  get; set; }
        public string queryText { get; set; }
        public string format { get; set; }
        public string formatValues { get; set; }
        public int sequence { get; set; }
        public List<SubSections> subSections { get; set; }
        //public List<QueryPlaceholders> placeholders { get; set; }
    }
}
