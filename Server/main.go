package main

import (
	"encoding/json"
	"fmt"
	"strconv"

	"log"
	"net/http"
	"os"
	"time"

	"github.com/TylerStein/galaxy-sandbox-online/internal/sim"
)

const DefaultMaxBodies = int64(512)
const DefaultMaxClients = int64(50)
const DefaultMaxVelocity = float64(10)
const DefaultMaxBounds = float64(100)
const DefaultGravity = float64(2)

type FrameData struct {
	D []sim.BodyData `json:"d"`
	P int            `json:"p"`
}

func rootHandler(w http.ResponseWriter, r *http.Request) {
	body := []byte("OK")
	_, err := w.Write(body)
	if err != nil {
		fmt.Println(err)
		w.WriteHeader(500)
		return
	}
}

func handleFrameIO(simState *sim.SimulationState, hub *Hub, updated chan uint64, input chan []byte, output chan []byte) {
	lastTick := uint64(0)
	for {
		select {
		case tick := <-updated:
			if tick != lastTick {
				buildFrameData(simState, hub, output)
			}
		case message := <-input:
			handleSimulationStateInput(simState, message)
		}
	}
}

func buildFrameData(simState *sim.SimulationState, hub *Hub, output chan []byte) {
	defer simState.Mu.Unlock()
	simState.Mu.Lock()

	bodyList := FrameData{D: simState.Bodies, P: len(hub.clients)}
	body, err := json.Marshal(bodyList)
	if err != nil {
		fmt.Println(err)
		output <- make([]byte, 0)
		return
	}

	output <- body
}

func handleSimulationStateInput(simState *sim.SimulationState, message []byte) {
	defer simState.Mu.Unlock()
	simState.Mu.Lock()

	data := sim.BodyData{}
	err := json.Unmarshal(message, &data)
	if err != nil {
		fmt.Println(err)
		return
	}

	sim.AddSimulationBody(simState, data)
}

func main() {
	port := os.Getenv("PORT")
	maxBodies, err := strconv.ParseInt(os.Getenv("MAX_BODIES"), 10, 32)
	if err != nil {
		maxBodies = DefaultMaxBodies
	}

	maxClients, err := strconv.ParseInt(os.Getenv("MAX_CLIENTS"), 10, 32)
	if err != nil {
		maxClients = DefaultMaxClients
	}

	maxVelocity, err := strconv.ParseFloat(os.Getenv("MAX_VELOCITY"), 32)
	if err != nil {
		maxVelocity = DefaultMaxVelocity
	}

	maxBounds, err := strconv.ParseFloat(os.Getenv("MAX_BOUNDS"), 32)
	if err != nil {
		maxBounds = DefaultMaxBounds
	}

	gravity, err := strconv.ParseFloat(os.Getenv("GRAVITY"), 32)
	if err != nil {
		gravity = DefaultGravity
	}

	var quit = make(chan bool)
	var simState = sim.CreateEmptySimulationState(int(maxBodies), float32(gravity), float32(maxVelocity), float32(maxBounds))

	fmt.Println("Starting server")

	if port == "" {
		port = "8080"
	}

	outgoing := make(chan []byte)
	incoming := make(chan []byte)
	hub := newHub(outgoing, incoming)
	go hub.run()

	updated := make(chan uint64)
	go handleFrameIO(simState, hub, updated, incoming, outgoing)
	go sim.StartSimulation(simState, 16*time.Millisecond, quit, updated)

	http.HandleFunc("/ws", func(w http.ResponseWriter, r *http.Request) {
		if len(hub.clients) > int(maxClients) {
			w.WriteHeader(404)
		}

		serveWs(hub, w, r)
	})

	http.HandleFunc("/", rootHandler)

	fmt.Printf("Listening on port %v\n", port)
	if err := http.ListenAndServe(":"+port, nil); err != nil {
		log.Fatal(err)
	}

	fmt.Println("Closing server")
}
