namespace GuardianCapitalLLC.Models
{
    public class UserFile
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ContentType { get; set; }
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
    }

}
