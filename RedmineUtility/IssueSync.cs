using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Security;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Xml.Linq;

using Redmine.Net.Api;
using Redmine.Net.Api.Types;

namespace RedmineUtility
{
    public class RedmineParams
    {

        /** RedmineのルートURL */
        public string RedmineRootUrl { get; set; }

        /** RedmineのREST APIアクセスキー */
        public string RedmineAccessKey { get; set; }

        /** キーワードに示すリンク先チケット番号のPrefix。リンク先のRedmineを特定できるキーワードを与える。 */
        public string LinkKeywordPrefix { get; set; }

        /** Redmineのチケットで固定したい値。プロジェクト名。この値がnullならコピー元の値をコピーし、nullでなければコピー元の値を無視してこの値を使用する。 */
        public string FixedParam_Project { get; set; }

        /** Redmineのチケットで固定したい値。トラッカー。この値がnullならコピー元の値をコピーし、nullでなければコピー元の値を無視してこの値を使用する。 */
        public string FixedParam_Tracker { get; set; }

        /** DLLと同じフォルダにある、Usersを記載したXMLのファイルパス（Src側）。管理者権限がないとIDの一覧が取れないため、事前に取得したXMLを置く。 */
        public string UsersFilePath;

        bool IsInitUserNameDic;
        Dictionary<string, int> _UsersNameIdDic;
        public Dictionary<string, int> UsersNameIdDic
        {
            get
            {
                if (!IsInitUserNameDic)
                {
                    _UsersNameIdDic = ReadUsersXml(UsersFilePath);
                    IsInitUserNameDic = true;
                }

                return _UsersNameIdDic;
            }
        }
        private static Dictionary<string, int> ReadUsersXml(string FilePath)
        {
            Dictionary<string, int> UsersNameId = null;
            if (System.IO.File.Exists(FilePath))
            {
                UsersNameId = new Dictionary<string, int>();
                var xdoc = XDocument.Load(FilePath);
                foreach (var User in xdoc.Root.Elements("user"))
                {
                    UsersNameId.Add($"{User.Element("lastname").Value} {User.Element("firstname").Value}", int.Parse(User.Element("id").Value));
                }
            }

            return UsersNameId;
        }
    }

    /**
     * @brief Issue同士の同期を行う。公開範囲が異なる2つのRedmineがあり、それらの同期を取る、という用途を想定。
     */
    public class IssueSync
    {

        
        public event Action<string> AddTraceLog;

        void TraceOut(string msg)
        {
            AddTraceLog?.Invoke(msg);
        }

