public class EmailVerificationModel
{
    public string ProductName { get; set; }
    public string ProductFullName { get; set; }
    public string VerificationCode { get; set; }
    public int? ExpInSeconds { get; set; }
}