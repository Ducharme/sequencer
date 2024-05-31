# Run

## Running dotnet services locally

```
sh buildDotnetServices.sh Release
sh testAllDotnetLocally.sh
```

Stats from 201 to 300
```json
{"start":201,"count":100,"stats":{"createdToProcessingStats":{"50p":2,"90p":4,"95p":4,"99p":5.01,"avg":2.18,"min":0,"max":6},"processingToProcessedStats":{"50p":505,"90p":509,"95p":510.05,"99p":513.03,"avg":505.46,"min":500,"max":516},"processedToSequencingStats":{"50p":6,"90p":10,"95p":12,"99p":14.02,"avg":6.44,"min":2,"max":16},"sequencingToSavedStats":{"50p":0,"90p":1,"95p":1,"99p":3.02,"avg":0.37,"min":0,"max":5},"savedToSequencedStats":{"50p":0,"90p":0,"95p":0.05,"99p":1.03,"avg":0.08,"min":0,"max":4},"processingToSequencedStats":{"50p":512,"90p":518,"95p":520,"99p":521.03,"avg":512.35,"min":503,"max":524},"createdToSequencedStats":{"50p":514,"90p":520,"95p":522.05,"99p":525,"avg":514.53,"min":506,"max":525},"maxCreatedToProcessingSeq":{"max":6,"seq":201},"maxProcessingToProcessedSeq":{"max":516,"seq":254},"maxProcessedToSequencingSeq":{"max":16,"seq":240},"maxSequencingToSavedSeq":{"max":5,"seq":284},"maxSavedToSequencedSeq":{"max":4,"seq":277},"maxCreatedToSequencedSeq":{"max":525,"seq":234}},"check":{"firstSeq":201,"lastSeq":300,"isOrdered":true,"brokenAfter":null,"brokenSeq":null,"ordered":[201,202,203,204,205,206,207,208,209,210,211,212,213,214,215,216,217,218,219,220,221,222,223,224,225,226,227,228,229,230,231,232,233,234,235,236,237,238,239,240,241,242,243,244,245,246,247,248,249,250,251,252,253,254,255,256,257,258,259,260,261,262,263,264,265,266,267,268,269,270,271,272,273,274,275,276,277,278,279,280,281,282,283,284,285,286,287,288,289,290,291,292,293,294,295,296,297,298,299,300],"others":[]}}
```

## Running docker containers

```
sh buildDockerImages.sh
sh testAllDockerLocally.sh
```

## Testing resiliency

Run one of these in parallel to `sh testAllDotnetLocally.sh`
```
disruptor_kill_proc.sh
```
or (you can change the tool inside the script by changing CLOSE_METHOD)
```
disruptor_close_tcp.sh
```

## Running dotnet services on AWS

Refer to [AWS.md](AWS.md)

## Running unit tests


dotnet test
```
