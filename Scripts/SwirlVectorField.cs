using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class SwirlVectorField : MonoBehaviour
{
    // const
    private const int UpdateKernel = 0;
    private const int MaxPointsCount = 128;

    [SerializeField] private Vector2Int _screenResolution = new(1920, 1080);
    [SerializeField] private int _vectorFieldResolution = 512;
    [SerializeField] private float _dampValue = 1.0f;
    [SerializeField, HideInInspector] private ComputeShader _computeShaderResource = null;

    private ComputeShader _computeShader = null;
    private ComputeBuffer _pointsBuffer = null;
    private RenderTexture _vectorFieldTexture = null;
    private List<Point> _points = new();

    // properties
    public Texture VectorFieldTexture => _vectorFieldTexture;
    private int ThreadCountX => (_vectorFieldResolution + 7) / 8;
    private int ThreadCountY => (_vectorFieldResolution * _screenResolution.y / _screenResolution.x + 7) / 8;

    private void Awake()
    {
        _computeShader = Instantiate(_computeShaderResource);
        _pointsBuffer = new ComputeBuffer(MaxPointsCount, Marshal.SizeOf(typeof(Point)));
        _vectorFieldTexture = CreateRenderTexture(ThreadCountX * 8, ThreadCountY * 8);
    }

    private void OnDestroy()
    {
        Destroy(_computeShader);
        Destroy(_vectorFieldTexture);
        _pointsBuffer.Release();
        _points.Clear();
    }

    private void Update()
    {
        // update points data
        var points = _points.ToArray();
        _points.Clear();
        _pointsBuffer.SetData(points);

        // update compute
        _computeShader.SetFloat("DeltaTime", Time.deltaTime);
        _computeShader.SetFloat("DampValue", _dampValue);
        _computeShader.SetInt("PointsCount", points.Length);
        _computeShader.SetBuffer(UpdateKernel, "Points", _pointsBuffer);
        _computeShader.SetTexture(UpdateKernel, "VectorField", _vectorFieldTexture);
        _computeShader.Dispatch(UpdateKernel, ThreadCountX, ThreadCountY, 1);
    }

    /// <summary>
    /// Add point to vector field.
    /// </summary>
    /// <param name="screenPosition">スクリーン座標</param>
    /// <param name="radiusPixel">追加する点の半径のピクセルサイズ</param>
    /// <param name="attractDirection">引力の方向 1が外向き -1が内向き</param>
    /// <param name="rotateDirection">回転の方向 1が時計回り -1が反時計回り</param>
    /// <param name="attractRate">引力と回転の比重</param>
    /// <param name="rotateIntensity">回転力</param>
    public void AddPoint(Vector2 screenPosition, float radiusPixel, int attractDirection, int rotateDirection = 1, float attractRate = 0.5f, float rotateIntensity = 1.0f)
    {
        var position = new Vector2((screenPosition.x - _screenResolution.x * 0.5f) / _screenResolution.y,
                                    (screenPosition.y - _screenResolution.y * 0.5f) / _screenResolution.y);
        var radius = radiusPixel / _screenResolution.y;
        var point = new Point(position, radius, attractDirection, rotateDirection, attractRate, rotateIntensity);
        AddPoint(point);
    }

    public void AddPoint(Point point)
    {
        _points.Add(point);
        while (_points.Count > MaxPointsCount)
        {
            _points.RemoveAt(0);
        }
    }

    private RenderTexture CreateRenderTexture(int width, int height)
    {
        var renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        return renderTexture;
    }

    public struct Point
    {
        public Vector2 Position { get; }
        public float Radius { get; }
        public int AttractDirection { get; }
        public int RotateDirection { get; }
        public float AttractRate { get; }
        public float RotateIntensity { get; }

        public Point(Vector2 position, float radius, int attractDirection, int rotateDirection, float attractRate, float rotateIntensity)
        {
            Position = position;
            Radius = radius;
            AttractDirection = (int)Mathf.Sign(attractDirection);
            RotateDirection = (int)Mathf.Sign(rotateDirection);
            AttractRate = attractRate;
            RotateIntensity = rotateIntensity;
        }
    }
}