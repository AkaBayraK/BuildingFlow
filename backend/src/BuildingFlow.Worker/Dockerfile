FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY backend/ ./backend/
RUN dotnet publish backend/src/BuildingFlow.Worker/BuildingFlow.Worker.csproj -c Release -o /app/publish
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "BuildingFlow.Worker.dll"]
