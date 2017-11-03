using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace RedmineUtilityConsoleApp
{
    public class Program
    {
        enum Command
        {
            Unknown,
            CopyCreate,
            Sync,
            TicketsAddOrSync,
            KeywordAdd,
            KeywordReplace,
            KeywordDelete,
            TicketsAddOrSyncBoth,
        }

        static void Trace(string msg)
        {
            Console.WriteLine(msg);
        }

        static int Main(string[] args)
        {
            int AppErr = 1;

            try
            {

                string Help = string.Join(Environment.NewLine,
                    $"{System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location)} [-CopyCreate n,n,n,n,n...]",
                    "",
                    "-CopyCreate n,n,n,n,n...：指定した番号のSrc側Redmineチケットの内容をコピーし、Dest側Redmineにチケットを新規作成する。nがチケット番号で、カンマ区切りで複数指定可能。",
                    "-Sync n,m：Src側RedmineチケットのnとDest側Redmineチケットのmを双方向同期する。n,mはチケット番号。",
                    "-TicketsAddOrSync n,n,n,n,n...：指定した番号のSrc側Redmineのチケットと対応するDest側Redmineのチケットを探し、見つかったら双方向同期する。見つからなかったらコピーして追加する。nがチケット番号で、カンマ区切りで複数指定可能。",
                    "-KeywordAdd <keyword> n,n,n,n,n...：ID==nのSrc側Redmineのチケットのキーワードに、<keyword>の単語を追加する。nがチケット番号で、カンマ区切りで複数指定可能。",
                    "-KeywordReplace <keywordOld> <keywordNew> n,n,n,n,n...：ID==nのSrc側Redmineのチケットのキーワードの、<keywordOld>の単語を<keywordNew>の単語に置換する。nがチケット番号で、カンマ区切りで複数指定可能。",
                    "-KeywordDelete <keyword> n,n,n,n,n...：ID==nのSrc側Redmineのチケットのキーワードの、<keyword>の単語を削除する。nがチケット番号で、カンマ区切りで複数指定可能。",
                    "-TicketsAddOrSyncBoth <SrcQueryURL> <DestQueryURL> <SrcProjectName> <DestProjectName>:指定したクエリURLで抽出したチケットを、相手方に同期する（無ければ追加する）。Src<>Dest双方向に実施する。Src(Dest)QueryURL=クエリ用のURL。redmineのチケット画面でクエリを実行したときのURLをそのまま貼る。 Src(Dest)ProjectName:チケット新規作成時のプロジェクト名。このコマンドでは、app.configの値ではなくこちらを使用する。"
                    );


                IEnumerable<int> SrcTicketNumbers = new int[0];
                int SyncSrcTicketNumber = 0;
                int SyncDestTicketNumber = 0;
                string Keyword1 = null;
                string Keyword2 = null;

                string SrcQueryURL = null;
                string DestQueryURL = null;
                string SrcProjectName = null;
                string DestProjectName = null;

                var ArgsCommandStr = args[0].TrimStart('-', '/');
                var ArgsCommand = (Command)Enum.Parse(typeof(Command), ArgsCommandStr);
                var ArgsParams = args.Skip(1).ToArray();
                switch (ArgsCommand)
                {
                    case Command.Unknown:
                        break;
                    case Command.CopyCreate:
                        SrcTicketNumbers = ArgsParams[0].Split(',').Select(item => int.Parse(item));
                        break;
                    case Command.Sync:
                        var nums = ArgsParams[0].Split(',').Select(item => int.Parse(item)).ToList();
                        SyncSrcTicketNumber = nums[0];
                        SyncDestTicketNumber = nums[1];
                        break;
                    case Command.TicketsAddOrSync:
                        SrcTicketNumbers = ArgsParams[0].Split(',').Select(item => int.Parse(item));
                        break;
                    case Command.KeywordAdd:
                        Keyword1 = ArgsParams[0];
                        SrcTicketNumbers = ArgsParams[1].Split(',').Select(item => int.Parse(item));
                        break;
                    case Command.KeywordReplace:
                        Keyword1 = ArgsParams[0];
                        Keyword2 = ArgsParams[1];
                        SrcTicketNumbers = ArgsParams[2].Split(',').Select(item => int.Parse(item));
                        break;
                    case Command.KeywordDelete:
                        Keyword1 = ArgsParams[0];
                        SrcTicketNumbers = ArgsParams[1].Split(',').Select(item => int.Parse(item));
                        break;
                    case Command.TicketsAddOrSyncBoth:
                        SrcQueryURL = ArgsParams[0];
                        DestQueryURL = ArgsParams[1];
                        SrcProjectName = ArgsParams[2];
                        DestProjectName = ArgsParams[3];
                        break;
                    default:
                        throw new NotImplementedException(ArgsCommandStr);
                }



                switch (ArgsCommand)
                {
                    case Command.Unknown:
                    default:
                        Console.Error.WriteLine(Help);
                        AppErr = 3;
                        break;
                    case Command.CopyCreate:

                        CopyCreate(SrcTicketNumbers);

                        Trace($"CopyCreate Success");
                        AppErr = 0;
                        break;
                    case Command.Sync:

                        Sync(SyncSrcTicketNumber, SyncDestTicketNumber);

                        Trace($"Sync Success");
                        AppErr = 0;
                        break;
                    case Command.TicketsAddOrSync:

                        TicketsAddOrSync(SrcTicketNumbers);

                        Trace($"TicketsAddOrSync Success");
                        AppErr = 0;
                        break;
                    case Command.KeywordAdd:
                        KeywordAdd(SrcTicketNumbers, Keyword1);
                        Trace($"KeywordAdd Success");
                        AppErr = 0;
                        break;
                    case Command.KeywordReplace:
                        KeywordReplace(SrcTicketNumbers, Keyword1, Keyword2);
                        Trace($"KeywordReplace Success");
                        AppErr = 0;
                        break;
                    case Command.KeywordDelete:
                        KeywordDelete(SrcTicketNumbers, Keyword1);
                        Trace($"KeywordDelete Success");
                        AppErr = 0;
                        break;
                    case Command.TicketsAddOrSyncBoth:
                        TicketsAddOrSyncBoth(SrcQueryURL, DestQueryURL, SrcProjectName, DestProjectName);
                        Trace($"KeywordDelete Success");
                        AppErr = 0;
                        break;
                }

            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                AppErr = 2;
            }

            return AppErr;
        }

        private static void CopyCreate(IEnumerable<int> CopyCreateTicketNumbers)
        {
            var inst = GenerateIssueSync();

            GenerateSrcDestRedmineParams(out RedmineUtility.RedmineParams SrcParams, out RedmineUtility.RedmineParams DestParams);

            foreach (var TicketNo in CopyCreateTicketNumbers)
            {
                Trace($"TicketSync Start({TicketNo}, {null})");
                inst.TicketSync(TicketNo, null, SrcParams, DestParams);
            }
        }

        private static RedmineUtility.IssueSync GenerateIssueSync()
        {
            var inst = new RedmineUtility.IssueSync();

            inst.AddTraceLog += Trace;
            return inst;
        }

        static string AppDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private static void GenerateSrcDestRedmineParams(out RedmineUtility.RedmineParams SrcParams, out RedmineUtility.RedmineParams DestParams)
        {
            SrcParams = new RedmineUtility.RedmineParams();

            SrcParams.RedmineRootUrl = ConfigurationManager.AppSettings["Src" + nameof(SrcParams.RedmineRootUrl)];
            SrcParams.RedmineAccessKey = ConfigurationManager.AppSettings["Src" + nameof(SrcParams.RedmineAccessKey)];
            SrcParams.FixedParam_Project = ConfigurationManager.AppSettings["Src" + nameof(SrcParams.FixedParam_Project)];
            SrcParams.FixedParam_Tracker = ConfigurationManager.AppSettings["Src" + nameof(SrcParams.FixedParam_Tracker)];
            SrcParams.UsersFilePath = Path.Combine(AppDir, "UsersSrc.xml");
            {
                var val = ConfigurationManager.AppSettings["Src" + nameof(SrcParams.LinkKeywordPrefix)];
                if (!string.IsNullOrEmpty(val))
                {
                    SrcParams.LinkKeywordPrefix = val;
                }
                else
                {
                    //初版のデフォルト値がこの値で、それを前提に処理しているスクリプトがある。そのため、デフォルト値は維持。
                    SrcParams.LinkKeywordPrefix = "redmine1";
                }
            }

            DestParams = new RedmineUtility.RedmineParams();

            DestParams.RedmineRootUrl = ConfigurationManager.AppSettings["Dest" + nameof(DestParams.RedmineRootUrl)];
            DestParams.RedmineAccessKey = ConfigurationManager.AppSettings["Dest" + nameof(DestParams.RedmineAccessKey)];
            DestParams.FixedParam_Project = ConfigurationManager.AppSettings["Dest" + nameof(DestParams.FixedParam_Project)];
            DestParams.FixedParam_Tracker = ConfigurationManager.AppSettings["Dest" + nameof(DestParams.FixedParam_Tracker)];
            DestParams.UsersFilePath = Path.Combine(AppDir, "UsersDest.xml");
            {
                var val = ConfigurationManager.AppSettings["Dest" + nameof(DestParams.LinkKeywordPrefix)];
                if (!string.IsNullOrEmpty(val))
                {
                    DestParams.LinkKeywordPrefix = val;
                }
                else
                {
                    //初版のデフォルト値がこの値で、それを前提に処理しているスクリプトがある。そのため、デフォルト値は維持。
                    DestParams.LinkKeywordPrefix = "redmine2";
                }
            }
        }

        private static void Sync(int SrcNum, int DestNum)
        {
            var inst = GenerateIssueSync();
            GenerateSrcDestRedmineParams(out RedmineUtility.RedmineParams SrcParams, out RedmineUtility.RedmineParams DestParams);

            inst.TicketSync(SrcNum, DestNum, SrcParams, DestParams);

        }
        private static void TicketsAddOrSync(IEnumerable<int> TicketNumbers)
        {
            var inst = GenerateIssueSync();
            GenerateSrcDestRedmineParams(out RedmineUtility.RedmineParams SrcParams, out RedmineUtility.RedmineParams DestParams);

            inst.TicketsAddOrSync(TicketNumbers, SrcParams, DestParams);
        }

        private static void KeywordAdd(IEnumerable<int> TicketNumbers, string Keyword)
        {
            var inst = GenerateIssueSync();
            GenerateSrcDestRedmineParams(out RedmineUtility.RedmineParams SrcParams, out RedmineUtility.RedmineParams DestParams);

            foreach (var TicketNo in TicketNumbers)
            {
                Trace($"KeywordAdd Start({TicketNo}, {Keyword})");
                inst.KeywordAdd(TicketNo, Keyword, SrcParams, DestParams);
            }
        }
        private static void KeywordDelete(IEnumerable<int> TicketNumbers, string Keyword)
        {
            var inst = GenerateIssueSync();
            GenerateSrcDestRedmineParams(out RedmineUtility.RedmineParams SrcParams, out RedmineUtility.RedmineParams DestParams);

            foreach (var TicketNo in TicketNumbers)
            {
                Trace($"KeywordDelete Start({TicketNo}, {Keyword})");
                inst.KeywordReplace(TicketNo, Keyword, null, SrcParams, DestParams);
            }
        }
        private static void KeywordReplace(IEnumerable<int> TicketNumbers, string KeywordOld, string KeywordNew)
        {
            var inst = GenerateIssueSync();
            GenerateSrcDestRedmineParams(out RedmineUtility.RedmineParams SrcParams, out RedmineUtility.RedmineParams DestParams);

            foreach (var TicketNo in TicketNumbers)
            {
                Trace($"KeywordReplace Start({TicketNo}, {KeywordOld}, {KeywordNew})");
                inst.KeywordReplace(TicketNo, KeywordOld, KeywordNew, SrcParams, DestParams);
            }
        }

        private static void TicketsAddOrSyncBoth(string SrcQueryURL, string DestQueryURL, string SrcProjectName, string DestProjectName)
        {
            var inst = GenerateIssueSync();
            GenerateSrcDestRedmineParams(out RedmineUtility.RedmineParams SrcParams, out RedmineUtility.RedmineParams DestParams);
            SrcParams.FixedParam_Project = SrcProjectName;
            DestParams.FixedParam_Project = DestProjectName;

            Trace($"TicketsAddOrSyncBoth Start({SrcQueryURL}, {DestQueryURL}, {SrcProjectName}, {DestProjectName})");
            inst.TicketsAddOrSyncBoth(SrcQueryURL, DestQueryURL, SrcParams, DestParams).Wait();
        }
    }
}
