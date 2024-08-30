//
// Copyright 2021 The Dapr Authors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"net/url"
	"os"
	"strings"

	"github.com/gin-gonic/gin"
	"go.opentelemetry.io/contrib/instrumentation/github.com/gin-gonic/gin/otelgin"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/exporters/otlp/otlptrace"
	"go.opentelemetry.io/otel/exporters/otlp/otlptrace/otlptracehttp"
	"go.opentelemetry.io/otel/sdk/resource"
	sdktrace "go.opentelemetry.io/otel/sdk/trace"
)

var (
	serviceName      = os.Getenv("SERVICE_NAME")
	collectorURL     = os.Getenv("OTEL_EXPORTER_OTLP_ENDPOINT")
	collectorHeaders = os.Getenv("OTEL_EXPORTER_OTLP_HEADERS")
	insecure         = "true"
)

type Operands struct {
	OperandOne float32 `json:"operandOne,string"`
	OperandTwo float32 `json:"operandTwo,string"`
}

func add(c *gin.Context) {
	c.Header("Content-Type", "application/json")
	c.Header("Access-Control-Allow-Origin", "*")
	var operands Operands
	json.NewDecoder(c.Request.Body).Decode(&operands)
	fmt.Printf("Adding %f to %f\n", operands.OperandOne, operands.OperandTwo)
	c.JSON(200, operands.OperandOne+operands.OperandTwo)
}

func main() {
	shutdown := initTracer()
	defer shutdown(context.Background())

	appPort := os.Getenv("APP_PORT")
	if appPort == "" {
		appPort = "6000"
	}

	r := gin.Default()
	r.Use(otelgin.Middleware(serviceName))
	r.POST("/add", add)
	r.Run(":" + appPort)

}

func initTracer() func(context.Context) error {
	// secureOption := otlptracegrpc.WithTLSCredentials(credentials.NewClientTLSFromCert(nil, ""))
	// if len(insecure) > 0 {
	// 	secureOption = otlptracegrpc.WithInsecure()
	// }
	secureOption := otlptracehttp.WithInsecure()

	parsedURL, err := url.Parse(collectorURL)
	fmt.Println("parsedURL: ", parsedURL)
	if err != nil {
		log.Fatalf("Failed to parse collector URL: %v", err)
	}
	endpoint := parsedURL.Host // Use the entire URL for HTTP/HTTPS
	headers := strings.Split(collectorHeaders, "=")

	exporter, err := otlptrace.New(
		context.Background(),
		otlptracehttp.NewClient(
			secureOption,
			otlptracehttp.WithEndpoint(endpoint),
			otlptracehttp.WithHeaders(map[string]string{headers[0]: headers[1]}),
		),
	)

	if err != nil {
		log.Fatal(err)
	}
	resources, err := resource.New(
		context.Background(),
		resource.WithAttributes(
			attribute.String("service.name", serviceName),
			attribute.String("library.language", "go"),
		),
	)
	if err != nil {
		log.Printf("Could not set resources: ", err)
	}

	otel.SetTracerProvider(
		sdktrace.NewTracerProvider(
			sdktrace.WithSampler(sdktrace.AlwaysSample()),
			sdktrace.WithBatcher(exporter),
			sdktrace.WithResource(resources),
		),
	)
	return exporter.Shutdown
}
