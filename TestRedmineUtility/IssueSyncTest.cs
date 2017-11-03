using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using NUnit.Framework;

namespace TestRedmineUtility
{
    [TestFixture]
    public class IssueSyncTest
    {

        /** 呼び出しのテストに使っているだけで、結果のチェックは未実装 */
        [Test]
        public void SyncTest_チケット上書き()
        {
            var inst = new RedmineUtility.IssueSync();

            var SrcParams = new RedmineUtility.RedmineParams();
            var DestParams = new RedmineUtility.RedmineParams();

            SrcParams.RedmineRootUrl = "http://<redmine url>";
            DestParams.RedmineRootUrl = "http://<redmine url>";
            SrcParams.RedmineAccessKey = "<Access Key>";
            DestParams.RedmineAccessKey = "<Access Key>";
            DestParams.FixedParam_Project = "<ProjectName>";
            DestParams.FixedParam_Tracker = "<TrackerName>";
            SrcParams.FixedParam_Project = "<ProjectName>";
            SrcParams.FixedParam_Tracker = "<TrackerName>";
            SrcParams.UsersFilePath = Path.Combine(AppDir, "UsersSrc.xml");
            DestParams.UsersFilePath = Path.Combine(AppDir, "UsersDest.xml");

            inst.TicketSync(1289, 1290, SrcParams, DestParams);

        }

        /** 呼び出しのテストに使っているだけで、結果のチェックは未実装 */
        [Test]
        public void SyncTest_新規チケット作成()
        {
            var inst = new RedmineUtility.IssueSync();

            var SrcParams = new RedmineUtility.RedmineParams();
            var DestParams = new RedmineUtility.RedmineParams();

            SrcParams.RedmineRootUrl = "http://<redmine url>";
            DestParams.RedmineRootUrl = "http://<redmine url>";
            SrcParams.RedmineAccessKey = "<Access Key>";
            DestParams.RedmineAccessKey = "<Access Key>";
            SrcParams.FixedParam_Project = "<ProjectName>";
            SrcParams.FixedParam_Tracker = "<TrackerName>";
            DestParams.FixedParam_Project = "<ProjectName>";
            DestParams.FixedParam_Tracker = "<TrackerName>";
            SrcParams.LinkKeywordPrefix = "testSrc";
            DestParams.LinkKeywordPrefix = "testDest";
            SrcParams.UsersFilePath = Path.Combine(AppDir, "UsersSrc.xml");
            DestParams.UsersFilePath = Path.Combine(AppDir, "UsersDest.xml");

            inst.TicketSync(1289, null, SrcParams, DestParams);

        }

        static string AppDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        /** 呼び出しのテストに使っているだけで、結果のチェックは未実装 */
        [Test]
        public void JournalAddTest()
        {
            var inst = new RedmineUtility.IssueSync();
            var SrcParams = new RedmineUtility.RedmineParams();
            var DestParams = new RedmineUtility.RedmineParams();

            SrcParams.RedmineRootUrl = "http://<redmine url>";
            DestParams.RedmineRootUrl = "http://<redmine url>";
            SrcParams.RedmineAccessKey = "＜AccessKey＞";
            DestParams.RedmineAccessKey = "＜AccessKey＞";

            inst.JournalAdd(SrcParams.RedmineRootUrl, SrcParams.RedmineAccessKey, 489, "addComment", DateTimeOffset.Now);
        }

        [Test]
        public void SyncTest_複数チケット同期()
        {
            var inst = new RedmineUtility.IssueSync();
            var SrcParams = new RedmineUtility.RedmineParams();
            var DestParams = new RedmineUtility.RedmineParams();

            SrcParams.RedmineRootUrl = "<TrackerName>";
            DestParams.RedmineRootUrl = "http://<redmine url>";
            SrcParams.RedmineAccessKey = "＜AccessKey＞";
            DestParams.RedmineAccessKey = "＜AccessKey＞";
            SrcParams.FixedParam_Project = "<ProjectName>";
            SrcParams.FixedParam_Tracker = "<TrackerName>";
            DestParams.FixedParam_Project = "<ProjectName>";
            DestParams.FixedParam_Tracker = "<TrackerName>";

            inst.TicketsAddOrSync(new int[] { 9784, 9940 }, SrcParams, DestParams);

        }

        /** 呼び出しのテストに使っているだけで、結果のチェックは未実装 */
        [Test]
        public void KeywordAddTest()
        {
            var inst = new RedmineUtility.IssueSync();
            var SrcParams = new RedmineUtility.RedmineParams();
            var DestParams = new RedmineUtility.RedmineParams();

            SrcParams.RedmineRootUrl = "http://<redmine url>";
            SrcParams.RedmineAccessKey = "＜AccessKey＞"; ;

            inst.KeywordAdd(1063, "追加キー", SrcParams, DestParams);
        }

        /** 呼び出しのテストに使っているだけで、結果のチェックは未実装 */
        [Test]
        public void KeywordReplaceTest()
        {
            var inst = new RedmineUtility.IssueSync();
            var SrcParams = new RedmineUtility.RedmineParams();
            var DestParams = new RedmineUtility.RedmineParams();

            SrcParams.RedmineRootUrl = "http://<redmine url>";
            SrcParams.RedmineAccessKey = "＜AccessKey＞"; ;

            inst.KeywordReplace(1063, "追加キー", "key", SrcParams, DestParams);

            inst.KeywordReplace(1063, "addComment", null, SrcParams, DestParams);
        }

        /** 呼び出しのテストに使っているだけで、結果のチェックは未実装 */
        [Test]
        public async Task TicketsAddOrSyncBothTest()
        {
            var inst = new RedmineUtility.IssueSync();
            var SrcParams = new RedmineUtility.RedmineParams();
            var DestParams = new RedmineUtility.RedmineParams();

            SrcParams.RedmineRootUrl = "http://<redmine url>";
            SrcParams.RedmineAccessKey = "＜AccessKey＞";

            await inst.TicketsAddOrSyncBoth(
                "http://<redmine url>/projects/<ProjectName>/issues?<Query>",
                null,
                SrcParams,
                DestParams);
        }
    }
}
