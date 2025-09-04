using System.Collections.Generic;
using System.Web;

namespace TemplateBuilder.DTO
{
    public class RepeatingGroups
    {
        public string contentValue { get; set; }        
        public string name { get; set; }
        public string format { get; set; }
        public Queries query { get; set; }
        public List<RepeatingGroups> nestedRepeatingGroups { get; set; }

    }
}
