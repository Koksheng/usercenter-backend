namespace usercenter_backend.Common
{
    public class ErrorCode
    {
        public int Code { get; }
        public string Message { get; }
        public string Description { get; }

        private ErrorCode(int code, string message, string description)
        {
            Code = code;
            Message = message;
            Description = description;
        }

        public static readonly ErrorCode SUCCESS = new ErrorCode(0, "ok", "");
        public static readonly ErrorCode PARAMS_ERROR = new ErrorCode(40000, "请求参数错误", "");
        public static readonly ErrorCode NULL_ERROR = new ErrorCode(40001, "请求数据为空", "");
        public static readonly ErrorCode NOT_LOGIN = new ErrorCode(40100, "未登录", "");
        public static readonly ErrorCode NO_AUTH = new ErrorCode(40101, "无权限", "");
        // Add more error codes as needed
    }

    //public enum ErrorCode
    //{
    //    SUCCESS(0,"ok","");
    //    PARAMS_ERROR(40000,"请求参数错误",""),
    //    NULL_ERROR(40001,"请求数据为空",""),
    //    NOT_LOGIN(40100,"未登录",""),
    //    NO_AUTH(40101,"无权限","");


    //    private final int code;
    //    private final string message;
    //    private final string description;

    //    private ErrorCode(int code, string message, string description)
    //    {
    //    this.code = code;
    //    this.message = message;
    //    this.description = description;
    //    }
    //}
}
