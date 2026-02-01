# Stage 1: Build
FROM --platform=${BUILDPLATFORM} mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH
WORKDIR /app

# Copy the solution and the csproj to restore dependencies
# This layer is cached unless your project files change
COPY *.sln .
COPY src/ListopiaParser/ListopiaParser.csproj ./src/ListopiaParser/
COPY tests/ListopiaParser.Tests/ListopiaParser.Tests.csproj ./tests/ListopiaParser.Tests/

# Restore NuGet packages
RUN dotnet restore -a ${TARGETARCH}

# Copy the remaining source code
COPY src/ListopiaParser/ ./src/ListopiaParser/
COPY tests/ListopiaParser.Tests/ ./tests/ListopiaParser.Tests/

# Build and publish the application
# We use -c Release and output to the /publish folder
FROM build AS publish
RUN dotnet publish "src/ListopiaParser/ListopiaParser.csproj" -c Release -o /publish -a ${TARGETARCH}

# Stage 2: Runtime
FROM --platform=${TARGETPLATFORM} mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

# Copy the published output from the build stage
COPY --from=publish /publish .

# Define the entry point for the BackgroundService
ENTRYPOINT ["dotnet", "ListopiaParser.dll"]
