using System;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RutaHttpModule;

namespace RutaHttpModuleTest
{
    [TestCategory("ActiveDirectoryRequired")]
    [TestClass]
    public class AdInteractionTest
    {
        private static Regex emailRegex = new Regex(@"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?", RegexOptions.Compiled);

        private AdInteraction adInteraction;
        private Mock<ISettings> settings;

        [TestInitialize]
        public void Init()
        {
            this.settings = new Mock<ISettings>();
            this.settings.SetupGet(x => x.AdGroupBaseDn).Returns(string.Empty);
            this.settings.SetupGet(x => x.AdUserBaseDns).Returns(new string[0]);
            this.settings.SetupGet(x => x.DowncaseUsers).Returns(true);
            this.settings.SetupGet(x => x.DowncaseGroups).Returns(true);
            this.settings.SetupGet(x => x.AppendString).Returns(string.Empty);

            this.adInteraction = new AdInteraction(this.settings.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullSettingsTest()
        {
            new AdInteraction(null);
        }

        [TestMethod]
        public void BasicInfoTest()
        {
            var result = this.adInteraction.GetUserInformation(WindowsIdentity.GetCurrent().Name);

            Assert.IsTrue(WindowsIdentity.GetCurrent().Name.EndsWith(result.login, StringComparison.Ordinal));
            Assert.IsNotNull(result.name);
            Assert.IsTrue(emailRegex.IsMatch(result.email));
            Assert.IsTrue(result.groups?.Count()>= 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullDomainUserTest()
        {
            var result = this.adInteraction.GetUserInformation(null);
        }

        [TestMethod]
        public void NoSuchUserTest()
        {
            var result = this.adInteraction.GetUserInformation("Domain\\NoSuchUserShouldExist");
            Assert.IsNull(result.login);
        }

        [TestMethod]
        public void DefaultNoDomainTest()
        {
            var result = this.adInteraction.GetUserInformation(WindowsIdentity.GetCurrent().Name.Split('\\')[1]);
            Assert.IsTrue(WindowsIdentity.GetCurrent().Name.EndsWith(result.login, StringComparison.Ordinal));
        }

        [TestMethod]
        public void GroupDnFilterTest()
        {
            var result = this.adInteraction.GetUserInformation(WindowsIdentity.GetCurrent().Name);

            CollectionAssert.DoesNotContain(result.groups.ToArray(), "Domain Users");
        }

        [TestMethod]
        public void UserDnFilterTest()
        {
            this.settings.SetupGet(x => x.AdUserBaseDns).Returns(new[] {"DON'T MATCH"});
            var result = this.adInteraction.GetUserInformation(WindowsIdentity.GetCurrent().Name);

            Assert.IsNull(result.login);
        }        
    }
}
