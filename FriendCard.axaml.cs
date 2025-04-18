using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System.Threading;
using System.Diagnostics;
using Avalonia.Threading;

namespace Hexo.Butterfly.BackLinksChecker;

public partial class FriendCard : UserControl
{
    public FriendCard(string name, string url)
    {
        InitializeComponent();
        Name.Text = name;
        Url.Text = url;
        State.Text = "等待检查";
    }

    public void UpdateBackground(BacklinkStatus status)
    {
        Root.Background = status switch
        {
            BacklinkStatus.Found => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.LightGreen),
            BacklinkStatus.NotFound => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.LightPink),
            BacklinkStatus.Timeout => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.LightGray),
            _ => Background
        };
    }

    private void Url_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var launcher = TopLevel.GetTopLevel(this)?.Launcher;
        if (Url.Text != null) launcher?.LaunchUriAsync(new Uri(Url.Text));
    }
}

