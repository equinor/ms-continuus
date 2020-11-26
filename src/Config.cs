using System;
public class Config
{

    public string ORGANIZATION { get; }
    public string GITHUB_TOKEN { get; }
    
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
            this.GITHUB_TOKEN = "test";
        }
    }
}
