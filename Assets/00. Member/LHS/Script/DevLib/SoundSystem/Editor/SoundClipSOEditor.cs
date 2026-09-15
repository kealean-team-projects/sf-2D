using _00._Member.LHS.Script.DevLib.SoundSystem.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace _00._Member.LHS.Script.DevLib.SoundSystem.Editor
{
    [CustomEditor(typeof(SoundClipSO))]
    public sealed class SoundClipSOEditor : UnityEditor.Editor
    {
        public VisualTreeAsset editorView;

        private const int WaveformHeight = 80;
        private const float MinDuration = 0.1f;
        private const float HandleGrabWidth = 15f;

        private Texture2D _waveformTexture;
        private AudioClip _cachedClip;

        private bool _draggingStart;
        private bool _draggingEnd;
        private bool _isPlaying;

        private float _playStartClipTime;
        private float _playEndClipTime;
        private bool _previewLoop;

        private Label _startLabel;
        private Label _endLabel;
        private Button _playButton;

        private VisualElement _controlContainer;
        private IMGUIContainer _waveformContainer;

        private static GameObject _previewGameObject;
        private static AudioSource _previewSource;

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;

            StopPreview();

            _isPlaying = false;

            DestroyWaveformTexture();
        }

        public override VisualElement CreateInspectorGUI()
        {
            SoundClipSO soundClip = target as SoundClipSO;

            if (soundClip == null)
                return base.CreateInspectorGUI();

            if (editorView == null)
            {
                Debug.LogError(
                    "[SoundClipSOEditor] Editor View UXML이 연결되지 않았습니다.");

                return base.CreateInspectorGUI();
            }

            VisualElement root = new VisualElement();

            editorView.CloneTree(root);
            root.Bind(serializedObject);

            _startLabel = root.Q<Label>("start-label");
            _endLabel = root.Q<Label>("end-label");
            _playButton = root.Q<Button>("play-btn");

            _controlContainer =
                root.Q<VisualElement>("control-container");

            VisualElement waveformSlot =
                root.Q<VisualElement>("waveform-slot");

            _waveformContainer =
                new IMGUIContainer(
                    () => OnWaveformGUI(soundClip));

            _waveformContainer.style.height =
                WaveformHeight;

            waveformSlot?.Add(_waveformContainer);

            PropertyField clipField =
                root.Q<PropertyField>("clip-field");

            clipField?.RegisterValueChangeCallback(
                evt => OnClipFieldChanged(soundClip, evt));

            if (_playButton != null)
            {
                _playButton.clicked +=
                    () => OnPlayButtonClicked(soundClip);
            }

            _cachedClip = soundClip.clip;

            bool hasClip =
                soundClip.clip != null;

            if (_controlContainer != null)
            {
                _controlContainer.style.display =
                    hasClip
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            }

            if (hasClip)
            {
                ValidateTimeRange(soundClip);
                UpdateLabels(soundClip);
            }

            return root;
        }

        private void OnClipFieldChanged(
            SoundClipSO soundClip,
            SerializedPropertyChangeEvent evt)
        {
            AudioClip newClip =
                evt.changedProperty.objectReferenceValue
                    as AudioClip;

            if (newClip == _cachedClip)
                return;

            StopPreview();

            _isPlaying = false;

            if (_playButton != null)
                _playButton.text = "Play";

            _cachedClip = newClip;

            DestroyWaveformTexture();

            serializedObject.Update();

            SerializedProperty startProperty =
                serializedObject.FindProperty("startTime");

            SerializedProperty endProperty =
                serializedObject.FindProperty("endTime");

            if (newClip != null)
            {
                startProperty.floatValue = 0f;
                endProperty.floatValue = newClip.length;
            }
            else
            {
                startProperty.floatValue = 0f;
                endProperty.floatValue = 0f;
            }

            serializedObject.ApplyModifiedProperties();

            if (_controlContainer != null)
            {
                _controlContainer.style.display =
                    newClip != null
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            }

            UpdateLabels(soundClip);

            _waveformContainer?.MarkDirtyRepaint();
        }

        private void ValidateTimeRange(
            SoundClipSO soundClip)
        {
            if (soundClip.clip == null)
                return;

            serializedObject.Update();

            SerializedProperty startProperty =
                serializedObject.FindProperty("startTime");

            SerializedProperty endProperty =
                serializedObject.FindProperty("endTime");

            float clipLength =
                soundClip.clip.length;

            startProperty.floatValue =
                Mathf.Clamp(
                    startProperty.floatValue,
                    0f,
                    clipLength);

            if (endProperty.floatValue <=
                startProperty.floatValue)
            {
                endProperty.floatValue =
                    clipLength;
            }

            endProperty.floatValue =
                Mathf.Clamp(
                    endProperty.floatValue,
                    startProperty.floatValue,
                    clipLength);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private void UpdateLabels(
            SoundClipSO soundClip)
        {
            if (soundClip == null)
                return;

            if (_startLabel != null)
            {
                _startLabel.text =
                    $"Start : {soundClip.startTime:F3}s";
            }

            if (_endLabel != null)
            {
                _endLabel.text =
                    $"End : {soundClip.endTime:F3}s";
            }
        }

        private void OnWaveformGUI(
            SoundClipSO soundClip)
        {
            if (soundClip == null ||
                soundClip.clip == null)
            {
                return;
            }

            Rect waveformRect =
                GUILayoutUtility.GetRect(
                    GUIContent.none,
                    GUIStyle.none,
                    GUILayout.Height(WaveformHeight),
                    GUILayout.ExpandWidth(true));

            if (Event.current.type ==
                EventType.Repaint)
            {
                int width =
                    Mathf.Max(
                        1,
                        Mathf.RoundToInt(
                            waveformRect.width));

                if (_waveformTexture == null ||
                    _waveformTexture.width != width)
                {
                    DestroyWaveformTexture();

                    _waveformTexture =
                        BuildWaveform(
                            soundClip.clip,
                            width,
                            WaveformHeight);
                }
            }

            if (_waveformTexture != null)
            {
                DrawWaveformAndHandles(
                    waveformRect,
                    soundClip);
            }

            if (Event.current.type ==
                EventType.Repaint)
            {
                UpdateLabels(soundClip);
            }
        }

        private Texture2D BuildWaveform(
            AudioClip clip,
            int width,
            int height)
        {
            float[] samples =
                new float[
                    clip.samples *
                    clip.channels];

            bool success =
                clip.GetData(samples, 0);

            Color backgroundColor =
                new Color(
                    0.13f,
                    0.13f,
                    0.13f,
                    1f);

            Color waveformColor =
                new Color(
                    0.38f,
                    0.68f,
                    1f,
                    1f);

            Color[] pixels =
                new Color[
                    width *
                    height];

            for (int i = 0;
                 i < pixels.Length;
                 i++)
            {
                pixels[i] =
                    backgroundColor;
            }

            if (!success)
            {
                Texture2D emptyTexture =
                    new Texture2D(
                        width,
                        height,
                        TextureFormat.RGBA32,
                        false);

                emptyTexture.SetPixels(pixels);
                emptyTexture.Apply();

                return emptyTexture;
            }

            int totalSamples =
                clip.samples;

            int channelCount =
                clip.channels;

            for (int x = 0;
                 x < width;
                 x++)
            {
                int sampleStart =
                    (int)(
                        (float)x /
                        width *
                        totalSamples) *
                    channelCount;

                int sampleEnd =
                    (int)(
                        (float)(x + 1) /
                        width *
                        totalSamples) *
                    channelCount;

                sampleStart =
                    Mathf.Clamp(
                        sampleStart,
                        0,
                        samples.Length - 1);

                sampleEnd =
                    Mathf.Clamp(
                        sampleEnd,
                        sampleStart + 1,
                        samples.Length);

                float min = 0f;
                float max = 0f;

                for (int sampleIndex =
                         sampleStart;
                     sampleIndex <
                         sampleEnd;
                     sampleIndex++)
                {
                    float sample =
                        samples[sampleIndex];

                    if (sample < min)
                        min = sample;

                    if (sample > max)
                        max = sample;
                }

                int yMin =
                    Mathf.Clamp(
                        (int)(
                            (min * 0.5f + 0.5f) *
                            height),
                        0,
                        height - 1);

                int yMax =
                    Mathf.Clamp(
                        (int)(
                            (max * 0.5f + 0.5f) *
                            height),
                        0,
                        height - 1);

                for (int y = yMin;
                     y <= yMax;
                     y++)
                {
                    pixels[y * width + x] =
                        waveformColor;
                }
            }

            Texture2D texture =
                new Texture2D(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false);

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        private void DrawWaveformAndHandles(
            Rect waveformRect,
            SoundClipSO soundClip)
        {
            float duration =
                soundClip.clip.length;

            if (duration <= 0f)
                return;

            float startX =
                waveformRect.x +
                soundClip.startTime /
                duration *
                waveformRect.width;

            float endX =
                waveformRect.x +
                soundClip.endTime /
                duration *
                waveformRect.width;

            GUI.DrawTexture(
                waveformRect,
                _waveformTexture,
                ScaleMode.StretchToFill);

            Color outsideColor =
                new Color(
                    0f,
                    0f,
                    0f,
                    0.5f);

            EditorGUI.DrawRect(
                new Rect(
                    waveformRect.x,
                    waveformRect.y,
                    Mathf.Max(
                        0f,
                        startX -
                        waveformRect.x),
                    waveformRect.height),
                outsideColor);

            EditorGUI.DrawRect(
                new Rect(
                    endX,
                    waveformRect.y,
                    Mathf.Max(
                        0f,
                        waveformRect.xMax -
                        endX),
                    waveformRect.height),
                outsideColor);

            DrawPlayHead(
                waveformRect,
                soundClip,
                duration);

            Color startColor =
                new Color(
                    0.25f,
                    0.9f,
                    0.25f);

            Color endColor =
                new Color(
                    0.95f,
                    0.35f,
                    0.2f);

            EditorGUI.DrawRect(
                new Rect(
                    startX - 1f,
                    waveformRect.y,
                    2f,
                    waveformRect.height),
                startColor);

            EditorGUI.DrawRect(
                new Rect(
                    endX - 1f,
                    waveformRect.y,
                    2f,
                    waveformRect.height),
                endColor);

            EditorGUI.DrawRect(
                new Rect(
                    startX - 5f,
                    waveformRect.y,
                    10f,
                    12f),
                startColor);

            EditorGUI.DrawRect(
                new Rect(
                    endX - 5f,
                    waveformRect.y,
                    10f,
                    12f),
                endColor);

            Rect startGrabRect =
                new Rect(
                    startX -
                    HandleGrabWidth * 0.5f,
                    waveformRect.y,
                    HandleGrabWidth,
                    waveformRect.height);

            Rect endGrabRect =
                new Rect(
                    endX -
                    HandleGrabWidth * 0.5f,
                    waveformRect.y,
                    HandleGrabWidth,
                    waveformRect.height);

            EditorGUIUtility.AddCursorRect(
                startGrabRect,
                MouseCursor.ResizeHorizontal);

            EditorGUIUtility.AddCursorRect(
                endGrabRect,
                MouseCursor.ResizeHorizontal);

            HandleDrag(
                waveformRect,
                soundClip,
                startGrabRect,
                endGrabRect,
                duration);
        }

        private void DrawPlayHead(
            Rect waveformRect,
            SoundClipSO soundClip,
            float duration)
        {
            if (!_isPlaying ||
                _previewSource == null ||
                _previewSource.clip !=
                soundClip.clip)
            {
                return;
            }

            float headX =
                waveformRect.x +
                _previewSource.time /
                duration *
                waveformRect.width;

            EditorGUI.DrawRect(
                new Rect(
                    headX - 1f,
                    waveformRect.y,
                    2f,
                    waveformRect.height),
                Color.white);
        }

        private void HandleDrag(
            Rect waveformRect,
            SoundClipSO soundClip,
            Rect startGrabRect,
            Rect endGrabRect,
            float duration)
        {
            Event currentEvent =
                Event.current;

            switch (currentEvent.type)
            {
                case EventType.MouseDown
                    when currentEvent.button == 0:
                {
                    if (startGrabRect.Contains(
                            currentEvent.mousePosition))
                    {
                        _draggingStart = true;
                        currentEvent.Use();
                    }
                    else if (
                        endGrabRect.Contains(
                            currentEvent.mousePosition))
                    {
                        _draggingEnd = true;
                        currentEvent.Use();
                    }

                    break;
                }

                case EventType.MouseUp
                    when currentEvent.button == 0:
                {
                    _draggingStart = false;
                    _draggingEnd = false;

                    break;
                }

                case EventType.MouseDrag
                    when _draggingStart ||
                         _draggingEnd:
                {
                    float normalized =
                        Mathf.Clamp01(
                            (currentEvent.mousePosition.x -
                             waveformRect.x) /
                            waveformRect.width);

                    float time =
                        normalized *
                        duration;

                    serializedObject.Update();

                    if (_draggingStart)
                    {
                        float maxStart =
                            Mathf.Max(
                                0f,
                                soundClip.endTime -
                                MinDuration);

                        time =
                            Mathf.Clamp(
                                time,
                                0f,
                                maxStart);

                        serializedObject
                            .FindProperty("startTime")
                            .floatValue = time;
                    }
                    else
                    {
                        float minEnd =
                            Mathf.Min(
                                duration,
                                soundClip.startTime +
                                MinDuration);

                        time =
                            Mathf.Clamp(
                                time,
                                minEnd,
                                duration);

                        serializedObject
                            .FindProperty("endTime")
                            .floatValue = time;
                    }

                    serializedObject
                        .ApplyModifiedProperties();

                    UpdateLabels(soundClip);

                    currentEvent.Use();

                    break;
                }
            }
        }

        private void OnPlayButtonClicked(
            SoundClipSO soundClip)
        {
            if (soundClip == null ||
                soundClip.clip == null)
            {
                return;
            }

            if (_isPlaying)
            {
                StopPreview();

                _isPlaying = false;

                if (_playButton != null)
                    _playButton.text = "Play";

                return;
            }

            float pitch =
                soundClip.pitch;

            if (soundClip.randomizePitch)
            {
                pitch += Random.Range(
                    -soundClip.randomPitchModifier,
                    soundClip.randomPitchModifier);
            }

            pitch =
                Mathf.Clamp(
                    pitch,
                    0.1f,
                    3f);

            _playStartClipTime =
                soundClip.startTime;

            _playEndClipTime =
                soundClip.endTime;

            _previewLoop =
                soundClip.isLoop;

            PlayPreview(
                soundClip.clip,
                _playStartClipTime,
                pitch);

            _isPlaying = true;

            if (_playButton != null)
                _playButton.text = "Stop";
        }

        private void PlayPreview(
            AudioClip clip,
            float startTime,
            float pitch)
        {
            AudioSource source =
                EnsurePreviewSource();

            source.Stop();

            source.clip = clip;
            source.pitch = pitch;
            source.loop = false;
            source.volume = 1f;

            SetPreviewTime(
                source,
                startTime);

            source.Play();
        }

        private void OnEditorUpdate()
        {
            if (!_isPlaying)
                return;

            if (_previewSource == null)
            {
                FinishPreview();
                return;
            }

            bool reachedEnd =
                _previewSource.isPlaying &&
                _previewSource.time >=
                _playEndClipTime;

            bool stopped =
                !_previewSource.isPlaying;

            if (reachedEnd || stopped)
            {
                if (_previewLoop)
                {
                    SetPreviewTime(
                        _previewSource,
                        _playStartClipTime);

                    _previewSource.Play();
                }
                else
                {
                    FinishPreview();
                }
            }

            _waveformContainer?
                .MarkDirtyRepaint();
        }

        private void FinishPreview()
        {
            StopPreview();

            _isPlaying = false;

            if (_playButton != null)
                _playButton.text = "Play";

            _waveformContainer?
                .MarkDirtyRepaint();
        }

        private static void SetPreviewTime(
            AudioSource source,
            float time)
        {
            if (source.clip == null ||
                source.clip.samples <= 0)
            {
                return;
            }

            int sample =
                Mathf.RoundToInt(
                    time *
                    source.clip.frequency);

            sample =
                Mathf.Clamp(
                    sample,
                    0,
                    source.clip.samples - 1);

            source.timeSamples =
                sample;
        }

        private static void StopPreview()
        {
            if (_previewSource != null)
                _previewSource.Stop();
        }

        private static AudioSource EnsurePreviewSource()
        {
            if (_previewSource != null)
                return _previewSource;

            _previewGameObject =
                EditorUtility
                    .CreateGameObjectWithHideFlags(
                        "~SoundPreview",
                        HideFlags.HideAndDontSave,
                        typeof(AudioSource));

            _previewSource =
                _previewGameObject
                    .GetComponent<AudioSource>();

            _previewSource.playOnAwake =
                false;

            _previewSource.loop =
                false;

            return _previewSource;
        }

        private void DestroyWaveformTexture()
        {
            if (_waveformTexture == null)
                return;

            DestroyImmediate(
                _waveformTexture);

            _waveformTexture = null;
        }
    }
}