        /**
         * @brief Src側Redmineのチケットと対応するDest側Redmineのチケットを探し、見つかったら双方向同期する。見つからなかったらコピーして追加する。
         * 1回実行ごとにDest側Redmineのチケットを総チェックする実装なので、出来るだけまとめて実行した方が効率が良い。
         * @param [in] SrcTickets 処理するSrc側Redmineのチケット番号。複数同時処理可能。
         */
        public void TicketsAddOrSync(IEnumerable<int> SrcTickets, RedmineParams redmineParamSrc, RedmineParams redmineParamDest)
        {
            var redmineSrc = new RedmineManager(redmineParamSrc.RedmineRootUrl, redmineParamSrc.RedmineAccessKey);
            var redmineDest = new RedmineManager(redmineParamDest.RedmineRootUrl, redmineParamDest.RedmineAccessKey);


            //あまり大量に送りつけるとサーバーの負荷が高いので、10個ごとに10秒のWaitを入れる
            var Chunks = SrcTickets.Chunks(10);

            bool IsFirstLoop = true;

            foreach (var Chunk in Chunks)
            {
                if (IsFirstLoop)
                {
                    IsFirstLoop = false;
                }
                else
                {
                    System.Threading.Thread.Sleep(10 * 1000);
                }

                var TimeoutItems = new ConcurrentQueue<int>();

                Parallel.ForEach(Chunk, SrcTicket =>
                {
                    try
                    {
                        TicketAddOrSync_OneItem(SrcTicket, redmineSrc, redmineParamSrc, redmineParamDest);
                    }
                    catch (Redmine.Net.Api.Exceptions.RedmineTimeoutException ex)
                    {
                        TraceOut($"Timeout, {nameof(SrcTicket)}={SrcTicket}, {ex.Message}");
                        TimeoutItems.Enqueue(SrcTicket);
                    }
                    catch (WebException ex)
                    {
                        // Redmineの不可状況によってホストへの接続失敗の例外が発生する場合があるためリトライ対象とする
                        TraceOut($"WebException, {nameof(SrcTicket)}={SrcTicket}, {ex.Message}");
                        TimeoutItems.Enqueue(SrcTicket);
                    }
                    catch (Redmine.Net.Api.Exceptions.RedmineException ex)
                    {
                        // 上記WebExceptionと同様に接続失敗の例外で上がるためリトライ対象とする
                        // 本来、InnerExceptionに含まれていた場合にリトライとするのが良いが社内ツールであり時間をかけたくないため間に合わせの対応を取っている
                        TraceOut($"RedmineException, {nameof(SrcTicket)}={SrcTicket}, {ex.Message}");
                        TimeoutItems.Enqueue(SrcTicket);
                    }
                    catch (Exception ex)
                    {
                        TraceOut($"TicketSync Excepion, {nameof(SrcTicket)}={SrcTicket}, {ex.ToString()}");
                        throw;
                    }
                });

                if (TimeoutItems.Any())
                {
                    //1ループの中でタイムアウトしたチケットがあった場合は、それでも負荷が高すぎたという事なので、大きめに1分のWaitを入れてからリトライする
                    System.Threading.Thread.Sleep(60 * 1000);

                    var Items = TimeoutItems.ToArray();
                    Parallel.ForEach(Items, SrcTicket =>
                    {
                        try
                        {
                            TicketAddOrSync_OneItem(SrcTicket, redmineSrc, redmineParamSrc, redmineParamDest);
                        }
                        catch (Exception ex)
                        {
                            TraceOut($"TicketSync Excepion, {nameof(SrcTicket)}={SrcTicket}, {ex.ToString()}");
                            throw;
                        }
                    });
                }

            }
        }

        private void TicketAddOrSync_OneItem(int SrcTicket, RedmineManager redmineSrc, RedmineParams redmineParamSrc, RedmineParams redmineParamDest)
        {
            var SrcIssue = redmineSrc.GetObject<Issue>(SrcTicket.ToString(), new NameValueCollection());
            var SrcKeyword = SrcIssue.CustomFields.First(item => item.Name == "キーワード").Values.First().Info;

            var DestKeyword = SrcKeyword.Split(' ').FirstOrDefault(item => item.StartsWith($"{redmineParamDest.LinkKeywordPrefix}#"));
            if (DestKeyword == null)
            {
                TraceOut($"TicketSync Start({SrcTicket}, {null})");
                TicketSync(SrcTicket, null, redmineParamSrc, redmineParamDest);
            }
            else
            {
                var DestNum = int.Parse(DestKeyword.Replace($"{redmineParamDest.LinkKeywordPrefix}#", null));
                TraceOut($"TicketSync Start({SrcTicket}, {DestNum})");
                TicketSync(SrcTicket, DestNum, redmineParamSrc, redmineParamDest);
            }
        }


        /**
         * @brief 2つのチケットの内容を双方向に同期する。
         * * 双方のキーワードに相手のチケット番号を示す「＜prefix＞#＜チケット番号＞」を追加
         * * 注記以外は、更新日時が新しいほうのチケットを古いほうのチケットに対してすべて上書き
         * * 注記は、双方向にテキストを追加する。テキスト追加の判断は、注記1つずつについて相手方に全く同じテキストがあるかどうかチェックし、1つもなければ追加対象とする。注記はテキストのコピーのみ対応し、フィールド変更履歴やユーザーなどの追加情報はコピーしない。
         * @param [in] SrcTicketNumber 1つ目のチケット
         * @param [in/opt] DestTicketNumber 2つ目のチケット。nullを渡すと、SrcTicketNumberの内容をコピーしたチケットを新規作成する。
         */
        public void TicketSync(int SrcTicketNumber, int? DestTicketNumber, RedmineParams redmineParamSrc, RedmineParams redmineParamDest)
        {

            var redmineSrc = new RedmineManager(redmineParamSrc.RedmineRootUrl, redmineParamSrc.RedmineAccessKey);
            var redmineDest = new RedmineManager(redmineParamDest.RedmineRootUrl, redmineParamDest.RedmineAccessKey);

            var ParamsJouralsInclude = new NameValueCollection {
                { RedmineKeys.INCLUDE, RedmineKeys.JOURNALS }
            };

            var SrcIssue = redmineSrc.GetObject<Issue>(SrcTicketNumber.ToString(), ParamsJouralsInclude);

            if (DestTicketNumber == null)
            {
                //同期先新規作成の場合は、単純に追加

                var DestIssue = TicketCreate_CopyFromSrcTicket(redmineSrc, redmineDest, SrcIssue, redmineParamSrc, redmineParamDest);
                TicketJournalsSync(redmineSrc, redmineDest, SrcIssue, DestIssue, redmineParamSrc, redmineParamDest);
            }
            else
            {
                //既存チケット同士の同期の場合は、新しいほうで古いほうを上書きする
                var DestIssue = redmineDest.GetObject<Issue>(DestTicketNumber.ToString(), ParamsJouralsInclude);
                TicketSyncBoth(redmineSrc, redmineDest, SrcIssue, DestIssue, redmineParamSrc, redmineParamDest);
                TicketJournalsSync(redmineSrc, redmineDest, SrcIssue, DestIssue, redmineParamSrc, redmineParamDest);

            }

        }


