import json
from copy import deepcopy
from random import randrange
from typing import List

import uvicorn
from fastapi import FastAPI, HTTPException, status
from pydantic import BaseModel
from starlette.responses import FileResponse

app = FastAPI()
class Repos(BaseModel):
    repositories: List[str]

with open("mock_data.json") as file:
    test_data = json.load(file)


@app.get("/orgs/{org}/repos")
def repos():
    return test_data["repos"]


@app.get("/orgs/{org}/migrations")
def list_migrations():
    return test_data["migrations"]





@app.post("/orgs/{org}/migrations")
def start_migration(repos: Repos):
    if repos.repositories[0] == "Repo4":
        raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY)
    print(repos.repositories[0])
    return test_data["startedMigration"][repos.repositories[0]]


@app.get("/orgs/{org}/migrations/{id}")
def migration_status(id: str):
    status = deepcopy(test_data["status"][id])
    # Add some delay to the export (not the failed ones)
    if status["state"] != "failed" and randrange(10) > 6:
        status["state"] = "exported"
    print(status)
    return status


@app.get("/orgs/{org}/migrations/{id}/archive")
async def download_archive(id: str):
    # Can be created with `head -c 5M </dev/urandom > archive1`
    return FileResponse("archive1")


if __name__ == "__main__":
    uvicorn.run("mock_gh_api:app", host="0.0.0.0", port=5000, reload=True)
