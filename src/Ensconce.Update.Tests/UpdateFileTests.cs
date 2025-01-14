﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.XPath;

namespace Ensconce.Update.Tests
{
    [TestFixture]
    public class UpdateFileTests
    {
        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;

            var wd = Path.Combine(Environment.CurrentDirectory, "TestUpdateFiles");
            foreach (var file in Directory.EnumerateFiles(wd, "*.xml_partial"))
            {
                File.Delete(file);
            }
        }

        [Test]
        public void SubstituteOldValueWithNewStaticValue()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(@"TestUpdateFiles\TestSubstitution1.xml", @"TestUpdateFiles\TestConfig1.xml"));
            Assert.AreEqual("newvalue", newConfig.XPathSelectElement("/root/value").Value);
        }

        [Test]
        public void LimitSubstitutionsByFileName()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(@"TestUpdateFiles\TestSubstitution2.xml", @"TestUpdateFiles\TestConfig1.xml"));
            Assert.AreEqual("oldValue", newConfig.XPathSelectElement("/root/value").Value);
        }

        [Test]
        public void SubstituteInNameSpacedFile()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(@"TestUpdateFiles\TestSubstitution3.xml", @"TestUpdateFiles\TestConfig2.xml"));
            var nms = new XmlNamespaceManager(new NameTable());
            nms.AddNamespace("c", "http://madeup.com");
            Assert.AreEqual("newvalue", newConfig.XPathSelectElement("/c:root/c:value", nms).Value);
        }

        [Test]
        public void SubstituteOldValueWithNewTaggedValue()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution4.xml", @"TestUpdateFiles\TestConfig1.xml", new Dictionary<string, object> { { "taggedReplacementContent", "newvalue" } }.ToLazyTagDictionary()
                ));
            Assert.AreEqual("newvalue", newConfig.XPathSelectElement("/root/value").Value);
        }

        [Test]
        public void SubstituteOldValueWithNewTaggedXmlValue()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution5.xml", @"TestUpdateFiles\TestConfig1.xml", new Dictionary<string, object> { { "taggedReplacementContent", "newvalue" } }.ToLazyTagDictionary()
                ));
            Assert.AreEqual("newvalue", newConfig.XPathSelectElement("/root/value").Descendants().First().Name.LocalName);
        }

        [Test]
        public void SubstituteOldValueWithNewLoopedTaggedXmlValues()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution6.xml", @"TestUpdateFiles\TestConfig1.xml", new Dictionary<string, object>
                                                                {
                                                                    {"configList", new []{"newvalue1", "newvalue2","newvalue3"}}
                                                                }.ToLazyTagDictionary()
                ));
            Assert.AreEqual(XElement.Parse("<value><newvalue1/><newvalue2 /><newvalue3/></value>").ToString(), newConfig.XPathSelectElement("/root/value").ToString());
        }

        [Test]
        public void SubstituteOldValueWithNewLoopedTaggedXmlValuesAsXml()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution6.xml", @"TestUpdateFiles\TestConfig1.xml", new Dictionary<string, object>
                                                                {
                                                                    {"configList", new []{"newvalue1", "newvalue2","newvalue3"}}
                                                                }.ToLazyTagDictionary()
                ));
            Assert.AreEqual("", newConfig.XPathSelectElement("/root/value/newvalue1").Value);
        }

        [Test]
        public void ThrowNewMissingArgExceptionIfNoTagValue()
        {
            Assert.Throws<ArgumentException>(() => UpdateFile.Update(@"TestUpdateFiles\TestSubstitution5.xml", @"TestUpdateFiles\TestConfig1.xml"));
        }

        [Test]
        public void DoNotCreatePartialOutputFileIfExceptionDuringProcessWhenIndicated()
        {
            Assert.Throws<ArgumentException>(() => UpdateFile.Update(@"TestUpdateFiles\TestSubstitution5.xml", @"TestUpdateFiles\TestConfig1.xml"));
            var fileName = @"TestUpdateFiles\TestConfig1.xml_partial";

            Assert.False(File.Exists(fileName));
        }

        [Test]
        public void CreatePartialOutputFileIfExceptionDuringProcessWhenIndicated()
        {
            Assert.Throws<ArgumentException>(() => UpdateFile.Update(@"TestUpdateFiles\TestSubstitution5.xml", @"TestUpdateFiles\TestConfig1.xml", outputFailureContext: true));

            var fileName = @"TestUpdateFiles\TestConfig1.xml_partial";

            Assert.IsTrue(File.Exists(fileName));
        }

        [Test]
        public void ChangeValueOfAttributeWithFixedSub()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution7.xml", @"TestUpdateFiles\TestConfig3.xml"
                ));
            Assert.AreEqual("after", newConfig.XPathSelectElement("/root/value").Attribute("myAttr").Value);
        }

        [Test]
        public void DoNotRemoveAttributesUnlessSpecified()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution7.xml", @"TestUpdateFiles\TestConfig3.xml"
                ));
            Assert.AreEqual("quack", newConfig.XPathSelectElement("/root/value").Attribute("duckAttr").Value);
        }

        [Test]
        public void DoNotChangeChildrenUnlessNewValueSpecified()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution7.xml", @"TestUpdateFiles\TestConfig3.xml"
                ));
            Assert.AreEqual("oldValue", newConfig.XPathSelectElement("/root/value").Value);
        }

        [Test]
        public void ChangeValueOfAttributeWithFixedSubEvenWhenOldAttributesRemoved()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution8.xml", @"TestUpdateFiles\TestConfig3.xml"
                ));
            Assert.AreEqual("after", newConfig.XPathSelectElement("/root/value").Attribute("myAttr").Value);
        }

        [Test]
        public void RemoveAttributesIfSpecified()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution8.xml", @"TestUpdateFiles\TestConfig3.xml"
                ));
            Assert.IsNull(newConfig.XPathSelectElement("/root/value").Attribute("duckAttr"));
        }

        [Test]
        public void RemoveChildrenIfEmptyReplacementSpecified()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution8.xml", @"TestUpdateFiles\TestConfig3.xml"
                ));
            Assert.AreEqual("", newConfig.XPathSelectElement("/root/value").Value);
        }

        [Test]
        public void SubstituteTaggedAttributeValue()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution9.xml", @"TestUpdateFiles\TestConfig3.xml", new Dictionary<string, object> { { "newValue", "after" } }.ToLazyTagDictionary()
                ));
            Assert.AreEqual("after", newConfig.XPathSelectElement("/root/value").Attribute("myAttr").Value);
        }

        [Test]
        public void AppendAfterWorks()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution14.xml", @"TestUpdateFiles\TestConfig1.xml", new Dictionary<string, object> { { "newValue", "after" } }.ToLazyTagDictionary()
                ));
            Assert.IsTrue(newConfig.Root.Descendants().Select(el => el.Name).Contains("NewTag"));
        }

        [Test]
        public void AddChildContentWorks()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution15.xml", @"TestUpdateFiles\TestConfig1.xml", new Dictionary<string, object> { { "newValue", "after" } }.ToLazyTagDictionary()
                ));
            Assert.AreEqual(1, newConfig.XPathSelectElements("/root/value/NewTag").Count());
        }

        [Test]
        public void ReplacementContentWorksWithAmpersandInTag()
        {
            var content = XDocument.Load(@"TestUpdateFiles\TestConfig1.xml");

            UpdateFile.UpdateFiles(@"TestUpdateFiles\TestSubstitution24.xml", new Dictionary<string, object> { { "tagValue", "t&his*text" } }.ToLazyTagDictionary(), false);

            var document = XDocument.Load(@"TestUpdateFiles\TestConfig1.xml");
            var nms = new XmlNamespaceManager(new NameTable());
            nms.AddNamespace("c", "http://madeup.com");
            Assert.AreEqual("t&his*text", document.XPathSelectElement("/root/value").Value);

            content.Save(@"TestUpdateFiles\TestConfig1.xml");
        }

        [Test]
        public void AddChildContenWorksWithAmpersandInTag()
        {
            var content = XDocument.Load(@"TestUpdateFiles\TestConfig1.xml");

            UpdateFile.UpdateFiles(@"TestUpdateFiles\TestSubstitution25.xml", new Dictionary<string, object> { { "tagValue", "t&his*text" } }.ToLazyTagDictionary(), false);

            var document = XDocument.Load(@"TestUpdateFiles\TestConfig1.xml");
            var nms = new XmlNamespaceManager(new NameTable());
            nms.AddNamespace("c", "http://madeup.com");
            Assert.AreEqual("t&his*text", document.XPathSelectElement("/root/value/testing").Value);

            content.Save(@"TestUpdateFiles\TestConfig1.xml");
        }

        [Test]
        public void AppendAfterWorksWithAmpersandInTag()
        {
            var content = XDocument.Load(@"TestUpdateFiles\TestConfig1.xml");

            UpdateFile.UpdateFiles(@"TestUpdateFiles\TestSubstitution26.xml", new Dictionary<string, object> { { "tagValue", "t&his*text" } }.ToLazyTagDictionary(), false);

            var document = XDocument.Load(@"TestUpdateFiles\TestConfig1.xml");
            var nms = new XmlNamespaceManager(new NameTable());
            nms.AddNamespace("c", "http://madeup.com");
            Assert.AreEqual("t&his*text", document.XPathSelectElement("/root/testing").Value);

            content.Save(@"TestUpdateFiles\TestConfig1.xml");
        }

        [Test]
        public void EmptyChildContentWorks()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution16.xml", @"TestUpdateFiles\TestConfig1.xml", new Dictionary<string, object> { { "newValue", "after" } }.ToLazyTagDictionary()
                ));
            Assert.IsTrue(newConfig.XPathSelectElements("/root/value/NewTag").Count() == 0);
        }

        [Test]
        public void MultipleChildContentWorks()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution17.xml", @"TestUpdateFiles\TestConfig1.xml", new Dictionary<string, object> { { "newValue", "after" } }.ToLazyTagDictionary()
                ));
            Assert.AreEqual("1", newConfig.XPathSelectElements("/root/value/one").First().Value);
            Assert.AreEqual("2", newConfig.XPathSelectElements("/root/value/two").First().Value);
        }

        [Test]
        public void UpdateAllTouchesAllFiles()
        {
            var content = XDocument.Load(@"TestUpdateFiles\TestConfig1.xml");
            var content2 = XDocument.Load(@"TestUpdateFiles\TestConfig2.xml");

            UpdateFile.UpdateFiles(@"TestUpdateFiles\TestSubstitution3.xml", new Dictionary<string, object> { { "tagValue", "Tagged!" } }.ToLazyTagDictionary(), false);

            var document = XDocument.Load(@"TestUpdateFiles\TestConfig1.xml");
            var document2 = XDocument.Load(@"TestUpdateFiles\TestConfig2.xml");
            var nms = new XmlNamespaceManager(new NameTable());
            nms.AddNamespace("c", "http://madeup.com");
            Assert.AreEqual("newvalue", document2.XPathSelectElement("/c:root/c:value", nms).Value);
            Assert.AreEqual("Tagged!", document.XPathSelectElement("/root/value").Value);

            content.Save(@"TestUpdateFiles\TestConfig1.xml");
            content2.Save(@"TestUpdateFiles\TestConfig2.xml");
        }

        [Test]
        public void UpdateAllKnowsAboutTaggedFiles()
        {
            var content = XDocument.Load(@"TestUpdateFiles\TestConfig-TaggedPath.xml");

            UpdateFile.UpdateFiles(@"TestUpdateFiles\TestSubstitution13.xml", new Dictionary<string, object> { { "FilePath", "TaggedPath" } }.ToLazyTagDictionary(), false);

            var document = XDocument.Load(@"TestUpdateFiles\TestConfig-TaggedPath.xml");
            Assert.AreEqual("newvalue", document.XPathSelectElement("/root/value").Value);

            content.Save(@"TestUpdateFiles\TestConfig-TaggedPath.xml");
        }

        [Test]
        public void UpdateAllWhenError_ThrowsAggregateException()
        {
            var content = XDocument.Load(@"TestUpdateFiles\TestConfig1.xml");
            var content2 = XDocument.Load(@"TestUpdateFiles\TestConfig2.xml");

            Assert.Throws<AggregateException>(() => UpdateFile.UpdateFiles(@"TestUpdateFiles\TestSubstitution31.xml", new Lazy<TagDictionary>(), false));

            content.Save(@"TestUpdateFiles\TestConfig1.xml");
            content.Save(@"TestUpdateFiles\TestConfig2.xml");
        }

        [Test]
        public void ChangeAttributeWhenDoesntExists_ThrowsApplicationException()
        {
            var content = XDocument.Load(@"TestUpdateFiles\TestConfig1.xml");

            Assert.Throws<ApplicationException>(() => UpdateFile.UpdateFiles(@"TestUpdateFiles\TestSubstitution33.xml", new Lazy<TagDictionary>(), false));

            content.Save(@"TestUpdateFiles\TestConfig1.xml");
        }

        [Test]
        public void AddAttributeWhenAlreadyExists_ThrowsApplicationException()
        {
            var content = XDocument.Load(@"TestUpdateFiles\TestConfig1.xml");

            Assert.Throws<ApplicationException>(() => UpdateFile.UpdateFiles(@"TestUpdateFiles\TestSubstitution34.xml", new Lazy<TagDictionary>(), false));

            content.Save(@"TestUpdateFiles\TestConfig1.xml");
        }

        [Test]
        public void UpdateMultipleXPathMatches()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution35.xml",
                @"TestUpdateFiles\TestConfig5.xml"
            ));

            Assert.AreEqual("UpdateAll", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate1'][1]").Attribute("value").Value);
            Assert.AreEqual("UpdateAll", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate1'][2]").Attribute("value").Value);
            Assert.AreEqual("UpdateAll", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate1'][3]").Attribute("value").Value);
            Assert.AreEqual("UpdateFirst", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate2'][1]").Attribute("value").Value);
            Assert.AreEqual("NotSet", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate2'][2]").Attribute("value").Value);
            Assert.AreEqual("NotSet", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate3'][1]").Attribute("value").Value);
            Assert.AreEqual("UpdateLast", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate3'][2]").Attribute("value").Value);

            Assert.AreEqual("UpdateAll", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate4'][1]").Attribute("value").Value);
            Assert.AreEqual("UpdateAll", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate4'][2]").Attribute("value").Value);
            Assert.AreEqual("UpdateAll", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate4'][3]").Attribute("value").Value);
            Assert.AreEqual("UpdateFirst", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate5'][1]").Attribute("value").Value);
            Assert.AreEqual("NotSet", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate5'][2]").Attribute("value").Value);
            Assert.AreEqual("NotSet", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate6'][1]").Attribute("value").Value);
            Assert.AreEqual("UpdateLast", newConfig.XPathSelectElement("/configuration/appSettings/add[@name='TestDuplicate6'][2]").Attribute("value").Value);
        }

        [Test]
        public void UpdateAllWhenError_SingleError_ThrowsArgumentException()
        {
            var content = XDocument.Load(@"TestUpdateFiles\TestConfig1.xml");

            Assert.Throws<ArgumentException>(() => UpdateFile.UpdateFiles(@"TestUpdateFiles\TestSubstitution32.xml", new Lazy<TagDictionary>(), false));

            content.Save(@"TestUpdateFiles\TestConfig1.xml");
        }

        [Test]
        public void TagsOutsideSpecifiedXPathsUnchanged()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution9.xml", @"TestUpdateFiles\TestConfig3.xml", new Dictionary<string, object> { { "newValue", "after" } }.ToLazyTagDictionary()
                ));
            Assert.AreEqual("{{ tagged }}", newConfig.XPathSelectElement("/root/myValue").Value);
        }

        [Test]
        public void ReplaceFileWithTemplateWorks()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution10.xml", @"TestUpdateFiles\TestConfig2.xml", new Dictionary<string, object> { { "tagged", "after" } }.ToLazyTagDictionary()
                ));
            Assert.AreEqual("after", newConfig.XPathSelectElement("/root/myValue").Value);
        }

        [Test]
        public void NewFileWithTemplateWorks()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution11.xml", @"TestUpdateFiles\DoesntExist.xml", new Dictionary<string, object> { { "tagged", "after" } }.ToLazyTagDictionary()
                ));
            Assert.AreEqual("after", newConfig.XPathSelectElement("/root/myValue").Value);
        }

        [Test]
        public void TemplateAndChangesWork()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution18.xml", @"TestUpdateFiles\TestConfig2.xml", new Dictionary<string, object> { { "tagged", "after" } }.ToLazyTagDictionary()
                ));
            Assert.AreEqual("after", newConfig.XPathSelectElement("/root/myValue").Value);
            Assert.AreEqual("afterAttr", newConfig.XPathSelectElement("/root/myValue").Attribute("myAttr").Value);
        }

        [Test]
        public void TemplateAndChangesWorkWithTemplatedXPaths()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution19.xml", @"TestUpdateFiles\TestConfig2.xml",
                new Dictionary<string, object> { { "tagged", "after" }, { "Environment", "LOC" } }.ToLazyTagDictionary()
                                                ));
            Assert.AreEqual("after", newConfig.XPathSelectElement("/root/myValue-LOC").Value);
            Assert.AreEqual("afterAttr", newConfig.XPathSelectElement("/root/myValue-LOC").Attribute("myAttr").Value);
        }

        [Test]
        public void InvalidSubstitutionXmlShouldThrow()
        {
            Assert.Throws<XmlSchemaValidationException>(() => XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution20.xml", @"TestUpdateFiles\TestConfig2.xml",
                new Dictionary<string, object> { { "tagged", "after" }, { "Environment", "LOC" } }.ToLazyTagDictionary()
                                                )));
        }

        [Test]
        public void PlainTextTemplatingWorks()
        {
            var newConfig = UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution21.xml", @"TestUpdateFiles\PlainText01.txt",
                new Dictionary<string, object> { { "tag", "after" }, { "Environment", "LOC" } }.ToLazyTagDictionary());
            Assert.IsTrue(newConfig.Contains("after"));
        }

        [Test]
        public void PlainTextTemplatingWorksEvenWithXmlEscapableCharacters()
        {
            var newConfig = UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution21.xml", @"TestUpdateFiles\PlainText01.txt",
                new Dictionary<string, object> { { "tag", "<after>" }, { "Environment", "LOC" } }.ToLazyTagDictionary());
            Assert.IsTrue(newConfig.Contains("<after>"));
        }

        [Test]
        public void PlainTextWithEscaping()
        {
            var newConfig = UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution22.xml", @"TestUpdateFiles\PlainText02.txt",
                new Dictionary<string, object> { { "tag", "<after>" }, { "Environment", "LOC" } }.ToLazyTagDictionary());
            Assert.AreEqual("Some plain text. With a {{ tag }}.", newConfig);
        }

        [Test]
        public void NDjangoFiltersAvailable()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution12.xml", @"TestUpdateFiles\TestConfig1.xml", new Dictionary<string, object> { { "newvalue", "AfTer" } }.ToLazyTagDictionary()
                ));
            Assert.AreEqual("after", newConfig.XPathSelectElement("/root/value").Value);
        }

        [Test]
        public void ConcatFilterWorks()
        {
            var newConfig = UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution23.xml", @"TestUpdateFiles\PlainText03.txt",
                new Dictionary<string, object> { { "tag", "<after>" }, { "Environment", "LOC" } }.ToLazyTagDictionary());
            Assert.AreEqual("Some plain text. With a concat <after></after>.", newConfig);
        }

        [Test]
        public void SimplifiedChangeAttributeStructure()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution27.xml", @"TestUpdateFiles\TestConfig3.xml"
            ));
            Assert.AreEqual("after", newConfig.XPathSelectElement("/root/value").Attribute("myAttr").Value);
        }

        [Test]
        public void CondensedChangeAttributeStructure()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution28.xml", @"TestUpdateFiles\TestConfig3.xml"
            ));
            Assert.AreEqual("after", newConfig.XPathSelectElement("/root/value").Attribute("myAttr").Value);
        }

        [Test]
        public void CondensedAllSubsStructure()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution29.xml", @"TestUpdateFiles\TestConfig3.xml"
            ));
            Assert.NotNull(newConfig.XPathSelectElement("/root/testing"));
            Assert.Null(newConfig.XPathSelectElement("/root/testing/test[1]").Attribute("myAttr"));
            Assert.AreEqual("after", newConfig.XPathSelectElement("/root/testing/test[1]").Attribute("myAttr2").Value);
            Assert.AreEqual("value", newConfig.XPathSelectElement("/root/testing/test[1]").Value);
            Assert.AreEqual("new-after", newConfig.XPathSelectElement("/root/testing/test[2]").Attribute("myAttr").Value);
            Assert.AreEqual("new-value", newConfig.XPathSelectElement("/root/testing/test[2]").Value);
            Assert.NotNull(newConfig.XPathSelectElement("/root/myValue"));
            Assert.AreEqual("nodeValue", newConfig.XPathSelectElement("/root/myValue").Value);
        }

        [Test]
        public void SubstituteIf_True()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution30.xml", @"TestUpdateFiles\TestConfig3.xml",
                new Dictionary<string, object> { { "Environment", "INT" } }.ToLazyTagDictionary()
            ));

            Assert.AreEqual("after", newConfig.XPathSelectElement("/root/value").Attribute("myAttr").Value);
        }

        [Test]
        public void SubstituteIf_False()
        {
            var newConfig = XDocument.Parse(UpdateFile.Update(
                @"TestUpdateFiles\TestSubstitution30.xml", @"TestUpdateFiles\TestConfig3.xml",
                new Dictionary<string, object> { { "Environment", "NOT_INT" } }.ToLazyTagDictionary()
            ));

            Assert.AreEqual("before", newConfig.XPathSelectElement("/root/value").Attribute("myAttr").Value);
        }
    }
}
