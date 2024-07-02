FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

WORKDIR /app
ADD *.csproj ./
RUN dotnet restore

ADD src LICENSE README.md ./
RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/runtime:5.0 AS run
LABEL org.opencontainers.image.source="https://github.com/equinor/ms-continuus"
WORKDIR /app

RUN groupadd -g 1000 dotnet-non-root-group
RUN useradd -u 1000 -g dotnet-non-root-group dotnet-non-root-user && chown -R 1000 /app
USER 1000

COPY --from=build /app/out .
ADD src/version /app/src/version
CMD ["dotnet", "ms-continuus.dll"]
