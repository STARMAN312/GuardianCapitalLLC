namespace GuardianCapitalLLC.Models
{
    public class FailedLoginLog
    {
        public int Id { get; set; }
        public string EmailOrUsername { get; set; }
        public DateTime AttemptedAt { get; set; }
        public string IPAddress { get; set; }
    }

}
