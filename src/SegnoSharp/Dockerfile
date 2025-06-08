#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG SEGNOSHARP_SEMVER="0.0.1"
ARG SEGNOSHARP_SEMVER_FULL="0.0.1-dev"

# Build expects DLL in /lib, but this folder also contains all "lib" content from the base image
# Therefore create another clean folder under "lib" and copy it there as well. This is the folder used in the steps below.
WORKDIR /lib
COPY lib .
WORKDIR /lib/segnosharp
COPY lib .
WORKDIR /src
COPY src .

WORKDIR "/src/SegnoSharp"
RUN dotnet publish "SegnoSharp.csproj" /p:PackageVersion=${SEGNOSHARP_SEMVER_FULL} /p:Version=${SEGNOSHARP_SEMVER} /p:InformationalVersion=${SEGNOSHARP_SEMVER_FULL} -c Release -o /app/publish

WORKDIR "/src/Modules"
RUN find . -type f -name '*.csproj' -printf "dotnet publish %p /p:PackageVersion=${SEGNOSHARP_SEMVER_FULL} /p:Version=${SEGNOSHARP_SEMVER} /p:InformationalVersion=${SEGNOSHARP_SEMVER_FULL} -c Release -o /app/publish/modules/%h\n" | sh

FROM base AS final
LABEL org.opencontainers.image.title=SegnoSharp
LABEL org.opencontainers.image.description="SegnoSharp is a simple audio streaming tool that can randomly stream audio files to Shoutcast/Icecast servers."
LABEL org.opencontainers.image.licenses=MIT
LABEL org.opencontainers.image.vendor=Whitestone
LABEL org.opencontainers.image.source=https://github.com/whitestone-no/segnosharp
LABEL org.opencontainers.image.documentation=https://segnosharp.readthedocs.io
LABEL org.opencontainers.image.url=https://segnosharp.whitestone.no
ENV SegnoSharp_SiteConfig__DataPath=/var/segnosharp
ENV SegnoSharp_SiteConfig__LibPath=/lib/segnosharp
ENV SegnoSharp_SiteConfig__MusicPath=/var/music
ENV SegnoSharp_Serilog__WriteTo__1__Args__path=$SegnoSharp_SiteConfig__DataPath/logs/segnosharp.log
# Ensure that the data and music folders exists in case users forget to map them as volumes
WORKDIR /var/segnosharp
WORKDIR /var/music
WORKDIR /lib/segnosharp
COPY --from=build /lib/segnosharp .
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Whitestone.SegnoSharp.dll"]