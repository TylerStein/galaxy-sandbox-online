using UnityEngine;
using System.Text;

[System.Serializable]
public class FrameData
{
    [SerializeField] public BodyData[] d;
    [SerializeField] public ushort p;
}

[System.Serializable]
public class BodyData
{
    public static string delimeter = ",";

    /// <summary>
    /// Uint16 id
    /// </summary>
    [SerializeField] public ushort i;

    /// <summary>
    /// Position
    /// </summary>
    [SerializeField] public float[] p;

    public Vector2 pvec {
        get => new Vector2(p[0], p[1]);
        set {
            p[0] = value.x;
            p[1] = value.y;
        }
    }

    /// <summary>
    /// Velocity
    /// </summary>
    [SerializeField] public float[] v;

    public Vector2 vvec {
        get => new Vector2(v[0], v[1]);
        set {
            v[0] = value.x;
            v[1] = value.y;
        }
    }

    /// <summary>
    /// Mass
    /// </summary>
    [SerializeField] public float m;

    /// <summary>
    /// Radius Field
    /// </summary>
    [SerializeField] public float r;

    /// <summary>
    /// Color (Hex)
    /// </summary>
    [SerializeField] public string c;

    /// <summary>
    /// Texture (Index)
    /// </summary>
    [SerializeField] public byte t;

    //public void Append(StringBuilder sb) {
    //    sb.Append(i)
    //      .Append(delimeter)
    //      .Append(p.x).Append(p.y)
    //      .Append(delimeter)
    //      .Append(v.x).Append(v.y)
    //      .Append(delimeter)
    //      .Append(m)
    //      .Append(delimeter)
    //      .Append(r)
    //      .Append(delimeter)
    //      .Append(c.x).Append(c.y)
    //      .Append(delimeter)
    //      .Append(t);
    //}
    public string ToJson() {
        return JsonUtility.ToJson(this);
    }

    public static BodyData FromJson(string data) {
        return JsonUtility.FromJson<BodyData>(data);
    }
}
