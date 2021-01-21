# ms-continuus

Github organization archive organizer

This program is meant to run as a cron job.  
It will do these things;

1. Start a Github Migration
2. Wait for the GH Migration to be ready for downloading
3. Download the archive
4. Upload the archive to AZ Blob storage with a metadata `retentionClass` value
5. Delete blobs where the retention limit is reached
6. Exit

## Configuration

ms-continuus is configured entirely with environment variables.

  |Name   |Default|Description|
  |-------|-------|-----------|
  |BLOB_TAG:|"weekly"("monthly" on first week of month)|Which value to use for the uploaded retentionClass metadata|
  |BLOB_CONTAINER:|"github-archives" |Container in a Azure StorageAccount where the blobs will be stored|
  |GITHUB_ORG:|null|Target Github organization|
  |WEEKLY_RETENTION:|60|Delete blobs with retentionClass='weekly' older than n-days|
  |MONTHLY_RETENTION:|230|Delete blobs with retentionClass='monthly' older than n-days|
  |YEARLY_RETENTION:|420|Delete blobs with retentionClass='yearly' older than n-days|
  |GITHUB_TOKEN:|null|Required: Github Personal Access Token|
  |STORAGE_KEY:|null|Required: Azure StorageAccount ConnectionString|

## (TODO: Waiting for Blob Idex Tags Preview Feature)

LifeCycleManagement is configured to delete old blobs following these rules;

- Blobs tagged with `retentionClass='weekly'` will be deleted after __60__ days
- Blobs tagged with `retentionClass='monthly'` will be deleted after __230__ days
- Blobs tagged with `retentionClass='yearly'` will be deleted after __420__ days
  
## Notes

- Github Migration archives are automatically deleted after seven days
- Some timings overview
  - Approx. 30min to migrate 100 repositories
  - 100 repositories with attachements ~= 6GB
