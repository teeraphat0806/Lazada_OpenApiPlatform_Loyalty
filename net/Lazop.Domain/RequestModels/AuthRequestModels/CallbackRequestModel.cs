namespace Lazop.Domain.RequestModels.AuthRequestModels
{
    public class CallbackRequestModel
    {
        public string? Code { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
