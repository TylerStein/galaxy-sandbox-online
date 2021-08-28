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

        public int IncrementSpriteIndex(int index) {
            if (index >= GetSpritesCount() - 1) {
                return 0;
            } else {
                return index + 1;
            }
        }

        public int DecrementSpriteIndex(int index) {
            if (index <= 0) {
                return GetSpritesCount() - 1;
            } else {
                return index - 1;
            }
        }

        public int GetSpritesCount() {
            return planetSprites.Count;
        }

        public int GetRandomSpriteIndex() {
            return Random.Range(0, planetSprites.Count);
        }

        public Sprite GetSpriteAtIndex(int index) {
            if (index < 0 || index > planetSprites.Count) {
                index = 0;
            }

            return planetSprites[index];
        }
    }
}