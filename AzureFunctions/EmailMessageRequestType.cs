namespace AzureFunctions
{
    public enum EmailMessageRequestType
    {
        SendMail = 0,
        SendMailWithPIIScan = 1,
        SendWithPIIRedacted = 2,
        SendMailWithHarmfulContentScan = 3,
        SendMailWithPIIAndHarmfulContentScan = 4,
        SendMailWithGeneragedBody = 5
    }
}
