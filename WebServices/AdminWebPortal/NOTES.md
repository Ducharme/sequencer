
# Notes

## C# .NET Core

[Tutorial: Get started with ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/getting-started/?view=aspnetcore-8.0&tabs=windows)
```
dotnet new webapp --name PROJECT_NAME --language "C#"
```

[Minimal APIs quick reference](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-8.0)
```
dotnet new webapi --name PROJECT_NAME --language "C#"
dotnet dev-certs https --trust
dotnet watch run
dotnet run --launch-profile https
```

[Automate VS Code terminal](https://code.visualstudio.com/docs/terminal/basics#_automating-terminals-with-tasks)

## Requests

```
curl -s --raw --show-error --verbose -X GET http://localhost:5000/
curl -X GET http://localhost:5000/
curl -X GET http://localhost:5000/healthz
curl -X DELETE http://localhost:5000/list/sequencer/messages?name=poc
curl -X DELETE http://localhost:5000/list/sequenced/messages?name=poc
curl -X POST -H "Content-Type: application/json" -d "{\"name\":\"poc\", \"numberOfMessages\": 100, \"creationDelay\": 50, \"processingTime\": 500}" "http://localhost:5000/list/sequencer/messages"
curl -X POST -H "Content-Type: application/json" -d "{\"name\":\"poc\", \"numberOfMessages\": 100, \"creationDelay\": 50, \"processingTime\": 500}" "http://localhost:5000/messages"
curl -X DELETE "http://localhost:5000/messages?name=poc"
curl -X GET http://localhost:5000/list/pending/messages?name=poc
curl -X GET http://localhost:5000/list/processing/messages?name=poc
curl -X GET http://localhost:5000/list/processed/messages?name=poc
curl -X GET http://localhost:5000/stream/pending/messages?name=poc
curl -X GET http://localhost:5000/stream/processed/messages?name=poc
curl -X GET http://localhost:5000/list/stats?name=poc
curl -X GET http://localhost:5000/list/perfs?name=poc
#curl -X GET http://localhost:5000/database/messages?name=poc
curl -X GET http://localhost:5000/list/sequenced/count?name=poc
curl -X GET http://localhost:5000/stream/sequenced/count?name=poc
curl -X GET http://localhost:5000/stream/sequenced/last?name=poc

```

## Docker

```
docker build -f ./Dockerfile --pull --progress=plain --no-cache -t adminwebportal ../..
docker build -f ./Dockerfile --pull --progress=plain -t adminwebportal ../..
docker build -f ./Dockerfile --pull -t adminwebportal ../..
docker run -it -p 8080:8080 adminwebportal

docker network ls | grep "bridge" | awk '{print $1}'
```
