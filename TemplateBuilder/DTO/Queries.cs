using System.Collections.Generic;

namespace TextFormatter.TemplateBuilder
{
    public class Queries
    {
        public string queryText { get; set; }
        public int sequence { get; set; }
        public List<SubSections> contentClasses { get; set; }
    }
}
