package main

import (
	"fmt"
	"testing"
	"time"

	"github.com/go-gl/mathgl/mgl32"
)

func TestClampMagnitude(t *testing.T) {
	p := float32(0.00001)

	v := mgl32.Vec2{100, 0}
	v = clampVectorMagnitude(v, 10)
	if float32(float64(v.Len())-10.0) > p {
		t.Errorf("Vector magnitude is %f, expected %f within %f prescision", v.Len(), 10.0, p)
	}

	v = mgl32.Vec2{0, 50}
	v = clampVectorMagnitude(v, 100)
	if float32(float64(v.Len())-50.0) > p {
		t.Errorf("Vector magnitude is %f, expected %f within %f prescision", v.Len(), 50.0, p)
	}
}

func TestIdPoolInitialize(t *testing.T) {
	idPool := NewIDPool(3, 1)
	id := "0000000000000000"
	if idPool.pool[0] != id {
		t.Errorf("IDPool first value is %v, expected %v", idPool.pool[0], id)
	}

	id = "0000000000000001"
	if idPool.pool[1] != id {
		t.Errorf("IDPool second value is %v, expected %v", idPool.pool[1], id)
	}

	id = "0000000000000002"
	if idPool.pool[2] != id {
		t.Errorf("IDPool third value is %v, expected %v", idPool.pool[2], id)
	}
}

func TestIdPoolGet(t *testing.T) {
	idPool := NewIDPool(3, 1)
	expectedId := "0000000000000000"
	id := idPool.DequeueId()

	if id != expectedId {
		t.Errorf("IDPool first Dequeue value is %v, expected %v", id, expectedId)
	}

	expectedId = "0000000000000001"
	id = idPool.DequeueId()

	if id != expectedId {
		t.Errorf("IDPool second Dequeue value is %v, expected %v", id, expectedId)
	}

	expectedId = "0000000000000002"
	id = idPool.DequeueId()

	if id != expectedId {
		t.Errorf("IDPool third Dequeue value is %v, expected %v", id, expectedId)
	}
}

func TestIdPoolRecycle(t *testing.T) {
	idPool := NewIDPool(3, 1)
	id := idPool.DequeueId()
	poolLen := len(idPool.pool)
	if poolLen != 2 {
		t.Errorf("IDPool first Dequeue len is %d, expected %d", poolLen, 2)
	}

	expectedId := "0000000000000000"
	if id != expectedId {
		t.Errorf("IDPool first Dequeue value is %v, expected %v", id, expectedId)
	}

	idPool.EnqueueId(id)
	poolLen = len(idPool.pool)
	if poolLen != 3 {
		t.Errorf("IDPool first Push len is %d, expected %d", poolLen, 3)
	}
}

func TestIdPoolStep(t *testing.T) {
	idPool := NewIDPool(1, 1)
	id := idPool.DequeueId()
	expectedId := "0000000000000000"
	if id != expectedId {
		t.Errorf("IDPool first Dequeue value is %v, expected %v", id, expectedId)
	}

	id = idPool.DequeueId()
	expectedId = "0000000000000001"
	if id != expectedId {
		t.Errorf("IDPool second Dequeue value is %v, expected %v", id, expectedId)
	}

	id = idPool.DequeueId()
	expectedId = "0000000000000002"
	if id != expectedId {
		t.Errorf("IDPool third Dequeue value is %v, expected %v", id, expectedId)
	}
}

