# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /workspace
COPY --from=buildingblocks / /workspace/fcg-dotnet-building-blocks
COPY . /workspace/fcg-identity-service
WORKDIR /workspace/fcg-identity-service
RUN --mount=type=secret,id=nuget_config,target=/root/.nuget/NuGet/NuGet.Config,required=true \
    dotnet restore FiapCloudGames.Identity.slnx -p:UseLocalBuildingBlocks=true --configfile /root/.nuget/NuGet/NuGet.Config
RUN dotnet publish src/FiapCloudGames.Identity.Api/FiapCloudGames.Identity.Api.csproj -c Release -p:UseLocalBuildingBlocks=true --no-restore -o /app/publish
RUN dotnet publish src/FiapCloudGames.Identity.Migrations/FiapCloudGames.Identity.Migrations.csproj -c Release -p:UseLocalBuildingBlocks=true --no-restore -o /app/migrations

FROM runtime AS migrations
WORKDIR /app
COPY --from=build /app/migrations .
ENTRYPOINT ["dotnet", "FiapCloudGames.Identity.Migrations.dll"]

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FiapCloudGames.Identity.Api.dll"]
