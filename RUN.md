# Run

## Running dotnet services locally

```
sh buildDotnetServices.sh Release
sh testAllDotnetLocally.sh
```

```
NB_PROCESSORS=10
NB_SEQUENCERS=3
NB_MESSAGES=300
CREATION_DELAY_MS=50
PROCESSING_DELAY_MS=500
```
Stats from 201 to 300
```json
{"start":201,"count":100,"stats":{"createdToProcessingStats":{"50p":3,"90p":4,"95p":5,"99p":5.01,"avg":3.03,"min":1,"max":6},"processingToProcessedStats":{"50p":503,"90p":507,"95p":508.05,"99p":510.03,"avg":503.59,"min":499,"max":513},"processedToSequencingStats":{"50p":3,"90p":4.1,"95p":5,"99p":6.03,"avg":3.57,"min":1,"max":9},"sequencingToSavedStats":{"50p":0,"90p":1,"95p":1,"99p":1,"avg":0.21,"min":0,"max":1},"savedToSequencedStats":{"50p":0,"90p":0,"95p":0,"99p":0.01,"avg":0.01,"min":0,"max":1},"processingToSequencedStats":{"50p":507,"90p":511,"95p":513,"99p":515.02,"avg":507.38,"min":502,"max":517},"createdToSequencedStats":{"50p":510,"90p":514,"95p":515.05,"99p":518.02,"avg":510.41,"min":504,"max":520},"maxCreatedToProcessingSeq":{"max":6,"seq":275},"maxProcessingToProcessedSeq":{"max":513,"seq":282},"maxProcessedToSequencingSeq":{"max":9,"seq":205},"maxSequencingToSavedSeq":{"max":1,"seq":204},"maxSavedToSequencedSeq":{"max":1,"seq":216},"maxCreatedToSequencedSeq":{"max":520,"seq":282}},"check":{"firstSeq":201,"lastSeq":300,"isOrdered":true,"brokenAfter":null,"brokenSeq":null,"ordered":[201,202,203,204,205,206,207,208,209,210,211,212,213,214,215,216,217,218,219,220,221,222,223,224,225,226,227,228,229,230,231,232,233,234,235,236,237,238,239,240,241,242,243,244,245,246,247,248,249,250,251,252,253,254,255,256,257,258,259,260,261,262,263,264,265,266,267,268,269,270,271,272,273,274,275,276,277,278,279,280,281,282,283,284,285,286,287,288,289,290,291,292,293,294,295,296,297,298,299,300],"others":[]}}
```

```
NB_PROCESSORS=10
NB_SEQUENCERS=3
NB_MESSAGES=300
CREATION_DELAY_MS=0
PROCESSING_DELAY_MS=500
```
Stats from 201 to 300
```json
{"start":201,"count":100,"stats":{"createdToProcessingStats":{"50p":12558,"90p":14404.8,"95p":14847.3,"99p":14859.06,"avg":12564.04,"min":10233,"max":14865},"processingToProcessedStats":{"50p":504,"90p":508,"95p":510.05,"99p":520,"avg":504.64,"min":499,"max":520},"processedToSequencingStats":{"50p":6,"90p":11.1,"95p":12.05,"99p":17.01,"avg":7.04,"min":1,"max":18},"sequencingToSavedStats":{"50p":0,"90p":1,"95p":1,"99p":4.02,"avg":0.29,"min":0,"max":6},"savedToSequencedStats":{"50p":0,"90p":0,"95p":0,"99p":0,"avg":0,"min":0,"max":0},"processingToSequencedStats":{"50p":511,"90p":517,"95p":521,"99p":528,"avg":511.97,"min":503,"max":528},"createdToSequencedStats":{"50p":13072,"90p":14912.1,"95p":15364.2,"99p":15368.09,"avg":13076.01,"min":10743,"max":15377},"maxCreatedToProcessingSeq":{"max":14865,"seq":300},"maxProcessingToProcessedSeq":{"max":520,"seq":211},"maxProcessedToSequencingSeq":{"max":18,"seq":245},"maxSequencingToSavedSeq":{"max":6,"seq":253},"maxSavedToSequencedSeq":{"max":0,"seq":201},"maxCreatedToSequencedSeq":{"max":15377,"seq":300}},"check":{"firstSeq":201,"lastSeq":300,"isOrdered":true,"brokenAfter":null,"brokenSeq":null,"ordered":[201,202,203,204,205,206,207,208,209,210,211,212,213,214,215,216,217,218,219,220,221,222,223,224,225,226,227,228,229,230,231,232,233,234,235,236,237,238,239,240,241,242,243,244,245,246,247,248,249,250,251,252,253,254,255,256,257,258,259,260,261,262,263,264,265,266,267,268,269,270,271,272,273,274,275,276,277,278,279,280,281,282,283,284,285,286,287,288,289,290,291,292,293,294,295,296,297,298,299,300],"others":[]}}
```

## Running docker containers

```
sh buildAllDockerImages.sh
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

```
dotnet test
```
