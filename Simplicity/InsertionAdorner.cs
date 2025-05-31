using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;

public class InsertionAdorner : Adorner
{
    private readonly bool _isAbove;
    public InsertionAdorner(UIElement adornedElement, bool isAbove)
        : base(adornedElement)
    {
        _isAbove = isAbove;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        var adorned = (FrameworkElement)AdornedElement;
        double y = _isAbove ? 0 : adorned.ActualHeight;
        var pen = new Pen(Brushes.DeepSkyBlue, 2);
        dc.DrawLine(pen, new Point(0, y), new Point(adorned.ActualWidth, y));
    }
}