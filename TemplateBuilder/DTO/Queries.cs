using System.Collections.Generic;

namespace TemplateBuilder.DTO
{
    public class Queries
    {
        public string queryText { get; set; }
        public int sequence { get; set; }
        public List<SubSections> contentClasses { get; set; }
        public List<QueryPlaceholders> placeholders { get; set; }
    }
}
