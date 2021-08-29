package sim

import (
	"bytes"
	"encoding/binary"
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
	TimeScale       float32
	MassScale       float32
	MaxVelocity     float32
	Bounds          float32

	Bodies []BodyData
	IdPool idpool.IDPool
}

func CreateEmptySimulationState(maxBodies int, gravityConstant float32, timeScale float32, massScale float32, maxVelocity float32, bounds float32) *SimulationState {
	return &SimulationState{
		GravityConstant: gravityConstant,
		TimeScale:       timeScale,
		MassScale:       massScale,
		MaxVelocity:     maxVelocity,
		Bounds:          bounds,
		Bodies:          make([]BodyData, 0, maxBodies),
		IdPool:          idpool.NewIDPool(maxBodies, 10),
	}
}

const BodyPacketBits = 16 + 32 + 32 + 32 + 32 + 32 + 32 + 8
const BodyPacketBytes = BodyPacketBits / 8

type BodyPacket struct {
	I  uint16
	PX float32
	PY float32
	VX float32
	VY float32
	M  float32
	R  float32
	T  uint8
}

type BodyData struct {
	I uint16     `json:"i"`
	P mgl32.Vec2 `json:"p"`
	V mgl32.Vec2 `json:"v"`
	M float32    `json:"m"`
	R float32    `json:"r"`
	T uint8      `json:"t"`
}

func (data *BodyData) Pack() ([]byte, error) {
	buffer := new(bytes.Buffer)
	packet := BodyPacket{
		I:  data.I,
		PX: data.P.X(),
		PY: data.P.Y(),
		VX: data.V.X(),
		VY: data.V.Y(),
		M:  data.M,
		R:  data.R,
		T:  data.T,
	}

	err := binary.Write(buffer, binary.LittleEndian, packet)
	if err != nil {
		fmt.Printf("Error packing BodyData %v\n", err)
		return nil, err
	}

	return buffer.Bytes(), nil
}

func UnpackBodyData(packet []byte, data *BodyData) error {
	reader := bytes.NewReader(packet)

	var dataOut BodyPacket
	err := binary.Read(reader, binary.LittleEndian, &dataOut)
	if err != nil {
		fmt.Printf("Error binary reading packet %v\n", err)
		return err
	}

	data.I = dataOut.I
	data.P = mgl32.Vec2{dataOut.PX, dataOut.PY}
	data.V = mgl32.Vec2{dataOut.VX, dataOut.VY}
	data.M = dataOut.M
	data.R = dataOut.R
	data.T = dataOut.T

	return nil
}

func calculateMass(radius float32, massSizeMultiplier float32) float32 {
	return pow32(1.0+radius, massSizeMultiplier)
}

func (data *BodyData) CleanBodyData(massScale float32) {
	if data.R <= 0.0 {
		data.R = 0.25
	} else if data.R > 4.0 {
		data.R = 4.0
	}

	// m = r * 10
	data.M = calculateMass(data.R, massScale)

	// if len(data.C) != 7 {
	// 	data.C = "#FFFFFF"
	// }

	if data.T < 0 {
		data.T = 0
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
		fmt.Println("Ignoring added simulation body due to full capacity")
		return
	}

	body.I = simState.IdPool.DequeueId()
	body.CleanBodyData(simState.MassScale)
	simState.Bodies = append(simState.Bodies, body)
}

func UpdateSimulationState(simState *SimulationState, deltaTime float32) {
	defer simState.Mu.Unlock()
	simState.Mu.Lock()

	deltaTime *= simState.TimeScale
	blen := len(simState.Bodies)

	for i := 0; i < blen; i++ {
		forces := mgl32.Vec2{0, 0}

		for j := 0; j < blen; j++ {
			if i == j {
				continue
			}

			forces = forces.Add(calculateForces2(simState.GravityConstant, simState.Bodies[i].P.X(), simState.Bodies[i].P.Y(), simState.Bodies[i].M, simState.Bodies[j].P.X(), simState.Bodies[j].P.Y(), simState.Bodies[j].M))
		}

		forces = clampVectorMagnitude(forces, simState.MaxVelocity)
		m2 := 1.0 / pow32(1.0+simState.Bodies[i].M, 2.0)
		simState.Bodies[i].V = clampVectorMagnitude(simState.Bodies[i].V.Add(forces.Mul(m2).Mul(deltaTime)), simState.MaxVelocity)
	}

	// use an empty struct map as a set
	toRemoveMap := make(map[int]bool)
	for i := 0; i < blen; i++ {
		simState.Bodies[i].P = simState.Bodies[i].P.Add(simState.Bodies[i].V.Mul(deltaTime))

		// add out of bounds bodies to the remove set
		if simState.Bodies[i].P.Len() > simState.Bounds {
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

			diff := simState.Bodies[i].P.Sub(simState.Bodies[j].P).Len()
			if diff < (simState.Bodies[i].R + simState.Bodies[j].R) {
				if simState.Bodies[i].R > simState.Bodies[j].R {
					toRemoveMap[j] = true
					absorb(&simState.Bodies[i], &simState.Bodies[j], simState.MassScale)
				} else {
					toRemoveMap[i] = true
					absorb(&simState.Bodies[j], &simState.Bodies[i], simState.MassScale)
				}
			}
		}
	}

	if len(toRemoveMap) > 0 {
		// create a new array with bodies removed
		remainingBodies := make([]BodyData, blen-len(toRemoveMap), cap(simState.Bodies))
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

func absorb(self *BodyData, other *BodyData, massScale float32) {
	self.R += other.R * 0.15
	self.M += calculateMass(self.R, massScale)
	// self.V = self.V.Add(other.V.Mul(0.5))
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

func calculateForces2(g float32, x1 float32, y1 float32, m1 float32, x2 float32, y2 float32, m2 float32) mgl32.Vec2 {
	r := mgl32.Vec2{x2 - x1, y2 - y1}
	d := r.Len()

	if d < 0.5 {
		d = 0.5
	}

	r.Normalize()
	s := (g * m1 * m2) / (d * d)
	return r.Mul(s)
}

func pow32(a float32, b float32) float32 {
	return float32(math.Pow(float64(a), float64(b)))
}

func sqrt32(a float32) float32 {
	return float32(math.Sqrt(float64(a)))
}
