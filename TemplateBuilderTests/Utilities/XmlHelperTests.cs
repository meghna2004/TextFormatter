using Microsoft.VisualStudio.TestTools.UnitTesting;
using TemplateBuilder.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using System.Xml;

namespace TemplateBuilder.Utilities.Tests
{
    [TestClass()]
    public class XmlHelperTests
    {
        [TestMethod()]
        public void T01_ExtractFetchQuery_ValidSingleFetch()
        {
            //Arrange
            var xml = "<fetch><entity name='account'></entity></fetch>";
            var expected = "<fetch><entity name=\"account\"></entity></fetch>";
            var helper = new XmlHelper();
            //Act
            var result = helper.ExtractFetchQuery(xml);
            //Assert
            Assert.AreEqual(expected,result);
        }
        [TestMethod()]
        public void T02_ExtractFetchQuery_NestedFetchQuery()
        {
            //Arrange
            var inputQuery = @"<customlayout>
                                  <templates>
                                    <template name='MainTemplate'>
                                      <controls>
                                        <control id='fetchControl'>
                                          <parameters>
                                            <fetchquery>
                                              <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                <entity name='lead'>
                                                  <attribute name='fullname' />
                                                  <attribute name='emailaddress1' />
                                                  <order attribute='createdon' descending='true' />
                                                </entity>
                                              </fetch>
                                            </fetchquery>
                                          </parameters>
                                        </control>
                                      </controls>
                                    </template>
                                  </templates>
                                </customlayout>";
            var expectedResult = @"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                                        <entity name=""lead"">
                                            <attribute name=""fullname"" />
                                            <attribute name=""emailaddress1"" />
                                            <order attribute=""createdon"" descending=""true"" />
                                        </entity>
                                   </fetch>";
            var helper = new XmlHelper();
            XmlDocument expectedDoc = new XmlDocument();
            expectedDoc.LoadXml(expectedResult);
            //Act
            var result = helper.ExtractFetchQuery(inputQuery);
            XmlDocument actualDoc = new XmlDocument();
            actualDoc.LoadXml(result);
            //Assert
            Assert.AreEqual(expectedDoc.OuterXml, actualDoc.OuterXml);
        }
        [TestMethod()]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void T03_ExtractFetchQuery_MultipleFetchNodes()
        {
            //Arrange
            var helper = new XmlHelper();
            var query = @"<fetch>
                            <entity name='account'>
                                <attribute name='name' />
                                <attribute name='accountid' />
                            </entity>
                        </fetch>
                        <fetch>
                            <entity name='contact'>
                                <attribute name='fullname' />
                                <attribute name='emailaddress1' />
                            </entity>
                        </fetch>";
            //Act
            helper.ExtractFetchQuery(query);

            //Assert
        }
      
        [TestMethod()]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void T04_ExtractFetchQuery_InvalidResult()
        {
            //Arrange
            var helper = new XmlHelper();
            var invalidXml = "<fetch><entity name='account'></fetch>";

            //Act
            helper.ExtractFetchQuery(invalidXml);
            //Assert
        }
        [TestMethod()]
        public void T05_ExtractFetchQuery_NoFetchNode()
        {
            //Arrange
            var helper = new XmlHelper();
            var invalidXml = @"<customlayout>
                                  <templates>
                                    <template name='MainTemplate'>
                                      <controls>
                                        <control id='fetchControl'>
                                          <parameters>
                                            <fetchquery>                                             
                                            </fetchquery>
                                          </parameters>
                                        </control>
                                      </controls>
                                    </template>
                                  </templates>
                                </customlayout>";

            //Act
            var result = helper.ExtractFetchQuery(invalidXml);
            //Assert
            Assert.AreEqual(invalidXml, result);
        }
        [TestMethod()]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void T06_ExtractFetchQuery_NullInput()
        {
            //Arrange
            var helper = new XmlHelper();
            string nullInput = null;

            //Act
            helper.ExtractFetchQuery(nullInput);
        }
        [TestMethod()]
        [ExpectedException(typeof(InvalidPluginExecutionException))]
        public void T07_ExtractFetchQuery_EmptyString()
        {
            //Arrange
            var helper = new XmlHelper();
            string emptyString = string.Empty;

            //Act
            helper.ExtractFetchQuery(emptyString);
        }
    }
}