        private void TicketSyncBoth(RedmineManager redmineSrc, RedmineManager redmineDest, Issue SrcIssue, Issue DestIssue, RedmineParams redmineParamSrc, RedmineParams redmineParamDest)
        {
            //新しいほうのチケットを古いほうのチケットに上書きする。新しいほうのチケットは、キーワード追加のみ行う。


            if (DestIssue.UpdatedOn <= SrcIssue.UpdatedOn)
            {
                TicketCopy(SrcIssue, DestIssue, redmineDest, redmineParamDest.FixedParam_Project, redmineParamDest.FixedParam_Tracker, redmineParamSrc, redmineParamDest);
                TicketCustomFieldCopy(SrcIssue, DestIssue);
                TicketLinkKeyword_AddToDestTicket(SrcIssue, DestIssue, redmineParamSrc.LinkKeywordPrefix, redmineDest);
                redmineDest.UpdateObject(DestIssue.Id.ToString(), DestIssue);

                TicketLinkKeyword_AddToDestTicket(DestIssue, SrcIssue, redmineParamDest.LinkKeywordPrefix, redmineSrc);
                redmineSrc.UpdateObject(SrcIssue.Id.ToString(), SrcIssue);
            }
            else
            {
                TicketCopy(DestIssue, SrcIssue, redmineSrc, redmineParamSrc.FixedParam_Project, redmineParamSrc.FixedParam_Tracker, redmineParamDest, redmineParamSrc);
                TicketCustomFieldCopy(DestIssue, SrcIssue);
                TicketLinkKeyword_AddToDestTicket(DestIssue, SrcIssue, redmineParamDest.LinkKeywordPrefix, redmineSrc);
                redmineSrc.UpdateObject(SrcIssue.Id.ToString(), SrcIssue);

                TicketLinkKeyword_AddToDestTicket(SrcIssue, DestIssue, redmineParamSrc.LinkKeywordPrefix, redmineDest);
                redmineDest.UpdateObject(DestIssue.Id.ToString(), DestIssue);
            }
        }

        private Issue TicketCreate_CopyFromSrcTicket(RedmineManager redmineSrc, RedmineManager redmineDest, Issue SrcIssue, RedmineParams redmineParamSrc, RedmineParams redmineParamDest)
        {
            var NewTicket = new Issue();

            TicketCopy(SrcIssue, NewTicket, redmineDest, redmineParamDest.FixedParam_Project, redmineParamDest.FixedParam_Tracker, redmineParamSrc, redmineParamDest);
            NewTicket = redmineDest.CreateObject(NewTicket);
            TicketCustomFieldCopy(SrcIssue, NewTicket);
            TicketLinkKeyword_AddToDestTicket(SrcIssue, NewTicket, redmineParamSrc.LinkKeywordPrefix, redmineDest);
            redmineDest.UpdateObject(NewTicket.Id.ToString(), NewTicket);

            TicketLinkKeyword_AddToDestTicket(NewTicket, SrcIssue, redmineParamDest.LinkKeywordPrefix, redmineSrc);
            redmineSrc.UpdateObject(SrcIssue.Id.ToString(), SrcIssue);

            return NewTicket;
        }


