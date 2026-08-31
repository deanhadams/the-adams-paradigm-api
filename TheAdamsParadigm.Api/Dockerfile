FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["TheAdamsParadigm.Api.csproj", "./"]

RUN dotnet restore "TheAdamsParadigm.Api.csproj"

COPY . .

RUN dotnet publish "TheAdamsParadigm.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TheAdamsParadigm.Api.dll"]