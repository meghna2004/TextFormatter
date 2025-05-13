using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateBuilder.Repositories
{
    public class TemplateRepository
    {
        private readonly IOrganizationService _service;
        public TemplateRepository(IOrganizationService service)
        {
            _service = service;
        }       
        /*public Guid GetTemplateConfig(string messageName,string triggerEntity,string filterAttributes,string executionMode, string stage)
        {

        }
        public EntityCollection GetTemplates(Guid templateconfigId)
        {
            string retrieveTemplates = string.Format("");
        }
        public CustomTemplates CreateTemplateModel(Guid templateId)
        {
            string retrieveTemplateSections = string.Format("");
            return;
        }*/
    }
}


