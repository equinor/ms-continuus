FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

WORKDIR /app
ADD *.csproj ./
RUN dotnet restore

ADD src LICENSE README.md ./
RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/runtime:5.0 AS run
LABEL org.opencontainers.image.source="https://github.com/equinor/ms-continuus"
WORKDIR /app

COPY --from=build /app/out .
ADD src/version /app/src/version
CMD ["dotnet", "ms-continuus.dll"]