        private void TicketJournalsSync(RedmineManager redmineSrc, RedmineManager redmineDest, Issue SrcIssue, Issue DestIssue, RedmineParams redmineParamSrc, RedmineParams redmineParamDest)
        {
            if (DestIssue.Journals == null)
            {
                foreach (var DiffJ in SrcIssue.Journals)
                {
                    JournalAdd(redmineParamDest.RedmineRootUrl, redmineParamDest.RedmineAccessKey, DestIssue.Id, DiffJ.Notes, new DateTimeOffset((DateTime)DiffJ.CreatedOn));
                }
            }
            else
            {
                var DiffJSrcs = SrcIssue.Journals.Where(item => !DestIssue.Journals.Any(item2 =>
                    item.Notes == item2.Notes
                ));

                foreach (var DiffJ in DiffJSrcs)
                {
                    JournalAdd(redmineParamDest.RedmineRootUrl, redmineParamDest.RedmineAccessKey, DestIssue.Id, DiffJ.Notes, new DateTimeOffset((DateTime)DiffJ.CreatedOn));
                }

                var DiffJDests = DestIssue.Journals.Where(item => !SrcIssue.Journals.Any(item2 =>
                    item.Notes == item2.Notes
                ));

                foreach (var DiffJ in DiffJDests)
                {
                    JournalAdd(redmineParamSrc.RedmineRootUrl, redmineParamSrc.RedmineAccessKey, SrcIssue.Id, DiffJ.Notes, new DateTimeOffset((DateTime)DiffJ.CreatedOn));
                }

            }

        }

        private void TicketCopy(Issue SrcIssue, Issue DestIssue, RedmineManager redmineDest, string DestFixedParam_Project, string DestFixedParam_Tracker, RedmineParams redmineParamSrc, RedmineParams redmineParamDest)
        {
            DestIssue.Subject = SrcIssue.Subject;
            DestIssue.Description = SrcIssue.Description;
            DestIssue.CreatedOn = SrcIssue.CreatedOn;
            DestIssue.UpdatedOn = SrcIssue.UpdatedOn;
            DestIssue.ClosedOn = SrcIssue.ClosedOn;
            DestIssue.DoneRatio = SrcIssue.DoneRatio;
            DestIssue.EstimatedHours = SrcIssue.EstimatedHours;
            DestIssue.Notes = SrcIssue.Notes;
            DestIssue.SpentHours = SrcIssue.SpentHours;
            DestIssue.StartDate = SrcIssue.StartDate;
            DestIssue.DueDate = SrcIssue.DueDate;
            {
                string Name = SrcIssue.Status.Name;
                DestIssue.Status = redmineDest.GetObjects<IssueStatus>(new NameValueCollection()).FirstOrDefault(item => item.Name == Name);
                if(DestIssue.Status == null)
                {
                    throw new Exception($"Status Not Found(Dest Redmine), Value={Name}");
                }
            }
            {
                string Name = SrcIssue.Priority.Name;

                DestIssue.Priority = redmineDest.GetObjects<IssuePriority>(new NameValueCollection()).FirstOrDefault(item => item.Name == Name);
                if (DestIssue.Priority == null)
                {
                    throw new Exception($"Priority Not Found(Dest Redmine), Value={Name}");
                }
            }
            {
                string Name;

                if (string.IsNullOrEmpty(DestFixedParam_Project))
                {
                    Name = Name = SrcIssue.Project.Name;
                }
                else
                {
                    Name = Name = DestFixedParam_Project;
                }

                var ParamsProject = new NameValueCollection();
                ParamsProject.Add(RedmineKeys.INCLUDE, RedmineKeys.ISSUE_CATEGORIES);
                var DestProject = redmineDest.GetObjects<Project>(ParamsProject).FirstOrDefault(item => item.Name == Name);
                if (DestProject == null)
                {
                    throw new Exception($"Project Not Found(Dest Redmine), Value={Name}");
                }

                DestIssue.Project = DestProject;

                DestIssue.Category = DestProject.IssueCategories.FirstOrDefault(item => item.Name == SrcIssue.Category.Name);
                if (DestIssue.Category == null)
                {
                    throw new Exception($"Category Not Found(Dest Redmine), Value={SrcIssue.Category.Name}");
                }
            }
            {
                string Name;

                if (string.IsNullOrEmpty(DestFixedParam_Tracker))
                {
                    Name = Name = SrcIssue.Tracker.Name;
                }
                else
                {
                    Name = Name = DestFixedParam_Tracker;
                }

                DestIssue.Tracker = redmineDest.GetObjects<Tracker>(new NameValueCollection()).FirstOrDefault(item => item.Name == Name);
                if (DestIssue.Tracker == null)
                {
                    throw new Exception($"Tracker Not Found(Dest Redmine), Value={Name}");
                }
            }
            {
                //ユーザー名は、事前情報がある場合のみ処理する。
                if (redmineParamSrc.UsersNameIdDic != null && redmineParamDest.UsersNameIdDic != null)
                {
                    //ユーザー名は、相手にも同じユーザー名がある場合のみ更新する（Src,Destで一致しているとは限らないため）
                    int UserId;
                    if(redmineParamDest.UsersNameIdDic.TryGetValue(SrcIssue.AssignedTo.Name, out UserId))
                    {
                        DestIssue.AssignedTo = new IdentifiableName() { Id = UserId };
                    }
                }

            }
            {
                //バージョンは、相手にも同じ名前がある場合のみ更新する（Src,Destで一致しているとは限らないため）
                string Name = SrcIssue.FixedVersion.Name;

                var ParamsGet = new NameValueCollection();
                ParamsGet.Add(RedmineKeys.PROJECT_ID, DestIssue.Project.Id.ToString());
                var DestVersion = redmineDest.GetObjects<Redmine.Net.Api.Types.Version>(ParamsGet).FirstOrDefault(item => item.Name == Name);
                if (DestVersion != null)
                {
                    DestIssue.FixedVersion = DestVersion;
                }

            }

        }


