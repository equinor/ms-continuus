FROM debian:10-slim

RUN apt update -y && apt install wget -y
RUN wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update  && \
    apt-get install -y apt-transport-https && \
    apt-get update && \
    apt-get install -y dotnet-sdk-5.0

WORKDIR /app
ADD src LICENSE ms-continuus.csproj README.md ./
RUN dotnet build
CMD dotnet run
