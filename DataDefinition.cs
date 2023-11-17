public class Source
{
    public Source(string key, string status, int attempts, int? FirstAttemptMinutes, int? LastAttemptMinutes, string description) 
    {
        this.Attempts = attempts;
        this.Key = key;
        if (FirstAttemptMinutes != null)
            this.FirstAttempt = DateTime.Now.AddMinutes((double)-FirstAttemptMinutes);
        if (LastAttemptMinutes != null)
            this.LastAttempt = DateTime.Now.AddMinutes((double)-LastAttemptMinutes);
        this.Status = status;
        this.Description = description;
    }

    public string Id = Guid.NewGuid().ToString("N");
    public string Key { get; set; }
    public string Description { get; set; }
    public DateTime? FirstAttempt { get; set; }
    public DateTime? LastAttempt { get; set; }
	public string Status { get; set; }
	public string? Action { get; set; } = null;
	public string Reason { get; set; }
	public int Attempts { get; set; } = 0;
	public int Cycles { get; set; } = 0;
    public List<string> Log { get; set; } = new();
}

