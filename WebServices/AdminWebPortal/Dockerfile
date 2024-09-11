ARG PROJECT_NAME="AdminWebPortal"
ARG SERVICE_FOLDER="WebServices"

# Download dd-trace-dotnet
FROM curlimages/curl:8.9.1 AS curl-dd
RUN TRACER_VERSION=$(curl -s https://api.github.com/repos/DataDog/dd-trace-dotnet/releases/latest | grep tag_name | cut -d '"' -f 4 | cut -c2-) \
    && curl -s -L -o datadog-dotnet-apm_amd64.deb https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION}/datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb

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
FROM mcr.microsoft.com/dotnet/aspnet:8.0
ARG PROJECT_NAME
ARG SERVICE_FOLDER
RUN mkdir -p /app
WORKDIR /app
COPY --from=build-env /app .
COPY ./$SERVICE_FOLDER/$PROJECT_NAME/log4net-docker.config ./log4net.config
RUN find . /app -name '.env.*' -type f -delete
# Datadog: Copy and install/enable the Tracer
COPY --from=curl-dd /home/curl_user/datadog-dotnet-apm_amd64.deb .
RUN mkdir -p /opt/datadog && mkdir -p /var/log/datadog \
    && dpkg -i ./datadog-dotnet-apm_amd64.deb \
    && rm ./datadog-dotnet-apm_amd64.deb
EXPOSE 8080
ENTRYPOINT ["dotnet", "/app/AdminWebPortal.dll"]
