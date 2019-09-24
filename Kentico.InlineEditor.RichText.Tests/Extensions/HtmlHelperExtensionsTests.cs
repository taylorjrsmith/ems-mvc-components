﻿using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;

using CMS.Base;
using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.LicenseProvider;
using CMS.Membership;
using CMS.SiteProvider;
using CMS.Tests;

using Kentico.Web.Mvc;

using NSubstitute;
using NUnit.Framework;


namespace Kentico.Components.Web.Mvc.InlineEditors.Tests
{
    public class HtmlHelperExtensionsTests
    {
        [TestFixture]
        public class RichTextEditorTests : UnitTests
        {
            private const string PROPERTY_NAME = "Test";
            private const string LICENSE_KEY = "license_key";

            private HtmlHelper htmlHelperMock;
            private TextWriter writerMock;

            private ISettingsService settingsService;
            private SiteInfo site;


            [SetUp]
            public void SetUp()
            {
                Fake<SettingsKeyInfo, SettingsKeyInfoProvider>()
                    .WithData(new SettingsKeyInfo { KeyName = "CMSRichTextEditorLicense", KeyValue = LICENSE_KEY });

                FakeEMSLicense();

                Fake<SiteInfo>();
                Fake<UserInfo>();
                MembershipContext.AuthenticatedUser = new CurrentUserInfo(new UserInfo(), false);

                site = new SiteInfo {
                    DomainName = "testdomain",
                    SiteName = "Site1",
                };

                var siteService = Substitute.For<ISiteService>();
                siteService.CurrentSite.Returns(site);

                settingsService = Substitute.For<ISettingsService>();
                settingsService[$"{site.SiteName}.CMSEnableOnlineMarketing"].Returns("true");

                Service.Use<ISiteService>(siteService);
                Service.Use<ISettingsService>(settingsService);

                htmlHelperMock = HtmlHelperMock.GetHtmlHelper();
                writerMock = htmlHelperMock.ViewContext.Writer;
            }


            [Test]
            public void RichTextEditor_InstanceIsNull_ThrowsArgumentNullException()
            {
                Assert.That(() => ((ExtensionPoint<HtmlHelper>)null).RichTextEditor(PROPERTY_NAME), Throws.ArgumentNullException);
            }


            [TestCase(null)]
            [TestCase("")]
            [TestCase(" ")]
            public void RichTextEditor_PropertyNameIsInvalid_ThrowsArgumentNullException(string invalidPropertyName)
            {
                Assert.That(() => htmlHelperMock.Kentico().RichTextEditor(invalidPropertyName), Throws.ArgumentException);
            }


            [Test]
            public void RichTextEditor_PropertyNameIsValidAndLicenseIsEMSAndOnlineMarketingEnabled_WritesToViewContext()
            {
                DynamicTextPatternRegister.Instance = new DynamicTextPatternRegister(new List<DynamicTextPattern>
                {
                   new DynamicTextPattern("Test", "TestDisplayName", null)
                });

                htmlHelperMock.Kentico().RichTextEditor(PROPERTY_NAME);

                Received.InOrder(() =>
                {
                    writerMock.Write($"<div data-inline-editor=\"Kentico.InlineEditor.RichText\" data-property-name=\"{PROPERTY_NAME.ToLower()}\">");
                    writerMock.Write($"<div class=\"ktc-rich-text-wrapper\" data-context-macros=\"{{&quot;Test&quot;:&quot;TestDisplayName&quot;}}\" data-rich-text-editor-license=\"{LICENSE_KEY}\" />");
                    writerMock.Write("</div>");
                });
            }


            [Test]
            public void RichTextEditor_PropertyNameIsValidAndLicenseIsEMSAndOnlineMarketingEnabledAndNoPatternsRegistered_WritesToViewContext()
            {
                DynamicTextPatternRegister.Instance = new DynamicTextPatternRegister(new List<DynamicTextPattern>());

                htmlHelperMock.Kentico().RichTextEditor(PROPERTY_NAME);

                Received.InOrder(() =>
                {
                    writerMock.Write($"<div data-inline-editor=\"Kentico.InlineEditor.RichText\" data-property-name=\"{PROPERTY_NAME.ToLower()}\">");
                    writerMock.Write($"<div class=\"ktc-rich-text-wrapper\" data-rich-text-editor-license=\"{LICENSE_KEY}\" />");
                    writerMock.Write("</div>");
                });
            }


