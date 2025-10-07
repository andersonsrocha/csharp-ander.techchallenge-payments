FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar o arquivo da solução
COPY TechChallengePayments.sln ./

# Copy project files
COPY src/TechChallengePayments.Api/TechChallengePayments.Api.csproj src/TechChallengePayments.Api/
COPY src/TechChallengePayments.Application/TechChallengePayments.Application.csproj src/TechChallengePayments.Application/
COPY src/TechChallengePayments.Data/TechChallengePayments.Data.csproj src/TechChallengePayments.Data/
COPY src/TechChallengePayments.Domain/TechChallengePayments.Domain.csproj src/TechChallengePayments.Domain/
COPY src/TechChallengePayments.Security/TechChallengePayments.Security.csproj src/TechChallengePayments.Security/
COPY src/TechChallengePayments.Elasticsearch/TechChallengePayments.Elasticsearch.csproj src/TechChallengePayments.Elasticsearch/
COPY tests/TechChallengePayments.Application.Test/TechChallengePayments.Application.Test.csproj tests/TechChallengePayments.Application.Test/

# Realizar o restore
RUN dotnet restore

# Copiar arquivos
COPY src/ src/
COPY tests/ tests/

# Construir o projeto
RUN dotnet build -c Release --no-restore

# Publicar o projeto
RUN dotnet publish src/TechChallengePayments.Api/TechChallengePayments.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install the agent
RUN apt-get update && apt-get install -y wget ca-certificates gnupg \
&& echo 'deb http://apt.newrelic.com/debian/ newrelic non-free' | tee /etc/apt/sources.list.d/newrelic.list \
&& wget https://download.newrelic.com/548C16BF.gpg \
&& apt-key add 548C16BF.gpg \
&& apt-get update \
&& apt-get install -y 'newrelic-dotnet-agent' \
&& rm -rf /var/lib/apt/lists/*

# Enable the agent
ENV CORECLR_ENABLE_PROFILING=1 \
CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
NEW_RELIC_APP_NAME="techchallenge-payments-newrelic"

WORKDIR /app

# Criar non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copiar os arquivos publicados
COPY --from=build /app/publish .

# Trocar ownership para non-root user
RUN chown -R appuser:appuser /app
USER appuser

# Expor a porta
EXPOSE 8080

ENTRYPOINT ["dotnet", "TechChallengePayments.Api.dll"]
