package idpool

import "fmt"

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
