using UnityEngine;

public class FloatBob : MonoBehaviour
{
    [Header("Dreamy Up/Down (Slow In Slow Out)")]
    public float amplitude = 0.03f; // 0.02~0.04
    public float period = 6f;       // 一轮上下浮动用多少秒：4~8
    public float verticalOffset = 0f;

    [Header("Per-object phase so they don't sync")]
    public bool randomPhase = true;

    Vector3 startPos;
    float phase;

    void Start()
    {
        startPos = transform.localPosition;
        phase = randomPhase ? Random.Range(0f, 10f) : 0f;
    }

    void Update()
    {
        // 0~1 的循环
        float t = (Time.time + phase) / Mathf.Max(0.01f, period);

        // Sine：天然 slow in / slow out
       float y1 = Mathf.Sin(t * Mathf.PI * 2f) * amplitude;
       float y2 = Mathf.Sin((t * 0.5f + 0.37f) * Mathf.PI * 2f) * (amplitude * 0.15f);
       float y = y1 + y2 + verticalOffset;


        transform.localPosition = startPos + new Vector3(0f, y, 0f);
    }
}
