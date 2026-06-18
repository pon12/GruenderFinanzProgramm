using UnityEngine;
using UnityEngine.UIElements;

public class BreakEvenChartElement : VisualElement
{
    private float[] _revenue;
    private float[] _expenses;

    public BreakEvenChartElement()
    {
        generateVisualContent += Draw;
    }

    public void SetData(float[] revenue, float[] expenses)
    {
        _revenue = revenue;
        _expenses = expenses;

        MarkDirtyRepaint();
    }

    private void Draw(MeshGenerationContext ctx)
    {
        if (_revenue == null || _expenses == null)
            return;

        float w = contentRect.width;
        float h = contentRect.height;

        float pad = 50f;
        int count = _revenue.Length;

        if (count < 2) return;

        float maxValue = 0f;

        for (int i = 0; i < count; i++)
        {
            maxValue = Mathf.Max(maxValue, _revenue[i]);
            maxValue = Mathf.Max(maxValue, _expenses[i]);
        }

        if (maxValue <= 0) maxValue = 1f;

        var p = ctx.painter2D;

        Vector2[] rev = new Vector2[count];
        Vector2[] exp = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            float x = pad + (w - 2 * pad) * i / (count - 1);

            rev[i] = new Vector2(
                x,
                pad + (h - 2 * pad) * (1f - _revenue[i] / maxValue)
            );

            exp[i] = new Vector2(
                x,
                pad + (h - 2 * pad) * (1f - _expenses[i] / maxValue)
            );
        }

        // ======================
        // EINNAHMEN (GRÜN)
        // ======================
        p.strokeColor = Color.green;
        p.lineWidth = 3;

        p.BeginPath();
        p.MoveTo(rev[0]);

        for (int i = 1; i < count; i++)
            p.LineTo(rev[i]);

        p.Stroke();

        // ======================
        // AUSGABEN (ROT)
        // ======================
        p.strokeColor = Color.red;
        p.lineWidth = 3;

        p.BeginPath();
        p.MoveTo(exp[0]);

        for (int i = 1; i < count; i++)
            p.LineTo(exp[i]);

        p.Stroke();
    }
}