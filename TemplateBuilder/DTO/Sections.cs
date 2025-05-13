using System.Collections.Generic;

namespace TextFormatter.TemplateBuilder
{
    public class Sections
    {
        public int sequence { get; set; }
        public string format { get; set; }
        public List<Queries> queryClasses { get; set; }
    }
}
