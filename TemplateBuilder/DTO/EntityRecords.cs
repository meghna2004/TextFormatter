using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateBuilder.DTO
{
    public class EntityRecords
    {
      public  List<EntityRecords> childRecords { get; set; }
        public string attributeName { get; set; }
        public object attributeValue { get; set; }
    }
}
