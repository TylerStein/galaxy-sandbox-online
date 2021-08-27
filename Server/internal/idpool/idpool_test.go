package idpool

import "testing"

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
