package main

import (
	"encoding/json"
	"testing"

	"github.com/TylerStein/galaxy-sandbox-online/internal/idpool"
	"github.com/TylerStein/galaxy-sandbox-online/internal/sim"
	"github.com/go-gl/mathgl/mgl32"
)

func TestSimulationAddBody(t *testing.T) {
	state := sim.CreateEmptySimulationState(2, 1, 10, 100)
	body := sim.BodyData{
		P: mgl32.Vec2{0, 0},
		V: mgl32.Vec2{0, 0},
		M: 1,
		R: 1,
		C: "#FFFFFF",
		T: "default",
	}

	bodyJson, err := json.Marshal(body)
	if err != nil {
		t.Fatalf("Error trying to marshal BodyJson: %v", err)
	}

	handleSimulationStateInput(state, bodyJson)

	if len(state.Bodies) != 1 {
		t.Fatalf("Simulation BodyData list has %v len, expected %v len", len(state.Bodies), 1)
	}

	expectedId := idpool.NewID(0)
	if state.Bodies[0].I != expectedId {
		t.Errorf("Simulation BodyData 0 has ID %v, expected %v", state.Bodies[0].I, expectedId)
	}
}

func TestSimulationReadUpdate(t *testing.T) {
	state := sim.CreateEmptySimulationState(2, 1, 10, 100)
	body := sim.BodyData{
		P: mgl32.Vec2{0, 0},
		V: mgl32.Vec2{0, 0},
		M: 1,
		R: 1,
		C: "#FFFFFF",
		T: "default",
	}
	sim.AddSimulationBody(state, body)

	output := make(chan []byte)
	go readSimulationStateUpdate(state, output)

	updatedBodies := sim.BodyDataList{}
	data := <-output

	err := json.Unmarshal(data, &updatedBodies)
	if err != nil {
		t.Fatalf("Error trying to unmarshal BodyJson list: %v", err)
	}

	if state.Bodies[0].I != updatedBodies.D[0].I {
		t.Errorf("Updated BodyData list 0 has ID %v, expected %v", updatedBodies.D[0].I, state.Bodies[0].I)
	}
}
