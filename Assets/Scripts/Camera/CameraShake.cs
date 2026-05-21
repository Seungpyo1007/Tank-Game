using UnityEngine;

[DefaultExecutionOrder(200)]
public class CameraShake : MonoBehaviour
{
    private float amplitude;
    private float duration;
    private float elapsed;
    private Vector3 offset;

    public void Shake(float amp, float dur)
    {
        amplitude = Mathf.Max(amplitude, amp);
        duration = Mathf.Max(duration, dur);
        elapsed = 0f;
    }

    void LateUpdate()
    {
        if (elapsed >= duration)
        {
            if (offset != Vector3.zero)
            {
                transform.localPosition -= offset;
                offset = Vector3.zero;
            }
            return;
        }
        elapsed += Time.deltaTime;
        float falloff = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration));
        Vector3 newOffset = Random.insideUnitSphere * amplitude * falloff;
        transform.localPosition += newOffset - offset;
        offset = newOffset;
    }
}
