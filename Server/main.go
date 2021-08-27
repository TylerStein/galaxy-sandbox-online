package main

import (
	"encoding/json"
	"fmt"

	"log"
	"net/http"
	"os"
	"time"

	"github.com/TylerStein/galaxy-sandbox-online/internal/sim"
)

const MaxBodies = int(512)
const MaxClients = int(256)

func rootHandler(w http.ResponseWriter, r *http.Request) {
	body := []byte("OK")
	_, err := w.Write(body)
	if err != nil {
		fmt.Println(err)
		w.WriteHeader(500)
		return
	}
}

func handleSimulationIO(simState *sim.SimulationState, updated chan uint64, input chan []byte, output chan []byte) {
	lastTick := uint64(0)
	for {
		select {
		case tick := <-updated:
			if tick != lastTick {
				readSimulationStateUpdate(simState, output)
			}
		case message := <-input:
			handleSimulationStateInput(simState, message)
		}
	}
}

func readSimulationStateUpdate(simState *sim.SimulationState, output chan []byte) {
	defer simState.Mu.Unlock()
	simState.Mu.Lock()

	body, err := json.Marshal(simState.Bodies)
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
	var quit = make(chan bool)
	var simState = sim.CreateEmptySimulationState(MaxBodies, 1, 100, 1000)

	fmt.Println("Starting server")

	port := os.Getenv("PORT")
	if port == "" {
		port = "8080"
	}

	outgoing := make(chan []byte)
	incoming := make(chan []byte)
	hub := newHub(outgoing, incoming)
	go hub.run()

	updated := make(chan uint64)
	go handleSimulationIO(simState, updated, incoming, outgoing)
	go sim.StartSimulation(simState, 16*time.Millisecond, quit, updated)

	http.HandleFunc("/ws", func(w http.ResponseWriter, r *http.Request) {
		serveWs(hub, w, r)
	})

	http.HandleFunc("/", rootHandler)

	fmt.Printf("Listening on port %v\n", port)
	if err := http.ListenAndServe(":"+port, nil); err != nil {
		log.Fatal(err)
	}

	fmt.Println("Closing server")
}
