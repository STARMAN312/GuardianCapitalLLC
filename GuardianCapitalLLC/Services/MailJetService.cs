using Mailjet.Client;
using Mailjet.Client.Resources;
using Mailjet.Client.TransactionalEmails;
using Newtonsoft.Json.Linq;

public class MailJetService
{
    private readonly IConfiguration _configuration;
    private readonly string _mailJetPublicKey;
    private readonly string _mailJetPrivateKey;

    public MailJetService(IConfiguration config, IConfiguration configuration)
    {
        _configuration = configuration;
        _mailJetPublicKey = _configuration["MailJet:PublicKey"] ?? throw new InvalidOperationException("MailJet Public API key is missing.");
        _mailJetPrivateKey = _configuration["MailJet:PrivateKey"] ?? throw new InvalidOperationException("MailJet Private API key is missing.");
    }

    public async Task<bool> SendVerificationCode(string to, string verificationCode)
    {
        var apiKeyPublic = _mailJetPublicKey;
        var apiKeyPrivate = _mailJetPrivateKey;
        var fromEmail = _configuration["Mailjet:FromEmail"];
        var fromName = _configuration["Mailjet:FromName"];
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
        var apiKeyPublic = _mailJetPublicKey;
        var apiKeyPrivate = _mailJetPrivateKey;
        var fromEmail = _configuration["Mailjet:FromEmail"];
        var fromName = _configuration["Mailjet:FromName"];
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
        var apiKeyPublic = _mailJetPublicKey;
        var apiKeyPrivate = _mailJetPrivateKey;
        var fromEmail = _configuration["Mailjet:FromEmail"];
        var fromName = _configuration["Mailjet:FromName"];
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

    public async Task<bool> SendConfirmedDeposit(string to, string amount, string datetime)
    {
        var apiKeyPublic = _mailJetPublicKey;
        var apiKeyPrivate = _mailJetPrivateKey;
        var fromEmail = _configuration["Mailjet:FromEmail"];
        var fromName = _configuration["Mailjet:FromName"];
        long templateId = 7175445;

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
                    { "depositamount", amount },
                    { "datetime", datetime },
                }
            )
            .Build();

        var response = await client.SendTransactionalEmailAsync(email);

        return true;
    }

    public async Task<bool> SendDailyInterest(string to, string interest, string datetime, string previous_balance, string current_balance)
    {
        var apiKeyPublic = _mailJetPublicKey;
        var apiKeyPrivate = _mailJetPrivateKey;
        var fromEmail = _configuration["Mailjet:FromEmail"];
        var fromName = _configuration["Mailjet:FromName"];
        long templateId = 7181699;

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
                    { "current_balance", current_balance },
                    { "previous_balance", previous_balance },
                    { "interest", interest },
                    { "datetime", datetime },
                }
            )
            .Build();

        var response = await client.SendTransactionalEmailAsync(email);

        return true;
    }
}
