namespace usercenter_backend.Model.Request
{
    public record UserRegisterRequest (string userAccount, string userPassword, string checkPassword);

}
