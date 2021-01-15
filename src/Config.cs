using System;
public class Config
{

    public string ORGANIZATION { get; }
    public string BLOB_CONTAINER { get; }
    public string BLOB_TAG { get; }
    public int WEEKLY_RETENTION { get; }
    public int MONTHLY_RETENTION { get; }
    public int YEARLY_RETENTION { get; }
    public string GITHUB_TOKEN { get; }
    public string STORAGE_KEY { get; }

    public Config()
    {
        this.ORGANIZATION = Environment.GetEnvironmentVariable("GITHUB_ORG");
        if (this.ORGANIZATION == null)
        {
            throw new Exception("Environment variable 'GITHUB_ORG' missing");
        }

        this.BLOB_CONTAINER = Environment.GetEnvironmentVariable("BLOB_CONTAINER");
        if (this.BLOB_CONTAINER == null)
        {
            Console.WriteLine("WARNING: Environment variable 'BLOB_CONTAINER' not set. Will assume a container named 'github-archives'");
            this.BLOB_CONTAINER = "github-archives";
        }

        this.BLOB_TAG = Environment.GetEnvironmentVariable("BLOB_TAG");
        if (this.BLOB_TAG == null)
        {
            // Set tag to 'monthly' for the first week of the month.
            if(DateTime.Today.Day < 8){
                this.BLOB_TAG = "monthly";
            }else{
                this.BLOB_TAG = "weekly";
            }
        }

        var weekly_from_env = Environment.GetEnvironmentVariable("WEEKLY_RETENTION");
        if (weekly_from_env == null)
        {
            this.WEEKLY_RETENTION = 60;
        }else{
            this.WEEKLY_RETENTION = int.Parse(weekly_from_env);
        }

        var monthly_from_env = Environment.GetEnvironmentVariable("MONTHLY_RETENTION");
        if (monthly_from_env == null)
        {
            this.MONTHLY_RETENTION = 230;
        }else{
            this.MONTHLY_RETENTION = int.Parse(monthly_from_env);
        }

        var yearly_from_env = Environment.GetEnvironmentVariable("YEARLY_RETENTION");
        if (yearly_from_env == null)
        {
            this.YEARLY_RETENTION = 420;
        }else{
            this.YEARLY_RETENTION = int.Parse(yearly_from_env);
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
    public override string ToString()
        {
            var org = $"\n\tORGANIZATION: {ORGANIZATION}";
            var container = $"\n\tBLOB_CONTAINER: {BLOB_CONTAINER}";
            var tag = $"\n\tBLOB_TAG: {BLOB_TAG}";
            var week_ret = $"\n\tWEEKLY_RETENTION: {WEEKLY_RETENTION}";
            var month_ret = $"\n\tMONTHLY_RETENTION: {MONTHLY_RETENTION}";
            var year_ret = $"\n\tYEARLY_RETENTION: {YEARLY_RETENTION}";

            return $"Configuration settings:"+org+container+tag+week_ret+month_ret+year_ret;
        }
}
