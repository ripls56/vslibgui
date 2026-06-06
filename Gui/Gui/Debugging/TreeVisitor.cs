using System.Collections.Generic;
using Gui.Core.Framework;
using Gui.Core.Painting;
using Gui.Widgets.Framework;

namespace Gui.Debugging;

public static class TreeVisitor
{
    public static List<string> GetTreeDebugStrings(
        Element root
    )
    {
        var result = new List<string>();
        Visit(
            root,
            result,
            0
        );
        return result;
    }

    private static void Visit(
        Element element,
        List<string> result,
        int depth
    )
    {
        if (element == null)
        {
            return;
        }

        var indent = new string(
            ' ',
            depth * 2
        );

        var roInfo = " [No RO]";
        var flags = "---";

        if (element.RenderObject != null)
        {
            var ro = element.RenderObject;
            var hitMiss = "";
            if (ro is RenderRepaintBoundary)
            {
                var frameId = RenderObject.CurrentFrameId;
                if (ro.RepaintRecord.WasCacheHitThisFrame(frameId))
                {
                    hitMiss = " [HIT]";
                }
                else if (ro.RepaintRecord.WasDirtyPaintedThisFrame(frameId))
                {
                    hitMiss = " [MISS]";
                }
            }

            roInfo =
                $" [RO:{ro.GetType().Name} Pos=({ro.X:F1},{ro.Y:F1}) Size={ro.Size.X:F0}x{ro.Size.Y:F0}]{hitMiss}";

            var lFlag = ro.NeedsLayout ? "L" : ro.ChildNeedsLayout ? "l" : "-";
            var pFlag = ro.NeedsPaint ? "P" : ro.ChildNeedsPaint ? "p" : "-";
            var rFlag = element.IsDirty
                ? "R"
                : "-";
            flags = $"{lFlag}{pFlag}{rFlag}";
        }

        result.Add(
            $"{indent}{element.Widget.GetType().Name} ({element.GetType().Name}){roInfo} Flags:{flags}");

        // Visit children using the non-reflective mechanism
        element.VisitChildren(child => Visit(
                child,
                result,
                depth + 1
            )
        );
    }
}
