using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IdentityModel.Metadata;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplateBuilder.DTO;
using TemplateBuilder.Enum;

namespace TemplateBuilder.Utilities
{
    public class QueryBuilder
    {
        private readonly IOrganizationService _service;
        private readonly IPluginExecutionContext _context;
        private readonly ITracingService _tracing;
        private readonly Entity _primaryEntity;
        public QueryBuilder(IOrganizationService service, IPluginExecutionContext context, ITracingService tracing)
        {
            _service = service;
            _context = context;
            _tracing = tracing;
            _primaryEntity = service.Retrieve(context.PrimaryEntityName,context.PrimaryEntityId,new ColumnSet(true));
        }
        public string FormatQueryWithPlaceholders(string fetchXML, List<QueryPlaceholders> placeholders)
        {
            //Check placeholder name field with placeholder text inside fetchXMLtext inside {}.
            //Retrive values as specified.
            _tracing.Trace("Create TokenProcessor");
            //Need to use tokens here as well.
            TokenProcessor processor = new TokenProcessor(_tracing);
            string replacedQuery = string.Empty;
            foreach (var qp in placeholders)
            {
                _tracing.Trace("Inside Query placeholders");

                if (qp.value == null || qp.value == string.Empty)
                {
                   replacedQuery= processor.ReplaceTokens(qp.name,fetchXML,_primaryEntity);                   
                }
            }
            _tracing.Trace("TokenProcessor Successfull");
            return replacedQuery;
        }
    } 
}
