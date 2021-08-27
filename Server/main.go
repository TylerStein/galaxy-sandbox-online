package main

import (
	"fmt"
	"math"

	"log"
	"net/http"
	"os"
	"time"

	"github.com/go-gl/mathgl/mgl32"
	"github.com/gorilla/websocket"
)

var upgrader = websocket.Upgrader{
	ReadBufferSize:  1024,
	WriteBufferSize: 1024,
}

func handler(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		fmt.Println(err)
		return
	}

	err = conn.WriteMessage(0, []byte("Hello World!"))
	if err != nil {
		fmt.Println(err)
	}

	err = conn.Close()
	if err != nil {
		fmt.Println(err)
	}
}

const MaxBodies = 500

type SimulationState struct {
	gravityConstant float32
	maxVelocity     float32
	bounds          float32

	bodies []BodyData
	idPool []string
}

type BodyData struct {
	i string
	p mgl32.Vec2
	v mgl32.Vec2
	m float32
	r float32
	c string
	t string
}

type IDPool struct {
	step int
	size int
	pool []string
}

func (pool *IDPool) IsEmpty() bool {
	return len(pool.pool) == 0
}

func (pool *IDPool) DequeueId() string {
	if pool.IsEmpty() {
		pool.AddStep()
	}

	index := 0
	element := pool.pool[index]
	pool.pool = pool.pool[1:]
	return element
}

func (pool *IDPool) EnqueueId(value string) {
	pool.pool = append(pool.pool, value)
}

func (pool *IDPool) AddStep() {
	newArr := make([]string, pool.step)
	for i := 0; i < pool.step; i++ {
		newArr[i] = NewID(pool.size + i)
	}

	pool.size = pool.size + pool.step
	pool.pool = append(pool.pool, newArr...)
}

func NewID(index int) string {
	return fmt.Sprintf("%016d", index)
}

func NewIDPool(size int, step int) IDPool {
	pool := make([]string, size)
	for i := 0; i < size; i++ {
		pool[i] = NewID(i)
	}
	return IDPool{pool: pool, size: size, step: step}
}

func main() {
	fmt.Println("Starting server")
	http.HandleFunc("/", handler)

	port := os.Getenv("PORT")
	if port == "" {
		port = "8080"
	}

	deltaTime := 16 * time.Millisecond
	simState := &SimulationState{
		gravityConstant: 1,
		maxVelocity:     100,
		bounds:          1000,
		bodies:          make([]BodyData, MaxBodies),
		idPool:          make([]string, MaxBodies),
	}

	// tick every 16 ms
	tick := time.NewTicker(deltaTime)
	quit := make(chan bool)

	fmt.Printf("Listening on port %v\n", port)

	if err := http.ListenAndServe(":"+port, nil); err != nil {
		log.Fatal(err)
	}

	go RunSimulation(tick, quit, simState, float32(deltaTime.Seconds()))
	fmt.Println("Closing")
}

func RunSimulation(tick *time.Ticker, quit chan bool, simState *SimulationState, deltaTime float32) {
	for {
		select {
		case <-tick.C:
			UpdatePhysicsBodies(simState, deltaTime)
		case <-quit:
			tick.Stop()
			return
		}
	}
}

func UpdatePhysicsBodies(simState *SimulationState, deltaTime float32) {
	blen := len(simState.bodies)

	for i := 0; i < blen; i++ {
		forces := mgl32.Vec2{0, 0}

		for j := 0; j < blen; j++ {
			if i == j {
				continue
			}

			forces = forces.Add(calculateForces(1.0, simState.bodies[i].p.X(), simState.bodies[i].p.Y(), simState.bodies[i].m, simState.bodies[j].p.X(), simState.bodies[j].p.Y(), simState.bodies[j].m))
			forces = clampVectorMagnitude(forces, 100)
		}

		// add total forces to velocity
		simState.bodies[i].v = simState.bodies[i].v.Add(forces.Mul(deltaTime))
	}

	// use an empty struct map as a set
	toRemoveMap := make(map[int]struct{})
	for i := 0; i < blen; i++ {
		simState.bodies[i].p = simState.bodies[i].p.Add(simState.bodies[i].v.Mul(deltaTime))

		// add out of bounds bodies to the remove set
		if simState.bodies[i].p.Len() > 1000 {
			toRemoveMap[i] = struct{}{}
		}
	}

	for i := 0; i < blen; i++ {
		// skip entries in the remove set
		if _, ok := toRemoveMap[i]; ok {
			continue
		}

		for j := 0; j < blen; j++ {
			if i == j {
				continue
			}

			// skip entires in the remove set
			if _, ok := toRemoveMap[j]; ok {
				continue
			}

			diff := simState.bodies[i].p.Sub(simState.bodies[j].p)
			if simState.bodies[i].r > simState.bodies[j].r {
				if diff.Len() < simState.bodies[i].r {
					// body[i] is bigger
					toRemoveMap[j] = struct{}{}
					absorb(&simState.bodies[i], &simState.bodies[j])
				}
			} else {
				if diff.Len() < simState.bodies[j].r {
					// body[j] is bigger
					toRemoveMap[i] = struct{}{}
					absorb(&simState.bodies[j], &simState.bodies[i])
				}
			}
		}
	}

	if len(toRemoveMap) > 0 {
		// create a new array with bodies removed
		remainingBodies := make([]BodyData, blen-len(toRemoveMap))
		idx := 0
		for i := 0; i < blen; i++ {
			// skip entires in the remove set
			if _, ok := toRemoveMap[i]; ok {
				continue
			}

			simState.bodies = remainingBodies
			idx++
		}
	}
}

func absorb(self *BodyData, other *BodyData) {
	self.r += other.r * 0.5
	self.m += other.m * 0.5
	self.v = self.v.Add(other.v.Mul(0.5))
}

func clampVectorMagnitude(vector mgl32.Vec2, mag float32) mgl32.Vec2 {
	if vector.Len() > mag {
		return vector.Normalize().Mul(mag)
	}

	return vector
}

func calculateForces(g float32, x1 float32, y1 float32, m1 float32, x2 float32, y2 float32, m2 float32) mgl32.Vec2 {
	d := sqrt32(pow32(x1-x2, 2.0) + pow32(y1-y2, 2))
	a := mgl32.Vec2{(x2 - x1) / d, (y2 - y1) / d}
	return a.Mul(g * m2 / (d * d))
}

func pow32(a float32, b float32) float32 {
	return float32(math.Pow(float64(a), float64(b)))
}

func sqrt32(a float32) float32 {
	return float32(math.Sqrt(float64(a)))
}

/**
g = 0, x1 = 0, y1 = 0, m1 = 1, x2 = 2, y2 = 0, m2 = 1
d = sqrt(pow(0 - 2, 2) + pow(0 - 0, 2))
	= sqrt(4 + 0)
	= 2

a = vec2((2 - 0) / d, (0 - 0) / d)
	= vec2(2 / 2, 0 / 2)
	= vec2(1, 0)

return = a * (g * 1 / (d * d))
			 = (1, 0) * (1 * 1 / (2 * 2))
			 = (1, 0) * (0.5)
			 = (0.5, 0)
*/
