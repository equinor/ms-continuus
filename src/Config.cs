using System;
public class Config
{

    public string ORGANIZATION { get; }
    public string GITHUB_TOKEN { get; }
    public string STORAGE_KEY { get; }
    
    public Config()
    {
        this.ORGANIZATION = Environment.GetEnvironmentVariable("ORGANIZATION");
        if (this.ORGANIZATION == null)
        {
            this.ORGANIZATION = "equinor";
        }
        
        this.GITHUB_TOKEN = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (this.GITHUB_TOKEN == null)
        {
            Console.WriteLine(
                "WARNING: Environment variable 'GITHUB_TOKEN' not set. Will continue operating on public repositories only");
        }
        
        this.STORAGE_KEY = Environment.GetEnvironmentVariable("STORAGE_KEY");
        if (this.STORAGE_KEY == null)
        {
            throw new Exception("Environment variable 'STORAGE_KEY' missing");
        }
    }
}
