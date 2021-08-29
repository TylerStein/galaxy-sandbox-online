package main

import (
	"bytes"
	"encoding/binary"
	"fmt"

	"log"
	"net/http"
	"time"

	"github.com/TylerStein/galaxy-sandbox-online/internal/sim"
)

const DefaultMaxBodies = int64(512)
const DefaultMaxClients = int64(50)
const DefaultMaxVelocity = float64(50)
const DefaultMaxBounds = float64(100)
const DefaultGravity = float64(5)
const DefaultTimescale = float64(3.75)
const DefaultMassScale = float64(4)

type FrameData struct {
	P int            `json:"p"`
	D []sim.BodyData `json:"d"`
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

	buffer := new(bytes.Buffer)
	clients := uint16(len(hub.clients))
	err := binary.Write(buffer, binary.LittleEndian, clients)
	if err != nil {
		fmt.Println(err)
		output <- make([]byte, 0)
		return
	}

	for _, body := range simState.Bodies {
		bodyBytes, err := body.Pack()
		if err != nil {
			fmt.Println(err)
			output <- make([]byte, 0)
			return
		}

		err = binary.Write(buffer, binary.LittleEndian, bodyBytes)
		if err != nil {
			fmt.Println(err)
			output <- make([]byte, 0)
			return
		}
	}

	// bodyList := FrameData{D: simState.Bodies, P: len(hub.clients)}
	// body, err := json.Marshal(bodyList)
	// if err != nil {
	// 	fmt.Println(err)
	// 	output <- make([]byte, 0)
	// 	return
	// }

	output <- buffer.Bytes()
}

func handleSimulationStateInput(simState *sim.SimulationState, message []byte) {
	defer simState.Mu.Unlock()
	simState.Mu.Lock()

	data := sim.BodyData{}
	err := sim.UnpackBodyData(message, &data)
	if err != nil {
		fmt.Println(err)
		return
	}

	// err := json.Unmarshal(message, &data)
	// if err != nil {
	// 	fmt.Println(err)
	// 	return
	// }

	sim.AddSimulationBody(simState, data)
}

func main() {
	port := parseEnvString("PORT", "8080")
	maxBodies := parseEnvInt("MAX_BODIES", int(DefaultMaxBodies))
	maxClients := parseEnvInt("MAX_CLIENTS", int(DefaultMaxClients))
	maxVelocity := parseEnvFloat32("MAX_VELOCITY", float32(DefaultMaxVelocity))
	maxBounds := parseEnvFloat32("MAX_BOUNDS", float32(DefaultMaxBounds))
	gravity := parseEnvFloat32("GRAVITY", float32(DefaultGravity))
	timescale := parseEnvFloat32("TIME_SCALE", float32(DefaultTimescale))
	massScale := parseEnvFloat32("MASS_SCALE", float32(DefaultMassScale))

	var quit = make(chan bool)
	var simState = sim.CreateEmptySimulationState(int(maxBodies), float32(gravity), float32(timescale), float32(massScale), float32(maxVelocity), float32(maxBounds))

	fmt.Println("Starting server")

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
