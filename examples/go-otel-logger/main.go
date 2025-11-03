package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"
	"time"
)

func main() {
	endpoint := getenv("LOGGLE_OTLP_ENDPOINT", "http://localhost:4318/v1/logs")
	bearer := getenv("LOGGLE_BEARER_TOKEN", "")
	serviceName := getenv("LOGGLE_SERVICE_NAME", "loggle-go-example")
	serviceVersion := getenv("LOGGLE_SERVICE_VERSION", "0.1.0")
	environment := getenv("LOGGLE_ENVIRONMENT", "local")

	logRecords := make([]map[string]interface{}, 0, 6)
	for i := 0; i < 5; i++ {
		logRecords = append(logRecords, newLogRecord(
			fmt.Sprintf("Go log iteration %d", i),
			"INFO",
			9,
			map[string]interface{}{
				"log.iteration":    i,
				"logger.language":  "go",
				"logger.code_path": "examples/go-otel-logger/main.go",
			},
		))
	}

	logRecords = append(logRecords, newLogRecord(
		"Go logging sample completed",
		"WARN",
		13,
		map[string]interface{}{
			"app.status": "complete",
		},
	))

	payload := map[string]interface{}{
		"resourceLogs": []interface{}{
			map[string]interface{}{
				"resource": map[string]interface{}{
					"attributes": []interface{}{
						attrString("service.name", serviceName),
						attrString("service.version", serviceVersion),
						attrString("deployment.environment", environment),
						attrString("logger.language", "go"),
					},
				},
				"scopeLogs": []interface{}{
					map[string]interface{}{
						"scope": map[string]interface{}{
							"name":    "loggle.go.example",
							"version": serviceVersion,
						},
						"logRecords": logRecords,
					},
				},
			},
		},
	}

	data, err := json.Marshal(payload)
	if err != nil {
		log.Fatalf("failed to marshal OTLP payload: %v", err)
	}

	req, err := http.NewRequest(http.MethodPost, endpoint, bytes.NewReader(data))
	if err != nil {
		log.Fatalf("failed to create request: %v", err)
	}
	req.Header.Set("Content-Type", "application/json")
	if bearer != "" {
		req.Header.Set("Authorization", "Bearer "+bearer)
	}

	client := &http.Client{Timeout: 10 * time.Second}
	resp, err := client.Do(req)
	if err != nil {
		log.Fatalf("failed to send log payload: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode >= 300 {
		log.Fatalf("collector returned status %d", resp.StatusCode)
	}

	log.Println("Go logs sent successfully")
}

func attrString(key, value string) map[string]interface{} {
	return map[string]interface{}{
		"key": key,
		"value": map[string]interface{}{
			"stringValue": value,
		},
	}
}

func attrAny(key string, value interface{}) map[string]interface{} {
	val := map[string]interface{}{}
	switch v := value.(type) {
	case int:
		val["intValue"] = v
	case int64:
		val["intValue"] = v
	case float64:
		val["doubleValue"] = v
	case bool:
		val["boolValue"] = v
	default:
		val["stringValue"] = fmt.Sprintf("%v", v)
	}
	return map[string]interface{}{
		"key":   key,
		"value": val,
	}
}

func newLogRecord(message, severityText string, severityNumber int, attributes map[string]interface{}) map[string]interface{} {
	now := time.Now().UnixNano()
	attrList := make([]interface{}, 0, len(attributes))
	for k, v := range attributes {
		attrList = append(attrList, attrAny(k, v))
	}

	return map[string]interface{}{
		"timeUnixNano":         now,
		"observedTimeUnixNano": now,
		"severityNumber":       severityNumber,
		"severityText":         severityText,
		"body": map[string]interface{}{
			"stringValue": message,
		},
		"attributes": attrList,
	}
}

func getenv(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}
