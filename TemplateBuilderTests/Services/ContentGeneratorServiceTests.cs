using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Moq;
using TemplateBuilder.Repositories;
using TemplateBuilder.DTO;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

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
        public void T03_BuildContent_MultipleQueries()
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

            var entityCollection = new EntityCollection(new List<Entity>() { testEntity });
            var entityCollection2 = new EntityCollection(new List<Entity>() { testEntity2 });
            _mockService.SetupSequence(s => s.RetrieveMultiple(It.IsAny<FetchExpression>()))
            .Returns(entityCollection)
            .Returns(entityCollection2);

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
        public void T04_ExecuteFetchAndPopulateValues_InvalidXml_ThrowsException()
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
        public void T05_ExecuteFetchAndPopulateValues_ReturnsContentForOneEntity()
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
        [TestMethod]
        public void T06_ExecutesRepeatingGroup_ForMultipleEntities()
        {
            // Arrange
            var e1 = new Entity("contact") { Id = Guid.NewGuid() };
            e1["firstname"] = "Alice";
            var e2 = new Entity("contact") { Id = Guid.NewGuid() };
            e2["firstname"] = "Bob";

            var query = "<fetch><entity name='contact'/><attribute name='firstname'/></fetch>";
            var entities = new EntityCollection(new List<Entity> { e1, e2 });

            _mockService.Setup(s => s.RetrieveMultiple(It.IsAny<FetchExpression>())).Returns(entities);

            var repeatingGroups = new List<RepeatingGroups>
        {
            new RepeatingGroups { format = "Name: {{firstname}}", name = "names" }
        };

            var templateModel = new TextDescriptionBodies
            {
                structure = "{{ContactQuery-names}}",
                queries = new List<Queries>
            {
                new Queries { name="ContactQuery", queryText=query, sequence=1, repeatingGroups=repeatingGroups }
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
            var result = _mockcontentGeneratorService.ExecuteFetchAndPopulateValues("ContactQuery", query, repeatingGroups) as List<RepeatingGroups>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result[0].contentValue.Contains("Alice"));
            Assert.IsTrue(result[0].contentValue.Contains("Bob"));
        }
        [TestMethod]
        public void T07_ReturnsNull_WhenNoRepeatingGroups()
        {
            // Arrange
            var entity = new Entity("contact") { Id = Guid.NewGuid() };
            entity["firstname"] = "John";
            var query = "<fetch><entity name='contact'/><attribute name='firstname'/></fetch>";
            var entities = new EntityCollection(new List<Entity> { entity });

            _mockService.Setup(s => s.RetrieveMultiple(It.IsAny<FetchExpression>())).Returns(entities);

            var templateModel = new TextDescriptionBodies
            {
                structure = "Hello {{ContactQuery-firstname}}",
                queries = new List<Queries>
            {
                new Queries { name="ContactQuery", queryText=query, sequence=1 }
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
            var result = _mockcontentGeneratorService.ExecuteFetchAndPopulateValues("ContactQuery", query, null);

            // Assert
            Assert.IsNull(result);
        }
        [TestMethod]
        public void T08_ExecuteFetchAndPopulateValues_MultipleEntitiesWithMultipleRepeatingGroups()
        {
            // Arrange
            var e1 = new Entity("contact") { Id = Guid.NewGuid() };
            e1["firstname"] = "Alice";
            e1["contactid"] = e1.Id;
            var e2 = new Entity("contact") { Id = Guid.NewGuid() };
            e2["firstname"] = "Bob";
            e2["contactid"] = e2.Id;
            var query = "<fetch><entity name='contact'/><attribute name='firstname'/></fetch>";
            var entities = new EntityCollection(new List<Entity> { e1, e2 });

            _mockService.Setup(s => s.RetrieveMultiple(It.IsAny<FetchExpression>()))
                        .Returns(entities);

            var repeatingGroups = new List<RepeatingGroups>
    {
        new RepeatingGroups { format = "Name: {{firstname}} ", name = "names" },
        new RepeatingGroups { format = "Id: {{contactid}} ", name = "ids" }
    };

            var templateModel = new TextDescriptionBodies
            {
                structure = "Contacts: {{ContactQuery-names}} | IDs: {{ContactQuery-ids}}",
                queries = new List<Queries>
        {
            new Queries
            {
                name = "ContactQuery",
                queryText = query,
                sequence = 1,
                repeatingGroups = repeatingGroups
            }
        }
            };

            _mockTemplateRepo.Setup(tr => tr.CreateTemplateModel(_descriptionID))
                             .Returns(templateModel);

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
            Assert.AreEqual(2, result.Count);

            // First group should contain concatenated names
            Assert.IsTrue(result.First(rg => rg.name == "names").contentValue.Contains("Alice"));
            Assert.IsTrue(result.First(rg => rg.name == "names").contentValue.Contains("Bob"));

            // Second group should contain both IDs
            Assert.IsTrue(result.First(rg => rg.name == "ids").contentValue.Contains(e1.Id.ToString()));
            Assert.IsTrue(result.First(rg => rg.name == "ids").contentValue.Contains(e2.Id.ToString()));
        }
        [TestMethod]
        public void T09_ExecuteFetchAndPopulateValues_SingleEntityWithNestedRepeatingGroup()
        {
            // Arrange
            var contact = new Entity("contact") { Id = Guid.NewGuid() };
            contact["firstname"] = "Alice";

            var contactQuery = "<fetch><entity name='contact'><attribute name='firstname'/></entity></fetch>";
            var contactEntities = new EntityCollection(new List<Entity> { contact });

            // Child entities (orders)
            var order1 = new Entity("salesorder") { Id = Guid.NewGuid() };
            order1["ordernumber"] = "1001";

            var order2 = new Entity("salesorder") { Id = Guid.NewGuid() };
            order2["ordernumber"] = "1002";

            var orderQuery = "<fetch><entity name='salesorder'><attribute name='ordernumber'/></entity></fetch>";
            var orderEntities = new EntityCollection(new List<Entity> { order1, order2 });


            _mockService.SetupSequence(s => s.RetrieveMultiple(It.IsAny<FetchExpression>()))
          .Returns(contactEntities)
          .Returns(orderEntities);
            // Nested repeating group for orders
            var nestedRepeatingGroups = new List<RepeatingGroups>
    {
        new RepeatingGroups { format = "OrderNumber: {{ordernumber}} ", name = "orders" }
    };

            // Parent repeating group that calls the nested query
            var repeatingGroups = new List<RepeatingGroups>
    {
        new RepeatingGroups
        {
            format = "Customer: {{firstname}} Orders: {{orders}}",
            name = "customerOrders",
            nestedRepeatingGroups = nestedRepeatingGroups,
            query = new Queries { name = "OrderQuery", queryText = orderQuery }
        }
    };

            var templateModel = new TextDescriptionBodies
            {
                structure = "{{ContactQuery-customerOrders}}",
                queries = new List<Queries>
        {
            new Queries
            {
                name = "ContactQuery",
                queryText = contactQuery,
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
            var result = _mockcontentGeneratorService.ExecuteFetchAndPopulateValues("ContactQuery", contactQuery, repeatingGroups) as List<RepeatingGroups>;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var content = result[0].contentValue;

            Assert.IsTrue(content.Contains("Customer: Alice"));
            Assert.IsTrue(content.Contains("OrderNumber: 1001"));
            Assert.IsTrue(content.Contains("OrderNumber: 1002"));
        }
        [TestMethod]
        public void T10_ExecuteFetchAndPopulateValues_MultipleEntitiesWithNestedRepeatingGroup()
        {
            // Arrange
            var contact1 = new Entity("contact") { Id = Guid.NewGuid() };
            contact1["firstname"] = "Alice";

            var contact2 = new Entity("contact") { Id = Guid.NewGuid() };
            contact2["firstname"] = "Bob";

            var contactQuery = "<fetch><entity name='contact'><attribute name='firstname'/></entity></fetch>";
            var contactEntities = new EntityCollection(new List<Entity> { contact1, contact2 });

            // Orders for Alice
            var order1 = new Entity("salesorder") { Id = Guid.NewGuid() };
            order1["ordernumber"] = "1001";
            var order2 = new Entity("salesorder") { Id = Guid.NewGuid() };
            order2["ordernumber"] = "1002";

            // Orders for Bob
            var order3 = new Entity("salesorder") { Id = Guid.NewGuid() };
            order3["ordernumber"] = "2001";

            // Two different queries for each contact’s orders
            var orderQuery = "<fetch><entity name='salesorder'><attribute name='ordernumber'/></entity></fetch>";
            var orderEntitiesAlice = new EntityCollection(new List<Entity> { order1, order2 });

            _mockService.SetupSequence(s => s.RetrieveMultiple(It.IsAny<FetchExpression>()))
.Returns(contactEntities)
.Returns(orderEntitiesAlice)
.Returns(new EntityCollection(new List<Entity> { order3 }));
            // Nested repeating group for orders
            var nestedRepeatingGroups = new List<RepeatingGroups>
    {
        new RepeatingGroups { format = "OrderNumber: {{ordernumber}} ", name = "orders" }
    };

            // Parent repeating group that references nested query
            var repeatingGroups = new List<RepeatingGroups>
    {
        new RepeatingGroups
        {
            format = "Customer: {{firstname}} Orders: {{orders}}\n",
            name = "customerOrders",
            nestedRepeatingGroups = nestedRepeatingGroups,
            query = new Queries { name = "OrderQuery", queryText = orderQuery }
        }
    };

            var templateModel = new TextDescriptionBodies
            {
                structure = "{{ContactQuery-customerOrders}}",
                queries = new List<Queries>
        {
            new Queries
            {
                name = "ContactQuery",
                queryText = contactQuery,
                sequence = 1,
                repeatingGroups = repeatingGroups
            }
        }
            };

            _mockTemplateRepo.Setup(tr => tr.CreateTemplateModel(_descriptionID))
                             .Returns(templateModel);

            _mockcontentGeneratorService = new ContentGeneratorService(
                _mockService.Object,
                _mockContext.Object,
                _mockTracing.Object,
                _mockTemplateRepo.Object,
                _descriptionID);

            // Act
            var result = _mockcontentGeneratorService.ExecuteFetchAndPopulateValues("ContactQuery", contactQuery, repeatingGroups) as List<RepeatingGroups>;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var content = result[0].contentValue;

            // Check Alice’s section
            Assert.IsTrue(content.Contains("Customer: Alice"));
            Assert.IsTrue(content.Contains("OrderNumber: 1001"));
            Assert.IsTrue(content.Contains("OrderNumber: 1002"));

            // Check Bob’s section
            Assert.IsTrue(content.Contains("Customer: Bob"));
            Assert.IsTrue(content.Contains("OrderNumber: 2001"));
        }
        [TestMethod]
        public void T11_ExecuteFetchAndPopulateValues_MultiLevelNestedRepeatingGroups()
        {
            // Arrange
            // ---- Parent: Contact ----
            var contact = new Entity("contact") { Id = Guid.NewGuid() };
            contact["firstname"] = "Alice";

            var contactQuery = "<fetch><entity name='contact'><attribute name='firstname'/></entity></fetch>";
            var contactEntities = new EntityCollection(new List<Entity> { contact });

            // ---- First-level: Orders ----
            var order1 = new Entity("salesorder") { Id = Guid.NewGuid() };
            order1["ordernumber"] = "1001";

            var order2 = new Entity("salesorder") { Id = Guid.NewGuid() };
            order2["ordernumber"] = "1002";

            var orderQuery = "<fetch><entity name='salesorder'><attribute name='ordernumber'/></entity></fetch>";
            var orderEntities = new EntityCollection(new List<Entity> { order1, order2 });


            // ---- Second-level: OrderLines ----
            var line1 = new Entity("salesorderdetail") { Id = Guid.NewGuid() };
            line1["productname"] = "LineA1";
            var line2 = new Entity("salesorderdetail") { Id = Guid.NewGuid() };
            line2["productname"] = "LineA2";
            var line3 = new Entity("salesorderdetail") { Id = Guid.NewGuid() };
            line3["productname"] = "LineB1";

            var orderLineQuery = "<fetch><entity name='salesorderdetail'><attribute name='productname'/></entity></fetch>";
            var orderLineEntitiesForOrder1 = new EntityCollection(new List<Entity> { line1, line2 });
            var orderLineEntitiesForOrder2 = new EntityCollection(new List<Entity> { line3 });

            _mockService.SetupSequence(s => s.RetrieveMultiple(It.IsAny<FetchExpression>()))
                        .Returns(contactEntities)
                        .Returns(orderEntities)
                        .Returns(orderLineEntitiesForOrder1)
                        .Returns(orderLineEntitiesForOrder2);
            // ---- Nested repeating groups ----
            var orderLineRepeatingGroups = new List<RepeatingGroups>
    {
        new RepeatingGroups { format = "Line: {{productname}} ", name = "orderlines" }
    };

            var orderRepeatingGroups = new List<RepeatingGroups>
    {
        new RepeatingGroups
        {
            format = "Order {{ordernumber}} -> {{orderlines}}\n",
            name = "orders",
            nestedRepeatingGroups = orderLineRepeatingGroups,
            query = new Queries { name = "OrderLineQuery", queryText = orderLineQuery }
        }
    };

            var contactRepeatingGroups = new List<RepeatingGroups>
    {
        new RepeatingGroups
        {
            format = "Customer: {{firstname}}\n{{orders}}",
            name = "customers",
            nestedRepeatingGroups = orderRepeatingGroups,
            query = new Queries { name = "OrderQuery", queryText = orderQuery }
        }
    };

            var templateModel = new TextDescriptionBodies
            {
                structure = "{{ContactQuery-customers}}",
                queries = new List<Queries>
        {
            new Queries
            {
                name = "ContactQuery",
                queryText = contactQuery,
                sequence = 1,
                repeatingGroups = contactRepeatingGroups
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
            var result = _mockcontentGeneratorService.ExecuteFetchAndPopulateValues("ContactQuery", contactQuery, contactRepeatingGroups) as List<RepeatingGroups>;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var content = result[0].contentValue;

            // Top-level check
            Assert.IsTrue(content.Contains("Customer: Alice"));

            // Orders
            Assert.IsTrue(content.Contains("Order 1001"));
            Assert.IsTrue(content.Contains("Order 1002"));

            // Lines under order 1001
            Assert.IsTrue(content.Contains("Line: LineA1"));
            Assert.IsTrue(content.Contains("Line: LineA2"));

            // Lines under order 1002
            Assert.IsTrue(content.Contains("Line: LineB1"));
        }

    }
}
