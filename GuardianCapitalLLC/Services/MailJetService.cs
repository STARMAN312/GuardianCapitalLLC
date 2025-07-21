using Mailjet.Client;
using Mailjet.Client.Resources;
using Mailjet.Client.TransactionalEmails;
using Newtonsoft.Json.Linq;

public class MailJetService
{
    private readonly IConfiguration _config;

    public MailJetService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<bool> SendVerificationCode(string to, string verificationCode)
    {
        var apiKeyPublic = _config["Mailjet:PublicKey"];
        var apiKeyPrivate = _config["Mailjet:PrivateKey"];
        var fromEmail = _config["Mailjet:FromEmail"];
        var fromName = _config["Mailjet:FromName"];
        long templateId = 7168332;

        MailjetClient client = new MailjetClient(apiKeyPublic, apiKeyPrivate);

        MailjetRequest request = new MailjetRequest
        {
            Resource = Send.Resource
        };

        var email = new TransactionalEmailBuilder()
            .WithTemplateId(templateId)
            .WithTo(new SendContact(to, to))
            .WithTemplateLanguage(true)
            .WithVariables(new Dictionary<string, object>
                {
                    { "verification_code", verificationCode },

                })
            .Build();

        var response = await client.SendTransactionalEmailAsync(email);

        return true;
    }

    public async Task<bool> SendCreatedUser(string to, string username, string password)
    {
        var apiKeyPublic = _config["Mailjet:PublicKey"];
        var apiKeyPrivate = _config["Mailjet:PrivateKey"];
        var fromEmail = _config["Mailjet:FromEmail"];
        var fromName = _config["Mailjet:FromName"];
        long templateId = 7168382;

        MailjetClient client = new MailjetClient(apiKeyPublic, apiKeyPrivate);

        MailjetRequest request = new MailjetRequest
        {
            Resource = Send.Resource
        };

        var email = new TransactionalEmailBuilder()
            .WithTemplateId(templateId)
            .WithTo(new SendContact(to, to))
            .WithTemplateLanguage(true)
            .WithVariables(new Dictionary<string, object>
                {
                    { "username", username },
                    { "user_pass", password },

                })
            .Build();

        var response = await client.SendTransactionalEmailAsync(email);

        return true;
    }

    public async Task<bool> SendUpdatedCredentials(string to, string username, string password)
    {
        var apiKeyPublic = _config["Mailjet:PublicKey"];
        var apiKeyPrivate = _config["Mailjet:PrivateKey"];
        var fromEmail = _config["Mailjet:FromEmail"];
        var fromName = _config["Mailjet:FromName"];
        long templateId = 7172268;

        MailjetClient client = new MailjetClient(apiKeyPublic, apiKeyPrivate);

        MailjetRequest request = new MailjetRequest
        {
            Resource = Send.Resource
        };

        var email = new TransactionalEmailBuilder()
            .WithTemplateId(templateId)
            .WithTo(new SendContact(to, to))
            .WithTemplateLanguage(true)
            .WithVariables(new Dictionary<string, object>
                {
                    { "username", username },
                    { "user_pass", password },
                }
            )
            .Build();

        var response = await client.SendTransactionalEmailAsync(email);

        return true;
    }
}
