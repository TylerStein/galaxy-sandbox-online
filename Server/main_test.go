package main

import (
	"bytes"
	"encoding/binary"
	"testing"

	"github.com/TylerStein/galaxy-sandbox-online/internal/idpool"
	"github.com/TylerStein/galaxy-sandbox-online/internal/sim"
	"github.com/go-gl/mathgl/mgl32"
)

func TestSimulationAddBody(t *testing.T) {
	state := sim.CreateEmptySimulationState(2, 1, 1, 10, 10, 100, 1)
	body := sim.BodyData{
		P: mgl32.Vec2{0, 0},
		V: mgl32.Vec2{0, 0},
		M: 1,
		R: 1,
		// C: "#FFFFFF",
		T: 0,
	}

	bodyBytes, err := body.Pack()
	// bodyJson, err := json.Marshal(body)
	if err != nil {
		t.Fatalf("Error trying to marshal BodyJson: %v", err)
	}

	handleSimulationStateInput(state, bodyBytes)

	if len(state.Bodies) != 1 {
		t.Fatalf("Simulation BodyData list has %v len, expected %v len", len(state.Bodies), 1)
	}

	expectedId := idpool.NewID(0)
	if state.Bodies[0].I != expectedId {
		t.Errorf("Simulation BodyData 0 has ID %v, expected %v", state.Bodies[0].I, expectedId)
	}
}

func TestSimulationReadUpdate(t *testing.T) {
	state := sim.CreateEmptySimulationState(2, 1, 1, 10, 10, 100, 1)
	body := sim.BodyData{
		P: mgl32.Vec2{123.456, -123.456},
		V: mgl32.Vec2{987.654, -987.654},
		M: 123.456,
		R: 789.123,
		// C: "#FFFFFF",
		T: 5,
	}
	sim.AddSimulationBody(state, body)
	hub := newHub(make(chan []byte), make(chan []byte))
	output := make(chan []byte)
	go buildFrameData(state, hub, output)

	frameData := FrameData{}
	data := <-output

	reader := bytes.NewReader(data)

	var count uint16
	err := binary.Read(reader, binary.LittleEndian, &count)

	if err != nil {
		t.Fatalf("Error binary reading count from packet %v\n", err)
	}

	if count != uint16(len(hub.clients)) {
		t.Fatalf("Packet player count is %v, expected %v", count, len(hub.clients))
	}

	bodyCount := (len(data) - 2) / sim.BodyPacketBytes
	bodyList := make([]sim.BodyPacket, bodyCount)
	err = binary.Read(reader, binary.LittleEndian, &bodyList)
	if err != nil {
		t.Fatalf("Error binary reading bodies from packet %v\n", err)
	}

	if len(state.Bodies) != len(bodyList) {
		t.Fatalf("Updated BodyData list has len %v, expected %v", len(bodyList), len(state.Bodies))
	}

	if state.Bodies[0].I != bodyList[0].I {
		t.Errorf("Updated BodyData list 0 has ID %v, expected %v", frameData.D[0].I, state.Bodies[0].I)
	}
}
