using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Moq;
using TemplateBuilder.Services;
using Microsoft.Xrm.Sdk.Query;
using TemplateBuilder.DTO;

namespace TemplateBuilder.Repositories.Tests
{
    [TestClass()]
    public class TemplateRepositoryTests
    {
        [TestMethod()]
        public void CreateTemplateModelTest()
        {
            //Arrange
            var mockService = new Mock<IOrganizationService>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockContext = new Mock<IPluginExecutionContext>();
            var mockTracing = new Mock<ITracingService>();
            var mockPluginConfig = new Mock<PluginConfigService>(mockService.Object, null);
            Guid descriptionID = Guid.NewGuid();
            var query = @"<fetch>
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
                        </fetch>";
            Entity testEntity = new Entity("vig_query", Guid.NewGuid());
            testEntity["vig_name"] = "sampleQuery";
            testEntity["vig_fetchquery"] = query;
            testEntity["vig_format"] = "";
            testEntity["vig_fetchsequence"] = 1;
            testEntity["TBD.vig_textformat"] = new AliasedValue("TBD", "vig_textformat", "Hello {{sampleQuery-name}}. Here is your order number: {{sampleQuery-co.ordernumber}} Here are the order details: {{sampleQuery-sampleRepeatingGroup}}");
            testEntity["RG.vig_name"] = new AliasedValue("RG", "vig_name", "sampleRepeatingGroup");
            testEntity["RG.vig_format"] = new AliasedValue("RG", "vig_format", "Order placed on: {{prod.orderdate:dd/MM/yyyy}} Product Name: {{prod.productName}} Price: £{{prod.price:0.00}} Amount: {{prod.amount}}[[suffix:kg]]");
            var entityCollection = new EntityCollection(new List<Entity> { testEntity });
            mockService.Setup(s => s.RetrieveMultiple(It.IsAny<FetchExpression>())).Returns(entityCollection);

            var tempRepo = new TemplateRepository(mockService.Object, mockContext.Object, mockTracing.Object, mockPluginConfig.Object);

            var repeatingGroup = new List<RepeatingGroups>{new RepeatingGroups
            {
                name = "sampleRepeatingGroup",
                format = "Order placed on: {{prod.orderdate:dd/MM/yyyy}} Product Name: {{prod.productName}} Price: £{{prod.price:0.00}} Amount: {{prod.amount}}[[suffix:kg]]"
            } };
            var templateModel = new TextDescriptionBodies
            {
                structure = "Hello {{sampleQuery-name}}. Here is your order number: {{sampleQuery-co.ordernumber}} Here are the order details: {{sampleQuery-sampleRepeatingGroup}}",
                queries = new List<Queries>
                {
                    new Queries
                    {
                        name = "sampleQuery",
                        queryText = query,
                        sequence = 1,
                        repeatingGroups =repeatingGroup
                    }
                }
            };
            var expectedResult = templateModel;
            //Act
            var result = tempRepo.CreateTemplateModel(descriptionID);

            //Assert
            Assert.AreEqual(expectedResult.structuredValue, result.structuredValue);
            Assert.AreEqual(expectedResult.structure, result.structure);
            foreach (var r in result.queries)
            {
                Assert.AreEqual("sampleQuery", r.name);
                Assert.AreEqual(query, r.queryText);
                Assert.AreEqual(1, r.sequence);
                foreach (var rg in r.repeatingGroups)
                {
                    Assert.AreEqual("sampleRepeatingGroup", rg.name);
                    Assert.AreEqual("Order placed on: {{prod.orderdate:dd/MM/yyyy}} Product Name: {{prod.productName}} Price: £{{prod.price:0.00}} Amount: {{prod.amount}}[[suffix:kg]]", rg.format);

                }
            }
        }
        [TestMethod]
        public void GetTemplatesTest()
        {

        }
    }
}