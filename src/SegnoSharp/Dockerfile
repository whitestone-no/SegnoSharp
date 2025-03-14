#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG SEGNOSHARP_SEMVER="0.0.1"
ARG SEGNOSHARP_SEMVER_FULL="0.0.1-dev"

WORKDIR /data
COPY data .
WORKDIR /src
COPY src .

WORKDIR "/src/SegnoSharp"
RUN dotnet publish "SegnoSharp.csproj" /p:PackageVersion=${SEGNOSHARP_SEMVER_FULL} /p:Version=${SEGNOSHARP_SEMVER} /p:InformationalVersion=${SEGNOSHARP_SEMVER_FULL} -c Release -o /app/publish

WORKDIR "/src/Modules"
RUN find . -type f -name '*.csproj' -printf "dotnet publish %p /p:PackageVersion=${SEGNOSHARP_SEMVER_FULL} /p:Version=${SEGNOSHARP_SEMVER} /p:InformationalVersion=${SEGNOSHARP_SEMVER_FULL} -c Release -o /app/publish/modules/%h\n" | sh

FROM base AS final
LABEL org.opencontainers.image.source=https://github.com/whitestone-no/segnosharp
LABEL org.opencontainers.image.description="SegnoSharp is a simple audio streaming tool that can randomly stream audio files to Shoutcast/Icecast servers. Enjoy an engaging and unpredictable listening experience every time you hit play."
LABEL org.opencontainers.image.licenses=MIT
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Whitestone.SegnoSharp.dll"]