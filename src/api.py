import requests

from config import Config

# For reference: https://docs.github.com/en/free-pro-team@latest/rest/reference/migrations

REPO_URL = f"https://api.github.com/orgs/{Config.ORGANIZATION}/repos/" if not Config.USER_MIGRATION else f"https://api.github.com/users/{Config.ORGANIZATION}/repos"
MIGRATIONS_URL = f"https://api.github.com/orgs/{Config.ORGANIZATION}/migrations" if not Config.USER_MIGRATION else f"https://api.github.com/user/migrations"
HEADERS = {"Accept": "application/vnd.github.wyandotte-preview+json",
           "Authorization": f"Bearer {Config.GITHUB_TOKEN}"}

# Documentation says to use this accept header on the user migration API, but that gives a header error the ORG header doesn't
# "Accept": "application/vnd.github.v3+json",


def list_repositories():
    req = requests.get(url=REPO_URL, headers=HEADERS)
    return [r["name"] for r in req.json()]


def list_migrations():
    req = requests.get(url=MIGRATIONS_URL, headers=HEADERS)
    return req.json()


def migration_status(migration_id: int):
    req = requests.get(url=f"{MIGRATIONS_URL}/{migration_id}", headers=HEADERS)
    return req.json()


def start_migration():
    req = requests.post(url=MIGRATIONS_URL, headers=HEADERS, data={"repositories": list_repositories()})
    return req.json()


def archive_url(migration_id: int):
    req = requests.get(url=f"{MIGRATIONS_URL}/{migration_id}/archive", headers=HEADERS)
    return req.json()


if __name__ == '__main__':
    start_migration()
