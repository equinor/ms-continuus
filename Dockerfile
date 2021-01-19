FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

WORKDIR /app
COPY *.csproj ./
RUN dotnet restore

COPY src LICENSE README.md ./
RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/runtime:5.0 AS run
LABEL org.opencontainers.image.source https://github.com/equinor/ms-continuus
WORKDIR /app

COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "ms-continuus.dll"]
