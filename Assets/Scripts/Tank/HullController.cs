using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class HullController : MonoBehaviour
{
    [Header("Wheel Positions (t55-local pre-scale)")]
    [SerializeField] private Vector3[] leftWheelsLocal = new[]
    {
        new Vector3(-130, 30, -240),
        new Vector3(-130, 30, -100),
        new Vector3(-130, 30,   60),
    };
    [SerializeField] private Vector3[] rightWheelsLocal = new[]
    {
        new Vector3(130, 30, -240),
        new Vector3(130, 30, -100),
        new Vector3(130, 30,   60),
    };

    [Header("Suspension (Newtons)")]
    [SerializeField] private float suspensionRestLength = 0.6f;
    [SerializeField] private float wheelRadius = 0.4f;
    [SerializeField] private float springK = 90000f;
    [SerializeField] private float springDamping = 25000f;

    [Header("Drive (Newtons)")]
    [SerializeField] private float maxForwardSpeed = 12f;
    [SerializeField] private float maxReverseSpeed = 6f;
    [SerializeField] private float driveForcePerError = 1200f;
    [SerializeField] private float engineBrake = 800f;
    [SerializeField] private float rollingResistance = 600f;

    [Header("Grip (Newtons)")]
    [SerializeField] private float lateralGripPerSpeed = 22000f;

    [Header("Climb / Ground")]
    [SerializeField] private float maxClimbAngle = 35f;
    [SerializeField] private LayerMask groundMask = ~(1 << 8);

    [Header("Stability")]
    [SerializeField] private Vector3 centerOfMassLocal = new Vector3(0f, 5f, 0f);

    [Header("Input")]
    [SerializeField] private bool readKeyboard = true;

    [Header("Detail Physics")]
    [SerializeField] private float antiRollStiffness = 18000f;
    [SerializeField] private float aerodynamicDrag = 0.04f;
    [SerializeField] private float handbrakeMultiplier = 4f;

    [Header("Modules")]
    [SerializeField] private TankModules modules;

    private Rigidbody rb;
    private float forwardSpeed;
    private float externalThrottle;
    private float externalSteer;
    private float[] leftCompressions;
    private float[] rightCompressions;

    public Vector3[] LeftWheels => leftWheelsLocal;
    public Vector3[] RightWheels => rightWheelsLocal;
    public float WheelRadius => wheelRadius;
    public float SuspensionRestLength => suspensionRestLength;

    public float ForwardSpeed => forwardSpeed;
    public bool OnSteepSlope => false;
    public float EngineRpm => Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(0.01f, maxForwardSpeed));

    public void SetInput(float throttle, float steer)
    {
        externalThrottle = Mathf.Clamp(throttle, -1f, 1f);
        externalSteer = Mathf.Clamp(steer, -1f, 1f);
    }

    public bool ReadKeyboard { get => readKeyboard; set => readKeyboard = value; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.None;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.centerOfMass = centerOfMassLocal;
        rb.maxAngularVelocity = 5f;
    }

    void FixedUpdate()
    {
        float throttle = 0f, steer = 0f;
        bool handbrake = false;
        if (readKeyboard)
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed) throttle += 1f;
                if (kb.sKey.isPressed) throttle -= 1f;
                if (kb.aKey.isPressed) steer -= 1f;
                if (kb.dKey.isPressed) steer += 1f;
                handbrake = kb.spaceKey.isPressed;
            }
        }
        else
        {
            throttle = externalThrottle;
            steer = externalSteer;
        }
        if (handbrake) throttle = 0f;

        float leftThrottle, rightThrottle;
        if (Mathf.Approximately(throttle, 0f) && !Mathf.Approximately(steer, 0f))
        {
            leftThrottle = steer * 0.6f;
            rightThrottle = -steer * 0.6f;
        }
        else
        {
            leftThrottle = Mathf.Clamp(throttle + steer * 0.6f, -1f, 1f);
            rightThrottle = Mathf.Clamp(throttle - steer * 0.6f, -1f, 1f);
        }

        if (leftCompressions == null || leftCompressions.Length != leftWheelsLocal.Length)
            leftCompressions = new float[leftWheelsLocal.Length];
        if (rightCompressions == null || rightCompressions.Length != rightWheelsLocal.Length)
            rightCompressions = new float[rightWheelsLocal.Length];

        float enginePerf = modules != null ? modules.EnginePerformance : 1f;
        float leftPerf = modules != null ? modules.LeftTrackPerformance : 1f;
        float rightPerf = modules != null ? modules.RightTrackPerformance : 1f;
        ProcessTrack(leftWheelsLocal, leftThrottle * enginePerf * leftPerf, handbrake, leftCompressions);
        ProcessTrack(rightWheelsLocal, rightThrottle * enginePerf * rightPerf, handbrake, rightCompressions);

        ApplyAntiRoll();

        Vector3 v = rb.linearVelocity;
        Vector3 dragForce = -v * v.magnitude * aerodynamicDrag;
        rb.AddForce(dragForce);

        if (rb.angularVelocity.sqrMagnitude > 0.001f)
            rb.AddTorque(-rb.angularVelocity * 1500f);

        forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
    }

    void ApplyAntiRoll()
    {
        if (leftCompressions == null || rightCompressions == null) return;
        int n = Mathf.Min(leftCompressions.Length, rightCompressions.Length);
        for (int i = 0; i < n; i++)
        {
            float diff = leftCompressions[i] - rightCompressions[i];
            if (Mathf.Abs(diff) < 0.001f) continue;
            float force = diff * antiRollStiffness;
            Vector3 leftPos = transform.TransformPoint(leftWheelsLocal[i]);
            Vector3 rightPos = transform.TransformPoint(rightWheelsLocal[i]);
            rb.AddForceAtPosition(-transform.up * force, leftPos);
            rb.AddForceAtPosition(transform.up * force, rightPos);
        }
    }

    void ProcessTrack(Vector3[] wheels, float throttle, bool handbrake, float[] compressionsOut)
    {
        if (wheels == null || wheels.Length == 0) return;
        for (int wi = 0; wi < wheels.Length; wi++)
        {
            var lp = wheels[wi];
            Vector3 worldPos = transform.TransformPoint(lp);
            Vector3 up = transform.up;

            float castLen = suspensionRestLength + wheelRadius;
            if (!Physics.Raycast(worldPos, -up, out RaycastHit hit, castLen, groundMask, QueryTriggerInteraction.Ignore))
            {
                if (compressionsOut != null) compressionsOut[wi] = 0f;
                continue;
            }

            float compression = (castLen - hit.distance) / suspensionRestLength;
            compression = Mathf.Clamp01(compression);
            if (compressionsOut != null) compressionsOut[wi] = compression;

            Vector3 contactPoint = hit.point;
            Vector3 groundN = hit.normal;
            Vector3 fwdOnGround = Vector3.ProjectOnPlane(transform.forward, groundN).normalized;
            Vector3 rightOnGround = Vector3.Cross(groundN, fwdOnGround).normalized;

            Vector3 wheelVel = rb.GetPointVelocity(contactPoint);
            float velUp = Vector3.Dot(wheelVel, groundN);
            float spring = Mathf.Max(0f, compression * springK - velUp * springDamping);
            rb.AddForceAtPosition(groundN * spring, worldPos);

            float maxSpeed = throttle >= 0f ? maxForwardSpeed : maxReverseSpeed;
            float fwdSpeed = Vector3.Dot(wheelVel, fwdOnGround);
            float targetFwd = throttle * maxSpeed;
            float fwdError = targetFwd - fwdSpeed;

            float groundAngle = Vector3.Angle(groundN, Vector3.up);
            float gradeMul = (throttle > 0f && groundAngle > maxClimbAngle) ? 0f : 1f;

            float driveF;
            if (handbrake)
                driveF = -fwdSpeed * engineBrake * handbrakeMultiplier;
            else if (Mathf.Approximately(throttle, 0f))
                driveF = -fwdSpeed * engineBrake;
            else
                driveF = fwdError * driveForcePerError * gradeMul;
            rb.AddForceAtPosition(fwdOnGround * driveF, contactPoint);

            float sideSpeed = Vector3.Dot(wheelVel, rightOnGround);
            rb.AddForceAtPosition(-rightOnGround * sideSpeed * lateralGripPerSpeed, contactPoint);

            if ((Mathf.Approximately(throttle, 0f) || handbrake) && Mathf.Abs(fwdSpeed) > 0.05f)
                rb.AddForceAtPosition(-fwdOnGround * Mathf.Sign(fwdSpeed) * rollingResistance, contactPoint);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (leftWheelsLocal != null) foreach (var lp in leftWheelsLocal)
        {
            var p = transform.TransformPoint(lp);
            Gizmos.DrawWireSphere(p, 0.15f);
            Gizmos.DrawLine(p, p - transform.up * (suspensionRestLength + wheelRadius));
        }
        Gizmos.color = Color.magenta;
        if (rightWheelsLocal != null) foreach (var lp in rightWheelsLocal)
        {
            var p = transform.TransformPoint(lp);
            Gizmos.DrawWireSphere(p, 0.15f);
            Gizmos.DrawLine(p, p - transform.up * (suspensionRestLength + wheelRadius));
        }
    }
}
