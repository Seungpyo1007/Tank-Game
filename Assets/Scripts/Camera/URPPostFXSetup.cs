using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class URPPostFXSetup : MonoBehaviour
{
    [SerializeField] private float bloomIntensity = 0.35f;
    [SerializeField] private float bloomThreshold = 1.1f;
    [SerializeField] private float vignetteIntensity = 0.15f;
    [SerializeField] private float contrast = 3f;
    [SerializeField] private float saturation = 0f;
    [SerializeField] private float exposure = 0.6f;
    [SerializeField] private bool useACES = false;
    [SerializeField] private bool applyOnAwake = true;

    void Awake() { if (applyOnAwake) Apply(); }

    [ContextMenu("Apply")]
    public void Apply()
    {
        var volume = GetComponent<Volume>();
        if (volume == null) volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "URPPostFXProfile";

        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.scatter.Override(0.65f);
        bloom.tint.Override(new Color(1f, 0.93f, 0.82f));

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(0.42f);
        vignette.color.Override(new Color(0.05f, 0.04f, 0.03f));

        var color = profile.Add<ColorAdjustments>(true);
        color.contrast.Override(contrast);
        color.saturation.Override(saturation);
        color.postExposure.Override(exposure);

        if (useACES)
        {
            var tm = profile.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.ACES);
        }

        volume.sharedProfile = profile;
    }
}
