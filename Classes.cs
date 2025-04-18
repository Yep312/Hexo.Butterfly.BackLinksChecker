using System;

namespace Hexo.Butterfly.BackLinksChecker;

public class Friend
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class LinkResult
{
    public string Url { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool IsFound { get; set; } = false;
}

public enum BacklinkStatus
{
    Found,
    NotFound,
    Timeout
}