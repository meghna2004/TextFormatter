using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Moq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using TemplateBuilderTests.TestableClass;

namespace TemplateBuilder.Utilities.Tests
{
    [TestClass()]
    public class TokenProcessorTests
    {
        [TestMethod()]
        public void T01_ReplaceTokensForTBDTest()
        {
            //Arrange
            var mockContext = new Mock<IPluginExecutionContext>().Object;
            var mockTracing = new Mock<ITracingService>().Object;
            var mockService = new Mock<IOrganizationService>().Object;
            var template = "Hello {{sampleQuery-name}}.Here is your order number: {{sampleQuery2-co.ordernumber}}. Here are the order details: {{sampleQuery2-sampleRepeatingGroup}}";
            var data = new Entity();
            var data2 = new Entity();
            data["name"] = "Meghna";
            data2["co.ordernumber"] = "MEG-09365";
            var repeatingGroup = "Order placed on: 25/06/2025 Product Name: ProductName Price: £45 Amount: 20kg";
            var entityDictionary = new Dictionary<string, Entity>();
            entityDictionary.Add("sampleQuery", data);
            entityDictionary.Add("sampleQuery2", data2);
            var sectionDictionary = new Dictionary<string, string>();
            sectionDictionary.Add("sampleRepeatingGroup", repeatingGroup);
            data["course.name"] = "Course Name";
            var processor = new TokenProcessor(mockTracing, mockService, mockContext, entityDictionary, sectionDictionary);

            //Act
            var result = processor.ReplaceTokens(template);

            //Assert
            Assert.AreEqual("Hello Meghna.Here is your order number: MEG-09365. Here are the order details: Order placed on: 25/06/2025 Product Name: ProductName Price: £45 Amount: 20kg", result);
        }

        [TestMethod()]
        public void T02_ReplaceTokensForQueryTest_ReferencedEntityAttribute()
        {
            //Arrange
            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var accountEntity = new Entity("account") { Id = accountId };
            accountEntity["primarycontactid"] = new EntityReference("contact", contactId);

            var contactEntity = new Entity("contact") { Id = contactId };
            contactEntity["firstname"] = "Alice";



            // Mock 
            var serviceMock = new Mock<IOrganizationService>();
            serviceMock.Setup(s => s.Retrieve("account", accountId, It.IsAny<ColumnSet>()))
           .Returns(accountEntity);

            var contextMock = new Mock<IPluginExecutionContext>();
            contextMock.Setup(c => c.PrimaryEntityId).Returns(accountId);
            contextMock.Setup(c => c.PrimaryEntityName).Returns("account");

            var tracingMock = new Mock<ITracingService>();

            var lookupAttribute = new LookupAttributeMetadata
            {
                LogicalName = "primarycontactid",
                Targets = new[] { "contact" }
            };

            var entityMetadata = new EntityMetadata();

            var retrieveEntityResponse = new RetrieveEntityResponse();
            retrieveEntityResponse.Results["EntityMetadata"] = entityMetadata;

            typeof(EntityMetadata)
                .GetProperty("Attributes")
                .SetValue(entityMetadata, new AttributeMetadata[] { lookupAttribute });


            serviceMock.Setup(s => s.Execute(It.IsAny<RetrieveEntityRequest>()))
                       .Returns(retrieveEntityResponse);
            var replacer = new TestableClass1(tracingMock.Object, serviceMock.Object, contextMock.Object, contactEntity);
            string template = "Primary contact name: {{contact.firstname}}";

            // Act
            var result = replacer.ReplaceTokens(template);

            // Assert
            Assert.AreEqual("Primary contact name: Alice", result);
        }
        [TestMethod()]
        public void T02_ReplaceTokensForQueryTest_PrimaryEntityAttribute()
        {
            var accountId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var accountEntity = new Entity("account") { Id = accountId };
            accountEntity["primarycontactid"] = new EntityReference("contact", contactId);

            // Mock 
            var serviceMock = new Mock<IOrganizationService>();
            serviceMock.Setup(s => s.Retrieve("account", accountId, It.IsAny<ColumnSet>()))
           .Returns(accountEntity);

            var contextMock = new Mock<IPluginExecutionContext>();
            contextMock.Setup(c => c.PrimaryEntityId).Returns(accountId);
            contextMock.Setup(c => c.PrimaryEntityName).Returns("account");

            var tracingMock = new Mock<ITracingService>();
        }
    }
}