ARG PROJECT_NAME="SequencerService"
ARG SERVICE_FOLDER="Services"

# Build temp image with sdk
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG PROJECT_NAME
ARG SERVICE_FOLDER
RUN mkdir -p /app
RUN mkdir -p /sln
WORKDIR /sln
COPY . .
RUN dotnet restore
RUN dotnet build --no-restore -c Release
RUN dotnet publish ./$SERVICE_FOLDER/$PROJECT_NAME/$PROJECT_NAME.csproj -c Release -o /app

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
ARG PROJECT_NAME
ARG SERVICE_FOLDER
RUN mkdir -p /app
WORKDIR /app
COPY --from=build-env /app .
COPY ./$SERVICE_FOLDER/$PROJECT_NAME/log4net-docker.config ./log4net.config
RUN find . /app -name '.env.*' -type f -delete
# Datadog: Download and install/enable the Tracer
RUN apt-get update && apt-get install -y curl
RUN mkdir -p /opt/datadog \
    && mkdir -p /var/log/datadog \
    && TRACER_VERSION=$(curl -s https://api.github.com/repos/DataDog/dd-trace-dotnet/releases/latest | grep tag_name | cut -d '"' -f 4 | cut -c2-) \
    && curl -LO https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION}/datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb \
    && dpkg -i ./datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb \
    && rm ./datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb
EXPOSE 8080
ENTRYPOINT ["dotnet", "/app/SequencerService.dll"]
