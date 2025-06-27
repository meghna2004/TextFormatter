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
            Guid descriptionID = Guid.NewGuid();
            var templateModel = new TextDescriptionBodies
            {
                structure = "Hello {{sampleQuery-name}}. Here is your order number: {{sampleQuery-co.ordernumber}} Here are the order details: {{sampleQuery-sampleRepeatingGroup}}",
                queries = new List<Queries>
                {
                    new Queries
                    {
                        name = "sampleQuery",
                        queryText = @"<fetch>
                                        <entity name='account'>
                                        <attribute name='name' />
                                        <link-entity name='order' from='orderid' to='orderid' link-type='outer' alias='co'>
                                            <attribute name='ordernumber' />
                                            <link-entity name='product' from='productid' to='productid' link-type='outer' alias='prod'>
                                                <attribute name='orderdate' />
                                                <attribute name='productName' />
                                                <attribute name='price' />
                                                <attribute name='ammount' />
                                            </link-entity>
                                        </link-entity>
                                        </entity>
                                    </fetch>",
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
            mockTemplateRepo.Setup(tr=>tr.CreateTemplateModel(descriptionID)).Returns(templateModel);
            Entity testEntity = new Entity("account",Guid.NewGuid());
            testEntity["name"] = "Meghna";
            testEntity["co.ordernumber"] = "CO-754112";
           // testEntity["prod.orderdate"] = new DateTime(1,1,1,)
            testEntity["name"] = "Meghna";
            testEntity["name"] = "Meghna";
            testEntity["name"] = "Meghna";
            //setup tokenprocessor
            //setup
            //Act

            //Assert
            Assert.Fail();
        }
    }
}