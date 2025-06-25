using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplateBuilder.Utilities;

namespace TemplateBuilderTests.TestableClass
{
    public class TestableClass1 : TokenProcessor
    {
        private readonly Entity _fakeEntityReferenceRecord;

        public TestableClass1(ITracingService tracing, IOrganizationService service, IPluginExecutionContext context, Entity fakeReference)
            : base(tracing, service, context)
        {
            _fakeEntityReferenceRecord = fakeReference;
        }

        public override  Entity GetEntityReferenceRecord(string entityName)
        {
            return _fakeEntityReferenceRecord;
        }
    }

}
