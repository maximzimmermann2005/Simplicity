using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

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
        var pen = new Pen(Brushes.DeepSkyBlue, 2);

        double halfPenWidth = pen.Thickness / 2;
        double y = _isAbove ? halfPenWidth : adorned.ActualHeight - halfPenWidth;

        dc.DrawLine(pen, new Point(0, y), new Point(adorned.ActualWidth, y));
    }
}