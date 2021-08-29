package sim

import (
	"testing"
	"time"

	"github.com/TylerStein/galaxy-sandbox-online/internal/idpool"
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

func TestEmptySimulationState(t *testing.T) {
	simState := CreateEmptySimulationState(10, 1, 1, 10, 100, 1000, 1)
	if simState == nil {
		t.Fatalf("SimulationState is nil, should be not nil")
	}

	if len(simState.Bodies) != 0 {
		t.Errorf("len(Bodies) is %v, expected %v", len(simState.Bodies), 0)
	}

	if cap(simState.Bodies) != 10 {
		t.Errorf("cap(Bodies) is %v, expected %v", cap(simState.Bodies), 10)
	}

	if simState.GravityConstant != 1 {
		t.Errorf("GravityConstant is %v, expected %v", simState.GravityConstant, 1)
	}

	if simState.MaxVelocity != 100 {
		t.Errorf("MaxVelocity is %v, expected %v", simState.MaxVelocity, 100)
	}

	if simState.Bounds != 1000 {
		t.Errorf("Bouns is %v, expected %v", simState.Bounds, 1000)
	}
}

func TestUpdatePhysicsBodiesVelocity(t *testing.T) {
	deltaTime := float32(1.0)
	idPool := idpool.NewIDPool(1, 1)
	bodies := make([]BodyData, 1)
	bodies[0] = BodyData{
		I: idpool.NewID(0),
		P: mgl32.Vec2{0, 0},
		V: mgl32.Vec2{0, 0},
		M: 1,
		R: 1,
		// C: "#FFFFFF",
		T: 0,
	}

	simState := &SimulationState{
		GravityConstant: 1,
		TimeScale:       1,
		MaxVelocity:     5,
		Bounds:          10,
		Bodies:          bodies,
		IdPool:          idPool,
	}

	UpdateSimulationState(simState, deltaTime)

	if simState.Bodies[0].P.X() != 0 {
		t.Errorf("Body X is %f, expected %f", simState.Bodies[0].P.X(), 0.0)
	}

	if simState.Bodies[0].P.Y() != 0 {
		t.Errorf("Body Y is %f, expected %f", simState.Bodies[0].P.Y(), 0.0)
	}

	simState.Bodies[0].V = simState.Bodies[0].V.Add(mgl32.Vec2{1, 1})

	UpdateSimulationState(simState, deltaTime)

	if simState.Bodies[0].P.X() != 1 {
		t.Errorf("Body X is %f, expected %f", simState.Bodies[0].P.X(), 1.0)
	}

	if simState.Bodies[0].P.Y() != 1 {
		t.Errorf("Body Y is %f, expected %f", simState.Bodies[0].P.Y(), 1.0)
	}
}

func TestUpdatePhysicsBodiesGravity(t *testing.T) {
	deltaTime := float32(1.0)
	idPool := idpool.NewIDPool(1, 1)
	bodies := make([]BodyData, 2)
	bodies[0] = BodyData{
		I: idpool.NewID(0),
		P: mgl32.Vec2{-2, 0},
		V: mgl32.Vec2{0, 0},
		M: 1,
		R: 1,
		// C: "#FFFFFF",
		T: 0,
	}
	bodies[1] = BodyData{
		I: idpool.NewID(1),
		P: mgl32.Vec2{2, 0},
		V: mgl32.Vec2{0, 0},
		M: 1,
		R: 1,
		// C: "#FFFFFF",
		T: 0,
	}

	simState := &SimulationState{
		GravityConstant: 1,
		TimeScale:       1,
		MaxVelocity:     1,
		Bounds:          10,
		Bodies:          bodies,
		IdPool:          idPool,
	}

	UpdateSimulationState(simState, deltaTime)

	if simState.Bodies[0].P.X() <= -2 {
		t.Errorf("Body[0] X is %f, expected > %f", simState.Bodies[0].P.X(), -2.0)
	}

	if simState.Bodies[0].P.Y() != 0 {
		t.Errorf("Body[0] Y is %f, expected %f", simState.Bodies[0].P.Y(), 0.0)
	}

	if simState.Bodies[1].P.X() >= 2 {
		t.Errorf("Body[1] X is %f, expected < %f", simState.Bodies[0].P.X(), 2.0)
	}

	if simState.Bodies[1].P.Y() != 0 {
		t.Errorf("Body[1] Y is %f, expected %f", simState.Bodies[0].P.Y(), 0.0)
	}
}

func TestSimulationBounds(t *testing.T) {
	deltaTime := float32(1.0)
	idPool := idpool.NewIDPool(1, 1)
	bodies := make([]BodyData, 1, 1)
	bodies[0] = BodyData{
		I: idpool.NewID(0),
		P: mgl32.Vec2{0, 0},
		V: mgl32.Vec2{10, 0},
		M: 1,
		R: 1,
		// C: "#FFFFFF",
		T: 0,
	}

	simState := &SimulationState{
		GravityConstant: 1,
		TimeScale:       1,
		MaxVelocity:     10,
		Bounds:          2,
		Bodies:          bodies,
		IdPool:          idPool,
	}

	UpdateSimulationState(simState, deltaTime)

	if len(simState.Bodies) != 0 {
		t.Errorf("len(Bodies) is %v, expected %v", len(simState.Bodies), 0)
	}
}

func TestStartSimulation(t *testing.T) {
	state := CreateEmptySimulationState(10, 1, 1, 10, 100, 1000, 1)
	delay := 16 * time.Millisecond
	quit := make(chan bool)
	updated := make(chan uint64)

	go StartSimulation(state, delay, quit, updated)

	for i := 0; i < 10; i++ {
		tick := <-updated
		if tick != uint64(i+1) {
			t.Errorf("Tick is %v, expected %v", tick, 1)
		}
	}
}

func TestPackBodyData(t *testing.T) {
	pv := mgl32.Vec2{123.456, -123.456}
	vv := mgl32.Vec2{987.654, -987.654}

	body := BodyData{
		I: idpool.NewID(6555),
		P: pv,
		V: vv,
		M: 12.34,
		R: 56.78,
		T: 34,
	}

	bytes, err := body.Pack()
	if err != nil {
		t.Fatalf("Error packing body data %v\n", err)
	}

	body2 := BodyData{}
	err = UnpackBodyData(bytes, &body2)
	if err != nil {
		t.Fatalf("Error unpacking body data %v\n", err)
	}

	if body.I != body2.I {
		t.Fatalf("Unpacked Body I is %v, expected %v", body2.I, body.I)
	}

	if body.P != body2.P {
		t.Fatalf("Unpacked Body P is %v, expected %v", body2.P, body.P)
	}

	if body.V != body.V {
		t.Fatalf("Unpacked Body V is %v, expected %v", body2.V, body.V)
	}

	if body.M != body2.M {
		t.Fatalf("Unpacked Body M is %v, expected %v", body2.M, body.M)
	}

	if body.R != body2.R {
		t.Fatalf("Unpacked Body R is %v, expected %v", body2.R, body.R)
	}

	if body.T != body2.T {
		t.Fatalf("Unpacked Body T is %v, expected %v", body2.T, body.T)
	}
}

func TestSimBodyAbsorb(t *testing.T) {
	deltaTime := float32(1.0)
	idPool := idpool.NewIDPool(2, 2)
	bodies := make([]BodyData, 2, 2)
	bodies[0] = BodyData{
		I: idpool.NewID(0),
		P: mgl32.Vec2{0, 0},
		V: mgl32.Vec2{0, 0},
		M: 1,
		R: 2,
		T: 0,
	}

	bodies[1] = BodyData{
		I: idpool.NewID(0),
		P: mgl32.Vec2{2.99, 0},
		V: mgl32.Vec2{0, 0},
		M: 1,
		R: 2,
		T: 0,
	}

	simState := &SimulationState{
		GravityConstant: 1,
		TimeScale:       1,
		MaxVelocity:     10,
		Bounds:          10,
		Bodies:          bodies,
		IdPool:          idPool,
	}

	UpdateSimulationState(simState, deltaTime)

	if len(simState.Bodies) != 1 {
		t.Errorf("len(Bodies) is %v, expected %v", len(simState.Bodies), 1)
	}
}

func benchmarkUpdateNPhysicsBodies(n int, b *testing.B) {
	b.StopTimer()
	deltaTime := float32((16 * time.Millisecond).Seconds())
	idPool := idpool.NewIDPool(1, 1)
	bodies := make([]BodyData, n)

	pX := float32(-n * 2)
	pY := float32(-n * 2)
	vX := float32(-1.0)
	vY := float32(1.0)

	for i := 0; i < n; i++ {
		bodies[i] = BodyData{
			I: idpool.NewID(i),
			P: mgl32.Vec2{pX, pY},
			V: mgl32.Vec2{vX, vY},
			M: 1,
			R: 1,
			// C: "#FFFFFF",
			T: 0,
		}

		pX += 2
		pY += 2
		vX *= -1
		vY *= -1
	}

	simState := &SimulationState{
		GravityConstant: 1,
		TimeScale:       1,
		MaxVelocity:     0.1,
		Bounds:          float32(n * 4),
		Bodies:          bodies,
		IdPool:          idPool,
	}

	b.StartTimer()
	for i := 0; i < b.N; i++ {
		UpdateSimulationState(simState, deltaTime)
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