func TestUpdatePhysicsBodiesVelocity(t *testing.T) {
	deltaTime := float32(1.0)
	idPool := make([]string, 0)
	bodies := make([]BodyData, 1)
	bodies[0] = BodyData{
		i: "0000000000000000",
		p: mgl32.Vec2{0, 0},
		v: mgl32.Vec2{0, 0},
		m: 1,
		r: 1,
		c: "#000000",
		t: "default",
	}

	simState := &SimulationState{
		gravityConstant: 1,
		maxVelocity:     1,
		bounds:          10,
		bodies:          bodies,
		idPool:          idPool,
	}

	UpdatePhysicsBodies(simState, deltaTime)

	if simState.bodies[0].p.X() != 0 {
		t.Errorf("Body X is %f, expected %f", simState.bodies[0].p.X(), 0.0)
	}

	if simState.bodies[0].p.Y() != 0 {
		t.Errorf("Body Y is %f, expected %f", simState.bodies[0].p.Y(), 0.0)
	}

	simState.bodies[0].v = simState.bodies[0].v.Add(mgl32.Vec2{1, 1})

	UpdatePhysicsBodies(simState, deltaTime)

	if simState.bodies[0].p.X() != 1 {
		t.Errorf("Body X is %f, expected %f", simState.bodies[0].p.X(), 1.0)
	}

	if simState.bodies[0].p.Y() != 1 {
		t.Errorf("Body Y is %f, expected %f", simState.bodies[0].p.Y(), 1.0)
	}
}

func TestUpdatePhysicsBodiesGravity(t *testing.T) {
	deltaTime := float32(1.0)
	idPool := make([]string, 0)
	bodies := make([]BodyData, 2)
	bodies[0] = BodyData{
		i: "0000000000000000",
		p: mgl32.Vec2{0, 0},
		v: mgl32.Vec2{0, 0},
		m: 1,
		r: 1,
		c: "#000000",
		t: "default",
	}
	bodies[1] = BodyData{
		i: "0000000000000001",
		p: mgl32.Vec2{2, 0},
		v: mgl32.Vec2{0, 0},
		m: 1,
		r: 1,
		c: "#000000",
		t: "default",
	}

	simState := &SimulationState{
		gravityConstant: 1,
		maxVelocity:     1,
		bounds:          10,
		bodies:          bodies,
		idPool:          idPool,
	}

	UpdatePhysicsBodies(simState, deltaTime)

	if simState.bodies[0].p.X() <= 0 {
		t.Errorf("Body[0] X is %f, expected > %f", simState.bodies[0].p.X(), 0.0)
	}

	if simState.bodies[0].p.Y() != 0 {
		t.Errorf("Body[0] Y is %f, expected %f", simState.bodies[0].p.Y(), 0.0)
	}

	if simState.bodies[1].p.X() >= 2 {
		t.Errorf("Body[1] X is %f, expected < %f", simState.bodies[0].p.X(), 2.0)
	}

	if simState.bodies[1].p.Y() != 0 {
		t.Errorf("Body[1] Y is %f, expected %f", simState.bodies[0].p.Y(), 0.0)
	}

}

func benchmarkUpdateNPhysicsBodies(n int, b *testing.B) {
	b.StopTimer()
	deltaTime := float32((16 * time.Millisecond).Seconds())
	idPool := make([]string, 0)
	bodies := make([]BodyData, n)

	pX := float32(-n * 2)
	pY := float32(-n * 2)
	vX := float32(-1.0)
	vY := float32(1.0)

	for i := 0; i < n; i++ {
		bodies[i] = BodyData{
			i: fmt.Sprintf("%d", i),
			p: mgl32.Vec2{pX, pY},
			v: mgl32.Vec2{vX, vY},
			m: 1,
			r: 1,
			c: "#000000",
			t: "default",
		}

		pX += 2
		pY += 2
		vX *= -1
		vY *= -1
	}

	simState := &SimulationState{
		gravityConstant: 1,
		maxVelocity:     0.1,
		bounds:          float32(n * 4),
		bodies:          bodies,
		idPool:          idPool,
	}

	b.StartTimer()
	for i := 0; i < b.N; i++ {
		UpdatePhysicsBodies(simState, deltaTime)
	}
}

func BenchmarkUpdate2PhysicsBodies(b *testing.B) {
	benchmarkUpdateNPhysicsBodies(2, b)
}

func BenchmarkUpdate100PhysicsBodies(b *testing.B) {
	benchmarkUpdateNPhysicsBodies(100, b)
}

func BenchmarkUpdate1000PhysicsBodies(b *testing.B) {
	benchmarkUpdateNPhysicsBodies(1000, b)
}

func BenchmarkUpdate10000PhysicsBodies(b *testing.B) {
	benchmarkUpdateNPhysicsBodies(10000, b)
}