            [Test]
            public void RichTextEditor_PropertyNameIsValidAndLicenseWithoutFullContactManagementFeature_WritesToViewContext()
            {
                Fake<LicenseKeyInfo, LicenseKeyInfoProvider>().WithData();

                htmlHelperMock.Kentico().RichTextEditor(PROPERTY_NAME);

                Received.InOrder(() =>
                {
                    writerMock.Write($"<div data-inline-editor=\"Kentico.InlineEditor.RichText\" data-property-name=\"{PROPERTY_NAME.ToLower()}\">");
                    writerMock.Write($"<div class=\"ktc-rich-text-wrapper\" data-rich-text-editor-license=\"{LICENSE_KEY}\" />");
                    writerMock.Write("</div>");
                });
            }


            [Test]
            public void RichTextEditor_PropertyNameIsValidAndOnlineMarketingDisabled_WritesToViewContext()
            {
                settingsService[$"{site.SiteName}.CMSEnableOnlineMarketing"].Returns("false");

                htmlHelperMock.Kentico().RichTextEditor(PROPERTY_NAME);

                Received.InOrder(() =>
                {
                    writerMock.Write($"<div data-inline-editor=\"Kentico.InlineEditor.RichText\" data-property-name=\"{PROPERTY_NAME.ToLower()}\">");
                    writerMock.Write($"<div class=\"ktc-rich-text-wrapper\" data-rich-text-editor-license=\"{LICENSE_KEY}\" />");
                    writerMock.Write("</div>");
                });
            }


            private void FakeEMSLicense()
            {
                var licenseFakeProvider = Fake<LicenseKeyInfo, LicenseKeyInfoProvider>();
                var license = new LicenseKeyInfo();
                license.SetValue("LicenseDomain", "testdomain");
                license.SetValue("LicenseExpiration", LicenseKeyInfo.TIME_UNLIMITED_LICENSE);
                license.SetValue("LicenseServers", 0);
                license.SetValue("LicenseEdition", 'X');
                license.SetValue("LicenseKey",
@"DOMAIN:testdomain
PRODUCT:CX12
EXPIRATION:00000000
SERVERS:0
abcdefghijklmnopqrstuvwxyz==");
                licenseFakeProvider.WithData(license);
            }
        }


        [TestFixture]
        public class ResolveRichTextTests : UnitTests
        {
            private HtmlHelper htmlHelperMock;


            [SetUp]
            public void SetUp()
            {
                Fake<UserInfo>();
                Fake<ContactInfo>();
                var currentContact = new ContactInfo
                {
                    ContactFirstName = "FIRSTNAME",
                };

                // First mock the user before accessing the instance
                MembershipContext.AuthenticatedUser = new CurrentUserInfo(new UserInfo(), false);
                DynamicTextPatternRegister.Instance.GetCurrentContact = () => currentContact;

                htmlHelperMock = HtmlHelperMock.GetHtmlHelper();
            }


            [Test]
            public void ResolveRichText_InstanceIsNull_ThrowsArgumentNullException()
            {
                Assert.That(() => ((ExtensionPoint<HtmlHelper>)null).ResolveRichText("text"), Throws.ArgumentNullException);
            }


            [Test]
            public void ResolveRichText_CorrectParameters_ReturnsResolvedText()
            {
                Assert.Multiple(() =>
                {
                    Assert.That(htmlHelperMock.Kentico().ResolveRichText(
                                        @"{% ResolveDynamicText(""pattern"", ""ContactFirstName"", ""RESOLVED"") %}"),
                                        Is.EqualTo("FIRSTNAME"));
                    Assert.That(htmlHelperMock.Kentico().ResolveRichText(
                                        @"{% ResolveDynamicText(""pattern"", ""ContactLastName"", ""DEFAULT"") %}"),
                                        Is.EqualTo("DEFAULT"));
                });
            }
        }
    }
}