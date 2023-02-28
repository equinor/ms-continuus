FROM python:3.10
WORKDIR /app
ADD . /app
RUN pip install poetry
RUN poetry config virtualenvs.create false
COPY pyproject.toml poetry.lock ./
RUN poetry install
