FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copiar arquivos de projeto para melhor cache do Docker
COPY TechChallengePayments.sln ./
COPY src/TechChallengePayments.Api/TechChallengePayments.Api.csproj src/TechChallengePayments.Api/
COPY src/TechChallengePayments.Application/TechChallengePayments.Application.csproj src/TechChallengePayments.Application/
COPY src/TechChallengePayments.Data/TechChallengePayments.Data.csproj src/TechChallengePayments.Data/
COPY src/TechChallengePayments.Domain/TechChallengePayments.Domain.csproj src/TechChallengePayments.Domain/
COPY src/TechChallengePayments.Security/TechChallengePayments.Security.csproj src/TechChallengePayments.Security/
COPY src/TechChallengePayments.Elasticsearch/TechChallengePayments.Elasticsearch.csproj src/TechChallengePayments.Elasticsearch/
COPY tests/TechChallengePayments.Application.Test/TechChallengePayments.Application.Test.csproj tests/TechChallengePayments.Application.Test/

# Restore, copiar código fonte e build em camadas otimizadas
RUN dotnet restore

# Copiar arquivos
COPY src/ src/
COPY tests/ tests/

# Publicar o projeto
RUN dotnet publish src/TechChallengePayments.Api/TechChallengePayments.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage - usando Alpine para imagem mais leve
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# Instalar New Relic
RUN apk update && apk add --no-cache wget tar \
    && wget https://download.newrelic.com/dot_net_agent/latest_release/newrelic-dotnet-agent_amd64.tar.gz -r \
    && tar -xzf download.newrelic.com/dot_net_agent/latest_release/newrelic-dotnet-agent_amd64.tar.gz -C /usr/local \ 
    && rm -rf download.newrelic.com

# Configurações New Relic
ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
    CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
    CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
    NEW_RELIC_APP_NAME="techchallenge-payments-newrelic"

WORKDIR /app

# Criar non-root user (Alpine Linux)
RUN addgroup -S appuser && adduser -S appuser -G appuser

# Copiar os arquivos publicados
COPY --from=build /app/publish .

# Trocar ownership para non-root user
RUN chown -R appuser:appuser /app
USER appuser

# Expor a porta
EXPOSE 8080

ENTRYPOINT ["dotnet", "TechChallengePayments.Api.dll"]
