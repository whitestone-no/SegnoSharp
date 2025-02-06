#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
WORKDIR "/src/SegnoSharp"
RUN dotnet publish "SegnoSharp.csproj" -c Release -o /app/publish

WORKDIR "/src/Modules"
RUN find . -type f -name '*.csproj' -printf "dotnet publish %p -c Release -o /app/publish/modules/%h\n" | sh

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Whitestone.SegnoSharp.dll"]