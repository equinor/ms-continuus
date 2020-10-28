import os


class Config:
    ORGANIZATION = os.getenv("ORGANIZATION", "soofstad")
    GITHUB_TOKEN = os.getenv("GITHUB_TOKEN")
    # Useful for testing purposes
    USER_MIGRATION = True