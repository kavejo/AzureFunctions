namespace AzureFunctions
{
    public enum EmailMessageRequestType
    {
        SendMail = 0,
        SendMailWithPIIScan = 1,
        SendMailWithHarmfulContentScan = 2,
        SendMailWithPIIAndHarmfulContentScan = 3,
        SendMailWithGeneragedBody = 4
    }
}
