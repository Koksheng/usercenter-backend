namespace usercenter_backend.Common
{
    public record BaseResponse<T>(int code, T data, string message = "")
    {
        //// Constructor overload with two parameters
        //public BaseResponse(int code, T data) : this(code, data, "")
        //{
        //}
    };


    //// Creating BaseResponse with int data
    //var response1 = new BaseResponse<int>(200, 123, "Success");

    //// Creating BaseResponse with string data
    //var response2 = new BaseResponse<string>(200, "Data", "Success");

    //// Creating BaseResponse with custom object data
    //var customObject = new CustomObject();
    //var response3 = new BaseResponse<CustomObject>(200, customObject, "Success");

}
