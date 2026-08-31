FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY the-adams-paradigm-api.sln ./
COPY TheAdamsParadigm.Api/TheAdamsParadigm.Api.csproj TheAdamsParadigm.Api/
RUN dotnet restore TheAdamsParadigm.Api/TheAdamsParadigm.Api.csproj

COPY TheAdamsParadigm.Api/ TheAdamsParadigm.Api/
RUN dotnet publish TheAdamsParadigm.Api/TheAdamsParadigm.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ASPNETCORE_URLS=http://0.0.0.0:$PORT dotnet TheAdamsParadigm.Api.dll
