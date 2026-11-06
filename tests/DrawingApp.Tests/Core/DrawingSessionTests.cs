using DrawingApp.Core.Application.Services;
using DrawingApp.Core.Domain.Documents;
using DrawingApp.Core.Domain.Imaging;
using DrawingApp.Core.Domain.Tools;

namespace DrawingApp.Tests.Core;

public class DrawingSessionTests
{
    private readonly ToolOptions _options = new();
    private readonly DrawingSession _session;
    private readonly List<PixelRect> _invalidated = [];

    public DrawingSessionTests()
    {
        _session = new DrawingSession(_options, [new PenTool(_options)], new DrawingDocument());
        _session.Invalidated += (_, e) => _invalidated.Add(e.Area);
    }

    [Fact]
    public void New_HasDefaultSizedTransparentDrawingLayer()
    {
        Assert.Equal(DrawingDocument.DefaultWidth, _session.DrawingLayer.Width);
        Assert.Equal(DrawingDocument.DefaultHeight, _session.DrawingLayer.Height);
        Assert.Equal(1280, DrawingDocument.DefaultWidth);
        Assert.Equal(720, DrawingDocument.DefaultHeight);
        Assert.False(_session.IsStrokeActive);
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new DrawingSession(null!, [], new DrawingDocument()));
        Assert.Throws<ArgumentNullException>(() => new DrawingSession(_options, null!, new DrawingDocument()));
        Assert.Throws<ArgumentNullException>(() => new DrawingSession(_options, [], null!));
    }

    [Fact]
    public void Constructor_DuplicateToolKind_Throws() =>
        Assert.Throws<ArgumentException>(() => new DrawingSession(_options, [new PenTool(_options), new PenTool(_options)], new DrawingDocument()));

    [Fact]
    public void PenStroke_DrawsOnDrawingLayerAndRaisesDirtyAreas()
    {
        _session.BeginStroke(new CanvasPoint(10, 10));
        Assert.True(_session.IsStrokeActive);
        _session.ContinueStroke(new CanvasPoint(20, 10));
        _session.EndStroke(new CanvasPoint(30, 10));

        Assert.False(_session.IsStrokeActive);
        Assert.Equal(3, _invalidated.Count);
        Assert.Equal(RgbaColor.Black, _session.DrawingLayer.GetPixel(25, 10));
    }

    [Fact]
    public void BeginWhileActive_IsIgnored()
    {
        _session.BeginStroke(new CanvasPoint(10, 10));
        _session.BeginStroke(new CanvasPoint(500, 500));

        Assert.Single(_invalidated);
        Assert.Equal(0, _session.DrawingLayer.GetPixel(500, 500).A);
    }

    [Fact]
    public void ToolWithoutImplementation_IsIgnored()
    {
        _options.ActiveTool = ToolKind.Dot;

        _session.BeginStroke(new CanvasPoint(10, 10));

        Assert.False(_session.IsStrokeActive);
        Assert.Empty(_invalidated);
    }

    [Fact]
    public void ActiveTool_SelectsMatchingTool()
    {
        var session = new DrawingSession(_options, [new PenTool(_options), new DotTool(_options)], new DrawingDocument());
        _options.ActiveTool = ToolKind.Dot;

        session.BeginStroke(new CanvasPoint(10.5, 10.5));
        session.ContinueStroke(new CanvasPoint(30.5, 10.5));
        session.EndStroke(new CanvasPoint(30.5, 10.5));

        Assert.Equal(RgbaColor.Black, session.DrawingLayer.GetPixel(10, 10));
        Assert.Equal(0, session.DrawingLayer.GetPixel(20, 10).A);
    }

    [Fact]
    public void Stroke_EditsActiveLayerOnly()
    {
        var background = _session.Document.Background.Pixels.GetBuffer().ToArray();

        _session.BeginStroke(new CanvasPoint(10, 10));
        _session.EndStroke(new CanvasPoint(100, 10));

        Assert.Same(_session.Document.ActiveLayer.Pixels, _session.DrawingLayer);
        Assert.Equal(RgbaColor.Black, _session.DrawingLayer.GetPixel(50, 10));
        Assert.Equal(background, _session.Document.Background.Pixels.GetBuffer());
    }

    [Fact]
    public void DocumentReset_DropsActiveStrokeAndUsesNewLayer()
    {
        _session.BeginStroke(new CanvasPoint(10, 10));
        _invalidated.Clear();

        _session.Document.LoadBackground(new PixelBuffer(200, 100));
        _session.ContinueStroke(new CanvasPoint(50, 10));

        Assert.False(_session.IsStrokeActive);
        Assert.Empty(_invalidated);
        Assert.Equal(200, _session.DrawingLayer.Width);
    }

    [Fact]
    public void MoveOrEndWithoutStroke_DoesNothing()
    {
        _session.ContinueStroke(new CanvasPoint(10, 10));
        _session.EndStroke(new CanvasPoint(10, 10));
        _session.CancelStroke();

        Assert.Empty(_invalidated);
    }

    [Fact]
    public void CancelStroke_EndsAtLastPoint()
    {
        _session.BeginStroke(new CanvasPoint(10, 10));
        _session.ContinueStroke(new CanvasPoint(40, 10));

        _session.CancelStroke();

        Assert.False(_session.IsStrokeActive);
        Assert.Equal(0, _session.DrawingLayer.GetPixel(45, 10).A);
    }

    [Fact]
    public void OutOfCanvasStroke_RaisesNothing()
    {
        _session.BeginStroke(new CanvasPoint(-100, -100));
        _session.EndStroke(new CanvasPoint(-90, -100));

        Assert.Empty(_invalidated);
    }
}
