# Kafka Setup via WSL

```
sudo apt update && sudo apt upgrade -y
java -version
sudo apt install openjdk-17-jdk -y
wget https://downloads.apache.org/kafka/3.9.0/kafka_2.13-3.9.0.tgz
tar -xzf kafka_2.13-3.9.0.tgz
mv kafka_2.13-3.9.0 kafka
cd kafka
KAFKA_CLUSTER_ID="$(bin/kafka-storage.sh random-uuid)"
bin/kafka-storage.sh format -t $KAFKA_CLUSTER_ID -c config/kraft/server.properties
bin/kafka-server-start.sh config/kraft/server.properties
```

### Start ZooKeeper
```
bin/zookeeper-server-start.sh config/zookeeper.properties
```

### Produce and consume some messages

```
bin/kafka-topics.sh --create --topic demo-messages --bootstrap-server localhost:9092
bin/kafka-console-producer.sh --topic demo-messages --bootstrap-server localhost:9092
>first message
>second message
>third message
```

```
bin/kafka-console-consumer.sh --topic demo-messages --from-beginning --bootstrap-server localhost:9092
```

### Stop Kafka
1. Stop the producer and consumer clients with Ctrl+C
2. Stop the Kafka server with Ctrl+C
3. Cleanup logs: `rm -rf /tmp/kraft-combined-logs`

### After WSL Shutdown
```
cd kafka
KAFKA_CLUSTER_ID="$(bin/kafka-storage.sh random-uuid)"
bin/kafka-storage.sh format -t $KAFKA_CLUSTER_ID -c config/kraft/server.properties
bin/kafka-server-start.sh config/kraft/server.properties
bin/kafka-topics.sh --list --bootstrap-server localhost:9092
bin/kafka-topics.sh --create --topic demo-messages --bootstrap-server localhost:9092
```