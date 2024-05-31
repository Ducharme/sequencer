# Resilient, Performant, and Scalable Message Processing System

## Challenge
Design and implement a message processing system that is resilient, performant, and scalable. The system should be able to handle a high volume of messages while maintaining the original sequence order of the messages.

## Flow
1. Retrieve messages one by one from an input queue
2. Process messages in parallel
3. Update the system of record for each message
4. Write messages to an output queue

## Constraints
1. Messages must be processed in their incoming order
2. The sequence order must be maintained when outgoing

## Tests
1. Start multiple services for parallel processing
2. Prepare hundreds of messages in an input queue
3. Each message has at least an unique sequence number and a small payload
4. Long processing time will be simulated by waiting for 500 ms

## Verifications
1. Order is preserved by confirming output sequence is the same as the input
2. System can recover fast for process and connection failures
3. No messages are lost or duplicated even when disruptions occur
4. Time elapsed when all messages are generated at once (burst mode / load test)
5. Time elapsed when messages are generated at a sustainable processing rate (soak test)

## Implementation choices
1. ElastiCache Serverless Redis providing 99.99% availability SLA
2. Develop clients in C# .NET Core 8.0 referencing StackExchange.Redis and Npgsql libraries
3. Deploy multiple active-active instances of a Processing service reading from an input List and writing to a Stream
4. Deploy a few master-slave instances of a Sequencing service reading from the Stream and writing to an output List
5. Implement master-slave with a distributed lock in Redis having expiry time of 1 second allowing very fast switch
6. Keep the slaves constantly ready to be master and continuously requesting ownership of the distributed lock
7. Utilize Redis Pub/Sub with channels to limit short polling requests
8. Leverage Lua scripts and transactions to ensure good performances
9. Implement idempotency in Redis for performance considerations
10. Use Redis as main system of record, although switching to PostgreSQL database is possible
11. On top of sequence number and payload, message format includes creating, processing, processed, sequencing, and sequenced datetimes to easily calculate performance statistics

## Potential improvements
1. Processing service handling multiple messages in parallel
2. Partitioning/Sharding the input queue and channels based on the message sequence number
3. Enhance security (note this is a PoC and much more is required to release to production)
4. Implement robust monitoring to identify faster the bottlenecks

## By relaxing the ordering constraint
1. Remove the Sequencing service to significantly increase performance
2. Create a Dead Letter Queue when processing fails a few time or encounter specific exceptions
