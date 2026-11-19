# Imagem da Api. O contexto do build é a pasta src/ (é a "raiz" do serviço "api" no vercel.json).
# Usada pela Vercel (serviço "api"), pelo docker-compose (perfil "api") e, se um dia precisar, pelo Render (render.yaml).
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS compilacao
WORKDIR /src
# Copia primeiro só os .csproj para o "restore" ficar em cache enquanto os pacotes não mudam.
COPY Commander500.Dominio/Commander500.Dominio.csproj Commander500.Dominio/
COPY Commander500.Aplicacao/Commander500.Aplicacao.csproj Commander500.Aplicacao/
COPY Commander500.Infraestrutura/Commander500.Infraestrutura.csproj Commander500.Infraestrutura/
COPY Commander500.Api/Commander500.Api.csproj Commander500.Api/
RUN dotnet restore Commander500.Api/Commander500.Api.csproj
COPY . .
RUN dotnet publish Commander500.Api/Commander500.Api.csproj -c Release -o /app/publicacao --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS execucao
WORKDIR /app
# A Api escuta na porta da variável PORT (Program.cs). 8080 é o padrão aqui; na Vercel, defina PORT=8080 no projeto.
ENV PORT=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
COPY --from=compilacao /app/publicacao .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Commander500.Api.dll"]
