# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Schedule.Core/Schedule.Core.csproj ./Schedule.Core/
COPY Schedule.Application/Schedule.Application.csproj ./Schedule.Application/
COPY Schedule.Infra/Schedule.Infra.csproj ./Schedule.Infra/
COPY Schedule/Schedule.Server/Schedule.Server.csproj ./Schedule/Schedule.Server/

RUN dotnet restore ./Schedule/Schedule.Server/Schedule.Server.csproj

COPY Schedule.Core ./Schedule.Core
COPY Schedule.Application ./Schedule.Application
COPY Schedule.Infra ./Schedule.Infra
COPY Schedule ./Schedule

# 👉 pula a parte JS/Node do publish
RUN dotnet publish ./Schedule/Schedule.Server/Schedule.Server.csproj \
    -c Release -o /app/publish \
    -p:BuildingInsideDocker=true -p:SkipJavaScriptBuild=true

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Schedule.Server.dll"]
