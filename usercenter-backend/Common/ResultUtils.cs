namespace usercenter_backend.Common
{
    public class ResultUtils
    {
        public static BaseResponse<T> success<T>(T data)
        {
            return new BaseResponse<T> ( 0, data, "ok" );
        }
    }
}
