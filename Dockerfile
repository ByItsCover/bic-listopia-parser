# Build Stage

ARG DOTNET_VERSION=10.0
ARG BATCH_DIR="/publish"
ARG TARGETARCH

FROM --platform=${BUILDPLATFORM} mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

ARG TARGETARCH

WORKDIR /build_dir
COPY *.sln .
COPY src/Common/Common.csproj ./src/Common/
COPY src/HotCoverParser/HotCoverParser.csproj ./src/HotCoverParser/
COPY src/ListopiaParser/ListopiaParser.csproj ./src/ListopiaParser/
COPY src/Orchestrator/Orchestrator.csproj ./src/Orchestrator/

RUN dotnet restore src/Orchestrator/Orchestrator.csproj -a ${TARGETARCH}

COPY . .

# Publish Stage

FROM build AS publish

ARG BATCH_DIR
ARG TARGETARCH

WORKDIR /build_dir/src/Orchestrator/

RUN dotnet publish -c Release -o ${BATCH_DIR} -a ${TARGETARCH}

# Deploy Stage

FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION}-noble-chiseled AS deploy

ARG BATCH_DIR

WORKDIR /app
COPY --from=publish ${BATCH_DIR} .

ENTRYPOINT ["dotnet", "Orchestrator.dll"]
