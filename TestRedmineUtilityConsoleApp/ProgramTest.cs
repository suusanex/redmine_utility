using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RedmineUtilityConsoleApp;
using NUnit.Framework;
using PrivateType = Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType;

namespace TestRedmineUtilityConsoleApp
{
 
    [TestFixture]
    public class ProgramTest
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
        }

        [Test]
        public void GenerateIssueSyncTest()
        {

            var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);
            {
                AddOrSetAppSetting(config, "SrcLinkKeywordPrefix", "src");
                AddOrSetAppSetting(config, "DestLinkKeywordPrefix", "");

                config.Save();

                System.Configuration.ConfigurationManager.RefreshSection("appSettings");

                var type = new PrivateType(typeof(Program));
                RedmineUtility.RedmineParams SrcParams = null;
                RedmineUtility.RedmineParams DestParams = null;
                var Params = new object[] { SrcParams, DestParams };
                type.InvokeStatic("GenerateSrcDestRedmineParams", Params);
                SrcParams = (RedmineUtility.RedmineParams)Params[0];
                DestParams = (RedmineUtility.RedmineParams)Params[1];
                Assert.That(SrcParams.LinkKeywordPrefix, Is.EqualTo("src"));
                Assert.That(DestParams.LinkKeywordPrefix, Is.EqualTo("redmine2"));
            }

            {
                AddOrSetAppSetting(config, "SrcLinkKeywordPrefix", "");
                AddOrSetAppSetting(config, "DestLinkKeywordPrefix", "dest");

                config.Save();

                System.Configuration.ConfigurationManager.RefreshSection("appSettings");

                var type = new PrivateType(typeof(Program));
                RedmineUtility.RedmineParams SrcParams = null;
                RedmineUtility.RedmineParams DestParams = null;
                var Params = new object[] { SrcParams, DestParams };
                type.InvokeStatic("GenerateSrcDestRedmineParams", Params);
                SrcParams = (RedmineUtility.RedmineParams)Params[0];
                DestParams = (RedmineUtility.RedmineParams)Params[1];

                Assert.That(SrcParams.LinkKeywordPrefix, Is.EqualTo("redmine1"));
                Assert.That(DestParams.LinkKeywordPrefix, Is.EqualTo("dest"));
            }

        }

        private static void AddOrSetAppSetting(System.Configuration.Configuration config, string Key, string Value)
        {
            var setting = config.AppSettings.Settings[Key];
            if (setting != null)
            {
                setting.Value = Value;
            }
            else
            {
                config.AppSettings.Settings.Add(Key, Value);
            }
        }
    }
}
