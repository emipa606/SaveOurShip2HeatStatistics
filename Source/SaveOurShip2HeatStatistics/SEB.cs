using System.Text;
using Verse;

namespace SOS2HS;

public class SEB
{
    private readonly StringBuilder builder = new StringBuilder();
    public string node;
    public string prefix;

    public SEB(string prefix = "", string node = "")
    {
        this.prefix = prefix;
        this.node = node;
    }


    public SEB Node(string node)
    {
        this.node = node;
        builder.AppendLine($"{prefix}_{node}".Translate());
        return this;
    }

    public SEB Equation()
    {
        builder.AppendLine($"  (= {$"{prefix}_Equation_{node}".Translate()} )");
        return this;
    }

    public SEB Calculus(params object[] equationValues)
    {
        builder.AppendLine($"  (= {$"{prefix}_Calculus_{node}".Translate(equationValues)} )");
        return this;
    }

    public SEB Value(float value)
    {
        builder.AppendLine($"  {$"{prefix}_Unit_{node}".Translate(value)}");
        return this;
    }

    public SEB ValueNoFormat(float value)
    {
        builder.AppendLine($"{prefix}_Unit_{node}".Translate(value));
        return this;
    }

    public SEB ValueNoFormat(string value)
    {
        builder.AppendLine($"{prefix}_Unit_{node}".Translate(value));
        return this;
    }

    public SEB Simple(string node, float value)
    {
        return Node(node)
            .Value(value);
    }

    public SEB Full(string node, float value, params object[] equationValues)
    {
        return Node(node)
            .Equation()
            .Calculus(equationValues)
            .Value(value);
    }

    public override string ToString()
    {
        return builder.ToString();
    }
}