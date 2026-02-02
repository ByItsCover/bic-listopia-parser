# Build Stage

FROM --platform=${BUILDPLATFORM} mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH

WORKDIR /build_dir
COPY *.sln .
COPY src/ListopiaParser/ListopiaParser.csproj ./src/ListopiaParser/
COPY tests/ListopiaParser.Tests/ListopiaParser.Tests.csproj ./tests/ListopiaParser.Tests/

RUN dotnet restore -a ${TARGETARCH}

COPY src/ListopiaParser/ ./src/ListopiaParser/
COPY tests/ListopiaParser.Tests/ ./tests/ListopiaParser.Tests/

# Publish Stage

FROM build AS publish

RUN dotnet publish "src/ListopiaParser/ListopiaParser.csproj" -c Release -o /publish -a ${TARGETARCH}

# Deploy Stage

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS deploy

WORKDIR /app
COPY --from=publish /publish .

ENTRYPOINT ["dotnet", "ListopiaParser.dll"]
