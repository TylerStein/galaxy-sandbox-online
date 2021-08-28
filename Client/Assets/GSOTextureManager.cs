using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GSO
{
    public class GSOTextureManager : MonoBehaviour
    {
        public List<Sprite> planetSprites = new List<Sprite>();

        public void Awake() {
            if (planetSprites.Count == 0) throw new UnityException("PlanetSprites cannot be empty!");
        }

        public byte IncrementSpriteIndex(byte index) {
            if (index >= GetSpritesCount() - 1) {
                return 0;
            } else {
                return (byte)(index + 1);
            }
        }

        public byte DecrementSpriteIndex(byte index) {
            if (index <= 0) {
                return (byte)(GetSpritesCount() - 1);
            } else {
                return (byte)(index - 1);
            }
        }

        public int GetSpritesCount() {
            return planetSprites.Count;
        }

        public byte GetRandomSpriteIndex() {
            return (byte)Random.Range(0, planetSprites.Count);
        }

        public Sprite GetSpriteAtIndex(int index) {
            if (index < 0 || index > planetSprites.Count) {
                index = 0;
            }

            return planetSprites[index];
        }
    }
}