using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using HtmlAgilityPack;
using Avalonia.Threading;
using System.Threading;

namespace Hexo.Butterfly.BackLinksChecker;

public class Methods
{
    public static async Task<BacklinkStatus> StartCheck(FriendCard card)
    {
        return await Task.Run(async () =>
        {
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler)
            {
                Timeout = new TimeSpan(0, 0, 40)
            };

            var backlinkHost = await Dispatcher.UIThread.InvokeAsync(() => App.MainWindow.BackTextBox.Text);
            var pages = await Dispatcher.UIThread.InvokeAsync(() => App.MainWindow.pages);
            var baseUrl = await Dispatcher.UIThread.InvokeAsync(() => card.Url.Text);

            // 创建所有需要检查的URL列表
            var urlsToCheck = pages.Select(page => $"{baseUrl.TrimEnd('/')}{page}").ToList();
            urlsToCheck.Add(baseUrl); // 添加基础URL

            // 创建取消令牌源
            var cts = new CancellationTokenSource();
            var countdownCompleted = new TaskCompletionSource<bool>();

            // 创建倒计时任务
            var countdownTask = Task.Run(async () =>
            {
                try
                {
                    for (int i = 40; i > 0; i--)
                    {
                        if (cts.Token.IsCancellationRequested)
                            break;

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            card.State.Text = $"检查中 {i}秒";
                        });
                        await Task.Delay(1000, cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // 任务被取消，不做任何处理
                }
                finally
                {
                    countdownCompleted.SetResult(true);
                }
            }, cts.Token);

            var hasTimeout = false;
            var allErrors = true;
            var successCount = 0;

            // 创建所有检查任务
            var tasks = urlsToCheck.Select(async url =>
            {
                try
                {
                    var html = await client.GetStringAsync(url);
                    successCount++;
                    allErrors = false;

                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    // 检查页面中是否包含反向链接
                    var links = doc.DocumentNode.SelectNodes("//a[@href]");
                    if (links != null)
                    {
                        foreach (var link in links)
                        {
                            var href = link.GetAttributeValue("href", "");
                            if (href.Contains(backlinkHost))
                            {
                                // 取消倒计时
                                cts.Cancel();
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    card.State.Text = "已找到";
                                    card.UpdateBackground(BacklinkStatus.Found);
                                });
                                return true;
                            }
                        }
                    }
                    return false;
                }
                catch (TaskCanceledException)
                {
                    hasTimeout = true;
                    throw;
                }
                catch (HttpRequestException ex)
                {
                    // HTTP请求错误（如404、403等）
                    return false;
                }
                catch (Exception)
                {
                    // 其他错误
                    return false;
                }
            });

            try
            {
                // 等待所有任务完成，如果任何一个任务返回true（找到反向链接），就返回Found
                var results = await Task.WhenAll(tasks);
                if (results.Any(r => r))
                {
                    // 取消倒计时
                    cts.Cancel();
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        card.State.Text = "已找到";
                        card.UpdateBackground(BacklinkStatus.Found);
                    });
                    await countdownCompleted.Task; // 等待倒计时任务完成
                    return BacklinkStatus.Found;
                }

                // 如果所有任务都完成了但没有找到反向链接
                // 取消倒计时
                cts.Cancel();

                if (allErrors)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        card.State.Text = "访问错误";
                        card.UpdateBackground(BacklinkStatus.Timeout); // 使用超时状态表示错误
                    });
                    await countdownCompleted.Task; // 等待倒计时任务完成
                    return BacklinkStatus.Timeout; // 使用超时状态表示错误
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        card.State.Text = "未找到";
                        card.UpdateBackground(BacklinkStatus.NotFound);
                    });
                    await countdownCompleted.Task; // 等待倒计时任务完成
                    return BacklinkStatus.NotFound;
                }
            }
            catch (Exception)
            {
                // 如果所有任务都出现错误
                // 取消倒计时
                cts.Cancel();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    card.State.Text = hasTimeout ? "超时" : "访问错误";
                    card.UpdateBackground(BacklinkStatus.Timeout);
                });
                await countdownCompleted.Task; // 等待倒计时任务完成
                return BacklinkStatus.Timeout;
            }
        });
    }
}