        private static void TicketCustomFieldCopy(Issue SrcIssue, Issue DestIssue)
        {
            foreach (var SrcField in SrcIssue.CustomFields)
            {
                var DestField = DestIssue.CustomFields.FirstOrDefault(item => item.Name == SrcField.Name);

                if(DestField == null)
                {
                    continue;
                }

                DestField.Values = new List<CustomFieldValue>(SrcField.Values);

            }
        }

        /** チケットリンク用キーワードを、Dest側のチケットに追加する。すでに同じキーワードがある場合は何もしない。
            * Dest側に、Src側のチケットを指すキーワードを追記するという事。そのため、SrcLinkKeywordPrefixにはSrc側のPrefixを渡す。 */
        private static void TicketLinkKeyword_AddToDestTicket(Issue SrcIssue, Issue DestIssue, string SrcLinkKeywordPrefix, RedmineManager redmineDest)
        {
            var SrcKeyword = SrcIssue.CustomFields.FirstOrDefault(item => item.Name == "キーワード");
            if (SrcKeyword == null)
            {
                return;
            }

            var DestKeyword = DestIssue.CustomFields.FirstOrDefault(item => item.Name == "キーワード");

            if (DestKeyword == null)
            {
                return;
            }

            var AddValue = $"{SrcLinkKeywordPrefix}#{SrcIssue.Id}";

            if (0 <= DestKeyword.Values.First().Info.IndexOf(AddValue))
            {
                return;                
            }

            DestKeyword.Values.First().Info = string.Join(" ", DestKeyword.Values.First().Info, AddValue).Trim();

        }
        public void JournalAdd(string RedmineRottURL, string AccessKey, int AddTicketId, string AddComment, DateTimeOffset CreatedOn)
        {
            //CreatedOnは強制設定できないようなので、XMLには含めるが実際には反映されない


            var TargetId = AddTicketId.ToString();

            using (var wc = new WebClient())
            {
                var URL = $"{RedmineRottURL}/issues/{TargetId}.xml";

                var Data = $"<issue><notes>{SecurityElement.Escape(AddComment)}</notes><created_on>{CreatedOn.ToString()}</created_on></issue>";

                wc.Encoding = Encoding.UTF8;
                wc.Headers.Add("Content-Type", "text/xml");
                wc.Headers.Add("X-Redmine-API-Key", AccessKey);

                string resData = wc.UploadString(URL, "PUT", Data);


            }
        }

