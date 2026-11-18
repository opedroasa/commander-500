# Imagem da Api (Render: runtime Docker, Dockerfile ./Dockerfile, contexto = raiz do repositório)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS compilacao
WORKDIR /src
COPY Commander500.sln ./
# Copia primeiro só os .csproj para o "restore" ficar em cache enquanto os pacotes não mudam.
COPY src/Commander500.Dominio/Commander500.Dominio.csproj src/Commander500.Dominio/
COPY src/Commander500.Aplicacao/Commander500.Aplicacao.csproj src/Commander500.Aplicacao/
COPY src/Commander500.Infraestrutura/Commander500.Infraestrutura.csproj src/Commander500.Infraestrutura/
COPY src/Commander500.Api/Commander500.Api.csproj src/Commander500.Api/
RUN dotnet restore src/Commander500.Api/Commander500.Api.csproj
COPY src/ src/
RUN dotnet publish src/Commander500.Api/Commander500.Api.csproj -c Release -o /app/publicacao --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS execucao
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
COPY --from=compilacao /app/publicacao .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Commander500.Api.dll"]
