package main

import (
	"os"
	"strconv"
)

func parseEnvString(name string, fallback string) string {
	value := os.Getenv(name)
	if len(value) == 0 {
		return fallback
	}
	return value
}

func parseEnvInt(name string, fallback int) int {
	value, err := strconv.ParseInt(os.Getenv(name), 10, 32)
	if err != nil {
		return fallback
	}
	return int(value)
}

func parseEnvFloat32(name string, fallback float32) float32 {
	value, err := strconv.ParseFloat(os.Getenv(name), 32)
	if err != nil {
		return fallback
	}
	return float32(value)
}
