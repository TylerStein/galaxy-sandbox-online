package sim

import (
	"fmt"
	"math"
	"sync"
	"time"

	"github.com/go-gl/mathgl/mgl32"

	"github.com/TylerStein/galaxy-sandbox-online/internal/idpool"
)

type SimulationState struct {
	Mu sync.Mutex

	GravityConstant float32
	MaxVelocity     float32
	Bounds          float32

	Bodies []BodyData
	IdPool idpool.IDPool
}

func CreateEmptySimulationState(maxBodies int, g float32, maxVelocity float32, bounds float32) *SimulationState {
	return &SimulationState{
		GravityConstant: g,
		MaxVelocity:     maxVelocity,
		Bounds:          bounds,
		Bodies:          make([]BodyData, 0, maxBodies),
		IdPool:          idpool.NewIDPool(maxBodies, 10),
	}
}

type BodyData struct {
	I string     `json:"i"`
	P mgl32.Vec2 `json:"p"`
	V mgl32.Vec2 `json:"v"`
	M float32    `json:"m"`
	R float32    `json:"r"`
	C string     `json:"c"`
	T string     `json:"t"`
}

func (data *BodyData) CleanBodyData() {
	if data.M <= 0 {
		data.M = 1
	}

	if data.R <= 0 {
		data.R = 0.25
	}

	if len(data.C) != 7 {
		data.C = "#FFFFFF"
	}

	if len(data.T) == 0 {
		data.T = "default"
	}
}

func StartSimulation(state *SimulationState, delay time.Duration, quit chan bool, updated chan uint64) {
	count := uint64(0)
	deltaTime := float32(delay) / float32(time.Second)
	tick := time.NewTicker(delay)

	for {
		select {
		case <-tick.C:
			UpdateSimulationState(state, float32(deltaTime))
			count++
			updated <- count
		case shouldQuit := <-quit:
			if shouldQuit {
				tick.Stop()
				break
			}
		}
	}
}

func AddSimulationBody(simState *SimulationState, body BodyData) {
	if len(simState.Bodies) >= cap(simState.Bodies)-1 {
		fmt.Printf("Ignoring added simulation body due to full capacity")
		return
	}

	body.I = simState.IdPool.DequeueId()
	body.CleanBodyData()
	simState.Bodies = append(simState.Bodies, body)
}

func UpdateSimulationState(simState *SimulationState, deltaTime float32) {
	defer simState.Mu.Unlock()
	simState.Mu.Lock()

	blen := len(simState.Bodies)

	for i := 0; i < blen; i++ {
		forces := mgl32.Vec2{0, 0}

		for j := 0; j < blen; j++ {
			if i == j {
				continue
			}

			forces = forces.Add(calculateForces(1.0, simState.Bodies[i].P.X(), simState.Bodies[i].P.Y(), simState.Bodies[i].M, simState.Bodies[j].P.X(), simState.Bodies[j].P.Y(), simState.Bodies[j].M))
			forces = clampVectorMagnitude(forces, 100)
		}

		// add total forces to velocity
		simState.Bodies[i].V = simState.Bodies[i].V.Add(forces.Mul(deltaTime))
	}

	// use an empty struct map as a set
	toRemoveMap := make(map[int]bool)
	for i := 0; i < blen; i++ {
		simState.Bodies[i].P = simState.Bodies[i].P.Add(simState.Bodies[i].V.Mul(deltaTime))

		// add out of bounds bodies to the remove set
		if simState.Bodies[i].P.Len() > 1000 {
			toRemoveMap[i] = true
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

			diff := simState.Bodies[i].P.Sub(simState.Bodies[j].P)
			if simState.Bodies[i].R > simState.Bodies[j].R {
				if diff.Len() < simState.Bodies[i].R {
					// body[i] is bigger
					toRemoveMap[j] = true
					absorb(&simState.Bodies[i], &simState.Bodies[j])
				}
			} else {
				if diff.Len() < simState.Bodies[j].R {
					// body[j] is bigger
					toRemoveMap[i] = true
					absorb(&simState.Bodies[j], &simState.Bodies[i])
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
			if rm, ok := toRemoveMap[i]; ok && rm {
				continue
			}

			remainingBodies[idx] = simState.Bodies[i]
			idx++
		}
		simState.Bodies = remainingBodies
	}
}

func absorb(self *BodyData, other *BodyData) {
	self.R += other.R * 0.5
	self.M += other.M * 0.5
	self.V = self.V.Add(other.V.Mul(0.5))
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
