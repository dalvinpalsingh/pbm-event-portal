namespace PbmEventPortal.Application.DTOs.Common;

public class ResultDTO
{
    public bool IsSuccess { get; set; } = false;
    public string NoticeTitle { get; set; } = string.Empty;
    public string NoticeDetails { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = [];

    public static ResultDTO Fail(string noticeTitle, string? noticeDetails = null, List<string>? errors = null)
        => new() { IsSuccess = false, NoticeTitle = noticeTitle, NoticeDetails = noticeDetails ?? string.Empty, Errors = errors ?? [] };
    public static ResultDTO Success(string noticeTitle, string? noticeDetails = null, List<string>? errors = null)
        => new() { IsSuccess = true, NoticeTitle = noticeTitle, NoticeDetails = noticeDetails ?? string.Empty, Errors = errors ?? [] };
    public static ResultDTO<T> Success<T>(string noticeTitle, string? noticeDetails = null, List<string>? errors = null, T? data = default)
        => new() { IsSuccess = true, NoticeTitle = noticeTitle, NoticeDetails = noticeDetails ?? string.Empty, Errors = errors ?? [], Data = data };
}

public class ResultDTO<T> : ResultDTO
{
    public T? Data { get; set; }
}
