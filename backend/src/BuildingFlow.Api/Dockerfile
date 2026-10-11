FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY backend/ ./backend/
RUN dotnet publish backend/src/BuildingFlow.Api/BuildingFlow.Api.csproj -c Release -o /app/publish
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
COPY docs/licenses/minio-dotnet-7.0.0-LICENSE.txt ./LICENSES/Minio-LICENSE.txt
COPY docs/licenses/rabbitmq-client-7.2.1-LICENSE-APACHE2.txt ./LICENSES/RabbitMQ.Client-LICENSE-APACHE2.txt
COPY docs/licenses/opentelemetry-1.18.0-LICENSE.txt ./LICENSES/OpenTelemetry-LICENSE.txt
COPY docs/licenses/opentelemetry-1.18.0-NOTICES.txt ./LICENSES/OpenTelemetry-NOTICES.txt
USER $APP_UID
ENTRYPOINT ["dotnet", "BuildingFlow.Api.dll"]
