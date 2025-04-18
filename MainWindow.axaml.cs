using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using HtmlAgilityPack;
using SukiUI.Controls;
using SukiUI.Dialogs;

namespace Hexo.Butterfly.BackLinksChecker
{
    public partial class MainWindow : SukiWindow
    {
        private readonly WindowNotificationManager notificationManager;
        private readonly SukiDialogManager sukiDialogManager;
        private const int BatchSize = 15; // 每批检查的卡片数量

        public List<Friend> friends = [];
        public List<string> pages = [];
        private List<FriendCard> friendCards = [];

        public MainWindow()
        {
            InitializeComponent();
            notificationManager = new WindowNotificationManager(GetTopLevel(this));
            sukiDialogManager = new SukiDialogManager();
            DialogHost.Manager = sukiDialogManager;
            RunButton.Click += async (_, _) => { await Fetch(); };
            PagesTextBox.Text =
                "\"/link\", \"/friends\", \"/links\", \"/pages/link\", \"/links.html\", \"/friendlychain\", \"/youlian\", \"/site/link/\", \"/social/link/\"";
        }

        private async Task Fetch()
        {
            pages.Clear();
            friends.Clear();
            friendCards.Clear();

            List<string> tmpPages = [.. PagesTextBox.Text?.Split([','], StringSplitOptions.RemoveEmptyEntries) ?? []];
            pages = tmpPages.Select(x => $"{x.Trim().Trim('"')}").ToList();

            WrapPanel.Children.Clear();
            CircleProgressBar.IsVisible = true;
            var linkPage = LinkPageTextBox.Text;
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler)
            {
                Timeout = new TimeSpan(0, 0, 40)
            };

            try
            {
                var html = await client.GetStringAsync(linkPage);
                CircleProgressBar.IsVisible = false;
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var flinkListItemNodes = doc.DocumentNode.SelectNodes("//div[@class='flink-list-item']");
                if (flinkListItemNodes != null)
                {
                    friends.AddRange(
                        from aNodes in flinkListItemNodes
                            .Select(flinkListItemNode => flinkListItemNode.SelectNodes("a"))
                            .OfType<HtmlNodeCollection>()
                        from aNode in aNodes
                        select new Friend
                        { Name = aNode.Attributes["title"].Value, Url = aNode.Attributes["href"].Value });
                }

                List<Task> tasks = [];
                
                foreach (var card in friends.Select(friend => new FriendCard(friend.Name, friend.Url)))
                {
                    friendCards.Add(card);
                    WrapPanel.Children.Add(card);
                    tasks.Add(Methods.StartCheck(card));
                }

                _ = Task.WhenAll(tasks);
            }
            catch (Exception exception)
            {
                CircleProgressBar.IsVisible = false;
                Console.WriteLine(exception);
                sukiDialogManager.CreateDialog()
                    .WithTitle("错误")
                    .WithContent(exception)
                    .WithActionButton("关闭", _ => { }, true)
                    .TryShow();
            }
        }
    }
}