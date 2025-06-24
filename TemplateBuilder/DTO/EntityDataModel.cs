using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateBuilder.DTO
{
    public class EntityDataModel
    {
        public string queryName { get; set; }
        public List<Dictionary<string,EntityRecords>> entityRecords { get; set; }
    }
}
