using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.IO;

namespace SensorSimulator
{
    public class DataRecorder : MonoBehaviour
    {
        [SerializeField]
        private SensorManager sensorManager;

        [Header("Recording Settings")]
        [SerializeField]
        private InputActionReference captureAction;
        [SerializeField]
        private InputActionReference recordingAction;
        [SerializeField]
        private float recordingInterval = 0.1f;
        
        [Header("Save Settings")]
        [SerializeField]
        private string baseFolderName = "SensorData";

        [Header("Animation Recording")]
        [SerializeField] private Animator pivotAnimator;
        [SerializeField] private string[] animationNames;
        private int currentAnimationIndex = -1;
        private bool isAnimationRecording = false;
        private bool isRecording = false;
        private float nextCaptureTime = 0f;
        private string currentSessionFolder;

        private void OnEnable()
        {
            if (captureAction != null && captureAction.action != null)
            {
                captureAction.action.performed += OnCapturePerformed;
                captureAction.action.Enable();
            }
            
            if (recordingAction != null && recordingAction.action != null)
            {
                recordingAction.action.performed += OnRecordingPerformed;
                recordingAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (captureAction != null && captureAction.action != null)
            {
                captureAction.action.performed -= OnCapturePerformed;
                captureAction.action.Disable();
            }
            
            if (recordingAction != null && recordingAction.action != null)
            {
                recordingAction.action.performed -= OnRecordingPerformed;
                recordingAction.action.Disable();
            }
        }

        private void Start()
        {
            if (sensorManager == null)
            {
                sensorManager = GetComponent<SensorManager>();
            }

            if (sensorManager == null)
            {
                Debug.LogError("SensorManager not found!");
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            if (isRecording && Time.time >= nextCaptureTime)
            {
                CaptureFrame();
                nextCaptureTime = Time.time + recordingInterval;
            }

            if (isAnimationRecording && pivotAnimator != null && currentAnimationIndex >= 0)
            {
                AnimatorStateInfo state = pivotAnimator.GetCurrentAnimatorStateInfo(0);
                if (!pivotAnimator.IsInTransition(0) && state.IsName(animationNames[currentAnimationIndex]) && state.normalizedTime >= 1f)
                {
                    isAnimationRecording = false;
                    isRecording = false;
                    currentAnimationIndex = -1;
                    Debug.Log("Animation recording completed");
                }
            }
        }

        public void StartAnimationRecording(int animationIndex)
        {
            if (isAnimationRecording || pivotAnimator == null || animationNames == null || animationIndex < 0 || animationIndex >= animationNames.Length)
                return;

            currentAnimationIndex = animationIndex;
            isAnimationRecording = true;
            isRecording = true;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            currentSessionFolder = Path.Combine(Application.dataPath, "..", baseFolderName, $"anim_{animationNames[animationIndex]}_{timestamp}");
            nextCaptureTime = Time.time;
            Debug.Log($"Animation {animationNames[animationIndex]}: recording started");
            pivotAnimator.Play(animationNames[animationIndex], 0, 0f);
        }

        private void OnCapturePerformed(InputAction.CallbackContext context)
        {
            CaptureFrame();
        }

        private void OnRecordingPerformed(InputAction.CallbackContext context)
        {
            ToggleRecording();
        }

        public void CaptureFrame()
        {
            if (sensorManager == null) return;

            if (string.IsNullOrEmpty(currentSessionFolder))
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                currentSessionFolder = Path.Combine(Application.dataPath, "..", baseFolderName, timestamp);
            }

            Directory.CreateDirectory(currentSessionFolder);
            StartCoroutine(CaptureFrameAsync());
        }

        private System.Collections.IEnumerator CaptureFrameAsync()
        {
            yield return StartCoroutine(sensorManager.CaptureFrameAsync((frame) =>
            {
                sensorManager.SaveFrame(frame, currentSessionFolder);
                Debug.Log($"Frame saved to folder: {currentSessionFolder}");
            }));
        }

        private void ToggleRecording()
        {
            isRecording = !isRecording;
            
            if (isRecording)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                currentSessionFolder = Path.Combine(Application.dataPath, "..", baseFolderName, timestamp);
                nextCaptureTime = Time.time;
                Debug.Log("Recording started");
            }
            else
            {
                Debug.Log("Recording stopped");
            }
        }

        private void OnGUI()
        {
            if (isRecording)
            {
                GUI.color = Color.red;
                GUI.Label(new Rect(10, 10, 200, 20), "● REC");
            }
        }
    }
}
