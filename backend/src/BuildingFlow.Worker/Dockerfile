FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY backend/ ./backend/
RUN dotnet publish backend/src/BuildingFlow.Worker/BuildingFlow.Worker.csproj -c Release -o /app/publish
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
COPY docs/licenses/aws-sdk-Apache-2.0.txt ./LICENSES/AWS-SDK-LICENSE.txt
COPY docs/licenses/aws-sdk-NOTICE.txt ./LICENSES/AWS-SDK-NOTICE.txt
COPY docs/licenses/rabbitmq-client-7.2.1-LICENSE-APACHE2.txt ./LICENSES/RabbitMQ.Client-LICENSE-APACHE2.txt
COPY docs/licenses/opentelemetry-1.18.0-LICENSE.txt ./LICENSES/OpenTelemetry-LICENSE.txt
COPY docs/licenses/opentelemetry-1.18.0-NOTICES.txt ./LICENSES/OpenTelemetry-NOTICES.txt
USER $APP_UID
ENTRYPOINT ["dotnet", "BuildingFlow.Worker.dll"]
