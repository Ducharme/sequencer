
# Notes

## Requests

```
curl -s --raw --show-error --verbose -X GET http://localhost:5000/
curl -X GET http://localhost:5000/
curl -X GET http://localhost:5000/healthz
curl -X GET http://localhost:5000/healthc
curl -X GET http://localhost:5000/healthd
```

## Docker

```
docker build -f ./Dockerfile --pull --progress=plain --no-cache -t sequencerwebservice ../..
docker build -f ./Dockerfile --pull --progress=plain -t sequencerwebservice ../..
docker build -f ./Dockerfile --pull -t sequencerwebservice ../..
docker run -it -p 8082:8082 sequencerwebservice

docker network ls | grep "bridge" | awk '{print $1}'
```
