using Microsoft.VisualStudio.TestTools.UnitTesting;
using TemplateBuilder.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Moq;
using TemplateBuilder.Repositories;
using static TextFormatter.PluginBase;
using TemplateBuilder.DTO;

namespace TemplateBuilder.Services.Tests
{
    [TestClass()]
    public class ContentGeneratorServiceTests 
    {
        [TestMethod()]
        public void T01_BuildContentTest()
        {
            //Arrange
            var mockService = new Mock<IOrganizationService>();
            var mockContext = new Mock<IPluginExecutionContext>();
            var mockTracing = new Mock<ITracingService>();
            var mockTemplateRepo = new Mock<TemplateRepository>();
            //var mockLocalContext = new Mock<LocalPluginContext>();

            var templateModel = new TextDescriptionBodies
            {
                structure = "Hello {{sampleQuery-name}}. Here is your order number: {{sampleQuery-co.ordernumber}} Here are the order details: {{sampleQuery-sampleRepeatingGroup}}",
                queries = new List<Queries>
                {
                    new Queries
                    {
                        name = "sampleQuery",
                        queryText = "",
                        sequence = 1,
                        repeatingGroups = new List<RepeatingGroups>
                        {
                            new RepeatingGroups
                            {
                                name = "sampleRepeatingGroup",
                                format = "Order placed on: {{prod.orderdate:dd/MM/yyyy}} Product Name: {{prod.productName}} Price: £{{prod.price:0.00}} Amount: {{prod.amount}}[[suffix:kg]]"
                            }
                        }
                    }
                }
            };
            //Act

            //Assert
            Assert.Fail();
        }
    }
}