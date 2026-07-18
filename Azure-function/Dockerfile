# ── BUILD STAGE ───────────────────────────────────────────────
# Use the official .NET 8 SDK image for compilation and publishing.
# Microsoft SDK container images contain the full SDK required to compile, build, and publish .NET applications.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first to optimize Docker layer caching.
# Only when these project files change will Docker re-run dotnet restore.
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["SatKon.EmailWorker/SatKon.EmailWorker.csproj", "SatKon.EmailWorker/"]

# Restore dependencies for the EmailWorker project.
# This restores transitive dependencies across Domain, Application, and Infrastructure too.
RUN dotnet restore "SatKon.EmailWorker/SatKon.EmailWorker.csproj"

# Copy the complete source code files.
COPY Domain/ Domain/
COPY Application/ Application/
COPY Infrastructure/ Infrastructure/
COPY SatKon.EmailWorker/ SatKon.EmailWorker/

# Build and publish only the SatKon.EmailWorker project.
# Azure Functions Isolated runtime requires pre-compiled binaries to execute in a separate process.
# We publish the project to /app/publish in Release mode, without a self-contained app host since
# the runtime container will execute it using the dotnet command.
WORKDIR "/src/SatKon.EmailWorker"
RUN dotnet publish "SatKon.EmailWorker.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ── RUNTIME STAGE ─────────────────────────────────────────────
# Use the official Azure Functions .NET Isolated runtime image.
# It includes the Azure Functions Host process which starts the container,
# binds to the appropriate ports, and delegates execution to the .NET 8 isolated worker.
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS runtime

# The Functions host looks for the published application binaries inside /home/site/wwwroot.
WORKDIR /home/site/wwwroot

# Copy the published outputs from the build stage.
COPY --from=build /app/publish .

# ── Configuration & Best Practices ────────────────────────────
# AzureWebJobsScriptRoot tells the host where the function configuration (like host.json and function metadata) resides.
# AzureFunctionsJobHost__Logging__Console__IsEnabled is recommended to enable standard output logging (e.g. for App Insights / Log Analytics).
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

# Port 80 is the default port exposed by the Azure Functions host for HTTP triggers and internal scale polling.
EXPOSE 80