        public void KeywordAdd(int SrcTicketNumber, string AddKeyword, RedmineParams redmineParamSrc, RedmineParams redmineParamDest)
        {

            var redmineSrc = new RedmineManager(redmineParamSrc.RedmineRootUrl, redmineParamSrc.RedmineAccessKey);

            var SrcIssue = redmineSrc.GetObject<Issue>(SrcTicketNumber.ToString(), new NameValueCollection { });

            var SrcKeyword = SrcIssue.CustomFields.First(item => item.Name == "キーワード");

            SrcKeyword.Values.First().Info = string.Join(" ", SrcKeyword.Values.First().Info, AddKeyword);

            redmineSrc.UpdateObject(SrcIssue.Id.ToString(), SrcIssue);
            
        }
        public void KeywordReplace(int SrcTicketNumber, string oldValue, string newValue, RedmineParams redmineParamSrc, RedmineParams redmineParamDest)
        {

            var redmineSrc = new RedmineManager(redmineParamSrc.RedmineRootUrl, redmineParamSrc.RedmineAccessKey);

            var SrcIssue = redmineSrc.GetObject<Issue>(SrcTicketNumber.ToString(), new NameValueCollection { });

            var SrcKeyword = SrcIssue.CustomFields.First(item => item.Name == "キーワード");

            SrcKeyword.Values.First().Info = SrcKeyword.Values.First().Info.Replace(oldValue, newValue);

            if(newValue == null)
            {
                //削除した場合はスペースが2連続で残る場合があるので、1つに置換
                SrcKeyword.Values.First().Info = SrcKeyword.Values.First().Info.Replace("  ", " ");
            }

            redmineSrc.UpdateObject(SrcIssue.Id.ToString(), SrcIssue);


        }

        /**
         * @brief クエリに対応するチケットを探し、相手方へ同期する（相手方に無ければ追加）。クエリはまずSrc側を処理し、次にDest側を処理する。
         */
        public async Task TicketsAddOrSyncBoth(string SrcQuery, string DestQuery, RedmineParams redmineParamSrc, RedmineParams redmineParamDest)
        {
            var redmineSrc = new RedmineManager(redmineParamSrc.RedmineRootUrl, redmineParamSrc.RedmineAccessKey);

            SrcQuery = SrcQuery.Replace("issues?", "issues.xml?");

            List<int> SrcIssueIds = await HttpGetIssueIds(SrcQuery, redmineSrc);

            TicketsAddOrSync(SrcIssueIds, redmineParamSrc, redmineParamDest);

            
            var redmineDest = new RedmineManager(redmineParamDest.RedmineRootUrl, redmineParamDest.RedmineAccessKey);

            DestQuery = DestQuery.Replace("issues?", "issues.xml?");

            List<int> DestIssueIds = await HttpGetIssueIds(DestQuery, redmineDest);

            TicketsAddOrSync(DestIssueIds, redmineParamDest, redmineParamSrc);


        }

        private static async Task<List<int>> HttpGetIssueIds(string QueryUrl, RedmineManager redmineSrc)
        {
            List<int> IssueIds = new List<int>();
            var queryUrl = $"{QueryUrl}&key={redmineSrc.ApiKey}";

            XDocument firstXml = await HttpGetXml(queryUrl);

            foreach (var Issue in firstXml.Root.Elements("issue"))
            {
                IssueIds.Add(int.Parse(Issue.Element("id").Value));

            }

            var Limit = int.Parse(firstXml.Root.Attribute("limit").Value);
            var TotalCount = int.Parse(firstXml.Root.Attribute("total_count").Value);

            if (Limit <= TotalCount)
            {
                var PageCount = (int)Math.Ceiling((double)TotalCount / Limit);
                for (int i = 2; i <= PageCount; i++)
                {
                    var NextXml = await HttpGetXml(queryUrl + $"&page={i}");
                    foreach (var Issue in NextXml.Root.Elements("issue"))
                    {
                        IssueIds.Add(int.Parse(Issue.Element("id").Value));

                    }
                }
            }

            return IssueIds;
        }

        private static async Task<XDocument> HttpGetXml(string queryUrl)
        {
            string APIRet;
            using (var cli = new HttpClient())
            {

                var res = await cli.GetAsync(queryUrl);
                if (!res.IsSuccessStatusCode)
                {
                    throw new Exception($"HttpResponse Fail, Uri={queryUrl}, {res.ToString()}");
                }
                APIRet = await res.Content.ReadAsStringAsync();
            }

            var XDoc = XDocument.Parse(APIRet);
            return XDoc;
        }
    }
}
