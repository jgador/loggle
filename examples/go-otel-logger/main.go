package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"
	"path/filepath"
	"runtime"
	"time"
)

type Config struct {
	Loggle LoggleConfig `json:"loggle"`
}

type LoggleConfig struct {
	ServiceName    string              `json:"serviceName"`
	ServiceVersion string              `json:"serviceVersion"`
	Environment    string              `json:"environment"`
	OtelCollector  OtelCollectorConfig `json:"otelCollector"`
}

type OtelCollectorConfig struct {
	LogsReceiverEndpoint string `json:"logsReceiverEndpoint"`
	BearerToken          string `json:"bearerToken"`
}

func loadConfig() (*Config, error) {
	configPath, err := findConfigPath()
	if err != nil {
		return nil, err
	}

	data, err := os.ReadFile(configPath)
	if err != nil {
		return nil, fmt.Errorf("read config file %s: %w", configPath, err)
	}

	var cfg Config
	if err := json.Unmarshal(data, &cfg); err != nil {
		return nil, fmt.Errorf("parse config file %s: %w", configPath, err)
	}

	if err := validateConfig(&cfg); err != nil {
		return nil, err
	}
	return &cfg, nil
}

func findConfigPath() (string, error) {
	candidates := []string{
		"config.json",
		filepath.Join(".", "config.json"),
		filepath.Join("examples", "go-otel-logger", "config.json"),
	}

	if _, file, _, ok := runtime.Caller(0); ok {
		dir := filepath.Dir(file)
		candidates = append([]string{filepath.Join(dir, "config.json")}, candidates...)
	}

	for _, candidate := range candidates {
		if candidate == "" {
			continue
		}
		path, err := filepath.Abs(candidate)
		if err != nil {
			continue
		}
		if _, err := os.Stat(path); err == nil {
			return path, nil
		}
	}

	return "", fmt.Errorf("could not locate config.json in expected locations. Checked: %v", candidates)
}

func validateConfig(cfg *Config) error {
	missing := make([]string, 0, 4)
	if cfg.Loggle.ServiceName == "" {
		missing = append(missing, "loggle.serviceName")
	}
	if cfg.Loggle.ServiceVersion == "" {
		missing = append(missing, "loggle.serviceVersion")
	}
	if cfg.Loggle.Environment == "" {
		missing = append(missing, "loggle.environment")
	}
	if cfg.Loggle.OtelCollector.LogsReceiverEndpoint == "" {
		missing = append(missing, "loggle.otelCollector.logsReceiverEndpoint")
	}

	if len(missing) > 0 {
		return fmt.Errorf("missing required configuration value(s): %v", missing)
	}

	return nil
}

func main() {
	cfg, err := loadConfig()
	if err != nil {
		log.Fatalf("failed to load config: %v", err)
	}
	loggleCfg := cfg.Loggle

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
						attrString("service.name", loggleCfg.ServiceName),
						attrString("service.version", loggleCfg.ServiceVersion),
						attrString("deployment.environment", loggleCfg.Environment),
						attrString("logger.language", "go"),
					},
				},
				"scopeLogs": []interface{}{
					map[string]interface{}{
						"scope": map[string]interface{}{
							"name":    "loggle.go.example",
							"version": loggleCfg.ServiceVersion,
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

	req, err := http.NewRequest(http.MethodPost, loggleCfg.OtelCollector.LogsReceiverEndpoint, bytes.NewReader(data))
	if err != nil {
		log.Fatalf("failed to create request: %v", err)
	}
	req.Header.Set("Content-Type", "application/json")
	if token := loggleCfg.OtelCollector.BearerToken; token != "" {
		req.Header.Set("Authorization", "Bearer "+token)
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
