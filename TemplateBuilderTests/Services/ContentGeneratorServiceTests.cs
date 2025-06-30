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
using TemplateBuilder.DTO;
using Microsoft.Xrm.Sdk.Query;

namespace TemplateBuilder.Services.Tests
{
    [TestClass()]
    public class ContentGeneratorServiceTests
    {
        [TestMethod()]
        public void T01_BuildContentAndExecuteFetch_ValidInputs()
        {
            //Arrange
            var mockService = new Mock<IOrganizationService>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockContext = new Mock<IPluginExecutionContext>();
            var mockTracing = new Mock<ITracingService>();
            var mockPluginConfig = new Mock<PluginConfigService>(mockService.Object, null);
            var mockTemplateRepo = new Mock<TemplateRepository>(mockService.Object, mockContext.Object, mockTracing.Object, mockPluginConfig.Object);
            Guid descriptionID = Guid.NewGuid();

            Entity primaryEntity = new Entity() { Id = Guid.NewGuid() };
            primaryEntity["name"] = "Meghna";
            var queryText = @"<fetch>
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
                        queryText = queryText,
                        sequence = 1,
                        repeatingGroups =repeatingGroup
                    }
                }
            };
            mockTemplateRepo.Setup(tr => tr.CreateTemplateModel(descriptionID)).Returns(templateModel);
            Entity testEntity = new Entity("account", Guid.NewGuid());
            testEntity["name"] = "Meghna";
            testEntity["co.ordernumber"] = new AliasedValue("co", "ordernumber", "CO-754112");
            testEntity["prod.orderdate"] = new AliasedValue("prod", "orderdate", new DateTime(2025, 06, 1, 14, 30, 06));
            testEntity["prod.productName"] = new AliasedValue("prod", "productName", "SampleProduct");
            testEntity["prod.price"] = new AliasedValue("prod", "price", new Decimal(53));
            testEntity["prod.amount"] = new AliasedValue("prod", "amount", new Decimal(20));
            Entity testEntity2 = new Entity("account", Guid.NewGuid());
            testEntity2["name"] = "Meghna";
            testEntity2["co.ordernumber"] = new AliasedValue("co", "ordernumber", "CO-754112");
            testEntity2["prod.orderdate"] = new AliasedValue("prod", "orderdate", new DateTime(2025, 06, 11, 14, 30, 06));
            testEntity2["prod.productName"] = new AliasedValue("prod", "productName", "SampleProduct2");
            testEntity2["prod.price"] = new AliasedValue("prod", "price", new Decimal(15));
            testEntity2["prod.amount"] = new AliasedValue("prod", "amount", new Decimal(5));

            var entityCollection = new EntityCollection(new List<Entity> { testEntity, testEntity2 });
            mockService.Setup(s => s.RetrieveMultiple(It.IsAny<FetchExpression>())).Returns(entityCollection);
            mockService.Setup(s => s.Retrieve("account", primaryEntity.Id, It.IsAny<ColumnSet>())).Returns(primaryEntity);
            mockContext.Setup(c => c.PrimaryEntityName).Returns("account");
            mockContext.Setup(c => c.PrimaryEntityId).Returns(primaryEntity.Id);

            var builder = new ContentGeneratorService(mockService.Object, mockContext.Object, mockTracing.Object, mockTemplateRepo.Object, descriptionID);
            var expected = "Hello Meghna. Here is your order number: CO-754112 Here are the order details: Order placed on: 01/06/2025 Product Name: SampleProduct Price: £53.00 Amount: 20kgOrder placed on: 11/06/2025 Product Name: SampleProduct2 Price: £15.00 Amount: 5kg";
           
            //Act
            var buildContentResult = builder.BuildContent();
            
            var executeFetchResult = builder.ExecuteFetchAndPopulateValues("sampleQuery", queryText, repeatingGroup);
            foreach (var rg in repeatingGroup)
            {
                rg.contentValue = "Order placed on: 01/06/2025 Product Name: SampleProduct Price: £53.00 Amount: 20kgOrder placed on: 11/06/2025 Product Name: SampleProduct2 Price: £15.00 Amount: 5kg";
            }
            var executeExpected = repeatingGroup;

            //Assert
            Assert.AreEqual(expected, buildContentResult);
            Assert.AreEqual(executeExpected, executeFetchResult);
        }       
    }
}
