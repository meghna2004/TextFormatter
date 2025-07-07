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
using System.Collections;

namespace TemplateBuilder.Services.Tests
{
    [TestClass()]
    public class ContentGeneratorServiceTests
    {
        private Mock<IOrganizationService> _mockService;
        private Mock<IPluginExecutionContext> _mockContext;
        private Mock<ITracingService> _mockTracing;
        private Mock<TemplateRepository> _mockTemplateRepo;
        private Mock<PluginConfigService> _mockPluginConfig;
        private ContentGeneratorService _mockcontentGeneratorService;
        Guid _descriptionID = Guid.NewGuid();

        [TestInitialize]
        public void Setup()
        {
            _mockService = new Mock<IOrganizationService>();
            _mockContext = new Mock<IPluginExecutionContext>();
            _mockTracing = new Mock<ITracingService>();
            _mockPluginConfig = new Mock<PluginConfigService>(_mockService.Object, null);
            _mockTemplateRepo = new Mock<TemplateRepository>(_mockService.Object, _mockContext.Object, _mockTracing.Object, _mockPluginConfig.Object);
           
        }
        [TestMethod()]
        public void T01_BuildContent_ValidInputs()
        {
            //Arrange
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
            _mockTemplateRepo.Setup(tr => tr.CreateTemplateModel(_descriptionID)).Returns(templateModel);
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
            _mockService.Setup(s => s.RetrieveMultiple(It.IsAny<FetchExpression>())).Returns(entityCollection);
            _mockService.Setup(s => s.Retrieve("account", primaryEntity.Id, It.IsAny<ColumnSet>())).Returns(primaryEntity);
            _mockContext.Setup(c => c.PrimaryEntityName).Returns("account");
            _mockContext.Setup(c => c.PrimaryEntityId).Returns(primaryEntity.Id);

            var expected = "Hello Meghna. Here is your order number: CO-754112 Here are the order details: Order placed on: 01/06/2025 Product Name: SampleProduct Price: £53.00 Amount: 20kgOrder placed on: 11/06/2025 Product Name: SampleProduct2 Price: £15.00 Amount: 5kg";
            _mockcontentGeneratorService = new ContentGeneratorService(
                 _mockService.Object,
                 _mockContext.Object,
                 _mockTracing.Object,
                 _mockTemplateRepo.Object,
                 _descriptionID);
             //Act
             var buildContentResult = _mockcontentGeneratorService.BuildContent();           

            //Assert
            Assert.AreEqual(expected, buildContentResult);
        }
        [TestMethod()]
        public void T02_BuildContent_ValidInputsNoResult()
        {
            //Arrange

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
            _mockTemplateRepo.Setup(tr => tr.CreateTemplateModel(descriptionID)).Returns(templateModel);
            var entityCollection = new EntityCollection(new List<Entity>());
            _mockService.Setup(s => s.RetrieveMultiple(It.IsAny<FetchExpression>())).Returns(entityCollection);
            _mockService.Setup(s => s.Retrieve("account", primaryEntity.Id, It.IsAny<ColumnSet>())).Returns(primaryEntity);
            _mockContext.Setup(c => c.PrimaryEntityName).Returns("account");
            _mockContext.Setup(c => c.PrimaryEntityId).Returns(primaryEntity.Id);

            _mockcontentGeneratorService = new ContentGeneratorService(
                   _mockService.Object,
                   _mockContext.Object,
                   _mockTracing.Object,
                   _mockTemplateRepo.Object,
                   descriptionID);
            var expected = "Hello . Here is your order number:  Here are the order details: ";

            //Act
            var buildContentResult = _mockcontentGeneratorService.BuildContent();


            //Assert
            Assert.AreEqual(expected, buildContentResult);
        }
        [TestMethod()]
        public void T02_BuildContent_MultipleQueries()
        {
            //Arrange

            Guid descriptionID = Guid.NewGuid();

            Entity primaryEntity = new Entity() { Id = Guid.NewGuid() };
            primaryEntity["name"] = "Meghna";
            var queryText1 = "<fetch><entity name='contact'/> <attribute name='firstname' /></fetch>";
            var queryText2 = "<fetch><entity name='account'/> <attribute name='firstname' /></fetch>";
            var repeatingGroup1 = new List<RepeatingGroups>{new RepeatingGroups
            {
                name = "sampleRepeatingGroup",
                format = "Hello {{firstname}}"
            } };
            var repeatingGroup2 = new List<RepeatingGroups>{new RepeatingGroups
            {
                name = "sampleRepeatingGroup2",
                format = "Hello {{firstname}}"
            } };
            var templateModel = new TextDescriptionBodies
            {
                structure = "Hello {{sampleQuery-firstname}} and {{sampleQuery2-firstname}}",
                queries = new List<Queries>
                {
                    new Queries
                    {
                        name = "sampleQuery",
                        queryText = queryText1,
                        sequence = 1
                    },
                    new Queries
                    {
                        name = "sampleQuery2",
                         queryText = queryText2,
                        sequence = 1
                    }
                   
                }
            };
            _mockTemplateRepo.Setup(tr => tr.CreateTemplateModel(descriptionID)).Returns(templateModel);
            Entity testEntity = new Entity("account", Guid.NewGuid());
            testEntity["firstname"] = "Alice";

            Entity testEntity2 = new Entity("contact", Guid.NewGuid());
            testEntity2["firstname"] = "Jane";

            var entityCollection = new EntityCollection(new List<Entity>() {testEntity});
            var entityCollection2 = new EntityCollection(new List<Entity>() { testEntity2 });
            _mockService.SetupSequence(s => s.RetrieveMultiple(It.IsAny<FetchExpression>()))
            .Returns(entityCollection)   // First call → Alice
            .Returns(entityCollection2); // Second call → Jane

            _mockService.Setup(s => s.Retrieve("account", primaryEntity.Id, It.IsAny<ColumnSet>())).Returns(primaryEntity);
            _mockContext.Setup(c => c.PrimaryEntityName).Returns("account");
            _mockContext.Setup(c => c.PrimaryEntityId).Returns(primaryEntity.Id);

            _mockcontentGeneratorService = new ContentGeneratorService(
                   _mockService.Object,
                   _mockContext.Object,
                   _mockTracing.Object,
                   _mockTemplateRepo.Object,
                   descriptionID);
            var expected = "Hello Alice and Jane";

            //Act
            var buildContentResult = _mockcontentGeneratorService.BuildContent();


            //Assert
            Assert.AreEqual(expected, buildContentResult);
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void ExecuteFetchAndPopulateValues_InvalidXml_ThrowsException()
        {
            // Arrange
            var repeatingGroups = new List<RepeatingGroups>();

            string badXml = "<fetch><entity></fetch>"; // malformed
            var templateModel = new TextDescriptionBodies
            {
                structure = "Hello {{sampleQuery-name}}. Here is your order number: {{sampleQuery-co.ordernumber}} Here are the order details: {{sampleQuery-sampleRepeatingGroup}}",
                queries = new List<Queries>
                {
                    new Queries
                    {
                        name = "sampleQuery",
                        queryText = badXml,
                        sequence = 1,
                        repeatingGroups = repeatingGroups
                    }
                }
            };
            _mockTemplateRepo.Setup(tr => tr.CreateTemplateModel(_descriptionID)).Returns(templateModel);

            _mockcontentGeneratorService = new ContentGeneratorService(
                   _mockService.Object,
                   _mockContext.Object,
                   _mockTracing.Object,
                   _mockTemplateRepo.Object,
                   _descriptionID);
            // Act
            _mockcontentGeneratorService.ExecuteFetchAndPopulateValues("BadQuery", badXml, repeatingGroups);

            // Assert: Exception expected
        }

        [TestMethod]
        public void ExecuteFetchAndPopulateValues_ReturnsContentForOneEntity()
        {
            // Arrange
            var entity = new Entity("contact") { Id = Guid.NewGuid() };
            entity["firstname"] = "Meghna";
            var query = "<fetch><entity name='contact'/> <attribute name='firstname' /></fetch>";
            var entities = new EntityCollection(new List<Entity> { entity });

            _mockService.Setup(s => s.RetrieveMultiple(It.IsAny<FetchExpression>()))
                        .Returns(entities);

            var repeatingGroups = new List<RepeatingGroups>
            {
                new RepeatingGroups { format = "Hello {{firstname}}", name = "greeting" }
            };
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
                        repeatingGroups = repeatingGroups
                    }
                }
            };
            var expectedContentValue = "Hello Meghna";
            _mockTemplateRepo.Setup(tr => tr.CreateTemplateModel(_descriptionID)).Returns(templateModel);
            _mockcontentGeneratorService = new ContentGeneratorService(
                   _mockService.Object,
                   _mockContext.Object,
                   _mockTracing.Object,
                   _mockTemplateRepo.Object,
                   _descriptionID);
            // Act
            var result = _mockcontentGeneratorService.ExecuteFetchAndPopulateValues("ContactQuery", query, repeatingGroups) as List<RepeatingGroups>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result[0].contentValue.Contains("Hello"));
            Assert.AreEqual(result[0].contentValue, expectedContentValue);
        }
    }
}
