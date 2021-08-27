using UnityEngine;
using System.Text;
using UnityEngine;

[System.Serializable]
public class BodyData
{
    public static string delimeter = ",";

    /// <summary>
    /// 16 Char ID
    /// </summary>
    [SerializeField] public string i;

    /// <summary>
    /// Position
    /// </summary>
    [SerializeField] public Vector2 p;

    /// <summary>
    /// Velocity
    /// </summary>
    [SerializeField] public Vector2 v;

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
    /// Texture
    /// </summary>
    [SerializeField] public string t;

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
