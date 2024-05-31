ARG PROJECT_NAME="SequencerWebService"
ARG SERVICE_FOLDER="WebServices"

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
COPY ./$SERVICE_FOLDER/$PROJECT_NAME/.env.production .
EXPOSE 8080
ENTRYPOINT ["dotnet", "/app/SequencerWebService.dll"]
