# API image (Render: Docker runtime, Dockerfile path ./Dockerfile, context = repo root)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Commander500.sln ./
COPY src/Commander500.Api/Commander500.Api.csproj src/Commander500.Api/
RUN dotnet restore src/Commander500.Api/Commander500.Api.csproj
COPY src/ src/
RUN dotnet publish src/Commander500.Api/Commander500.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
COPY --from=build /app/publish .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Commander500.Api.dll"]
