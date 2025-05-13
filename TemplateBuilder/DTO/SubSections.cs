using System.Collections.Generic;

namespace TextFormatter.TemplateBuilder
{
    public class SubSections
    {
        public int sequence { get; set; }
        public List<Columns> content { get; set; }
        public string contentValue { get; set; }        
        public string name { get; set; }
        public string format { get; set; }
        public string defaultFormat = "<p>placeholder</p>";
    }
}
