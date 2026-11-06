using Microsoft.AspNetCore.Razor.TagHelpers;
using PosWebApp.Services;

namespace PosWebApp.TagHelpers;

/// <summary>
/// Razor Tag Helper that renders an optimised <c>&lt;img&gt;</c> element with:
/// <list type="bullet">
///   <item><description><c>loading="lazy"</c> for browser-native lazy loading (Requirement 12.7).</description></item>
///   <item><description><c>srcset</c> built from the standard size breakpoints (Requirement 12.10).</description></item>
///   <item><description><c>onerror</c> fallback to <c>/images/placeholder.png</c> (Requirement 12.8, 5.7).</description></item>
///   <item><description>An <c>alt</c> attribute for accessibility (WCAG 2.1 AA).</description></item>
/// </list>
///
/// Usage in a Razor Page / Layout:
/// <code>
///   &lt;optimized-img src="@item.ImagePath" alt="@item.Name" width="300" height="200" /&gt;
/// </code>
///
/// **Validates: Requirements 3.8, 3.9, 4.9, 5.7, 5.8, 12.7, 12.8, 12.10**
/// </summary>
[HtmlTargetElement("optimized-img")]
public sealed class ImageTagHelper : TagHelper
{
    // ── Standard srcset breakpoints (width × height) ──────────────────────
    // These cover 1×, 1.5×, and 2× DPI for the default requested size.

    private static readonly (int w, int h)[] DefaultSrcSetMultipliers =
    [
        (1, 1),   // 1×  — base size (set from tag attributes)
        (2, 2),   // 2×  — high-DPI / Retina
    ];

    // ── Dependencies ─────────────────────────────────────────────────────

    private readonly IImageOptimizerService _imageOptimizer;

    public ImageTagHelper(IImageOptimizerService imageOptimizer)
    {
        _imageOptimizer = imageOptimizer;
    }

    // ── Tag attributes ────────────────────────────────────────────────────

    /// <summary>Relative path / URL forwarded to the backend image handler.</summary>
    [HtmlAttributeName("src")]
    public string? Src { get; set; }

    /// <summary>Alternative text (required for accessibility).</summary>
    [HtmlAttributeName("alt")]
    public string? Alt { get; set; }

    /// <summary>Desired width in pixels (default: 300).</summary>
    [HtmlAttributeName("width")]
    public int Width { get; set; } = 300;

    /// <summary>Desired height in pixels (default: 200).</summary>
    [HtmlAttributeName("height")]
    public int Height { get; set; } = 200;

    /// <summary>Optional CSS class(es) applied to the rendered <c>&lt;img&gt;</c>.</summary>
    [HtmlAttributeName("css-class")]
    public string? CssClass { get; set; }

    // ── Rendering ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "img";
        output.TagMode = TagMode.SelfClosing;

        if (string.IsNullOrWhiteSpace(Src))
        {
            // Render the placeholder directly when no src is provided.
            output.Attributes.SetAttribute("src",     "/images/placeholder.png");
            output.Attributes.SetAttribute("alt",     Alt ?? string.Empty);
            output.Attributes.SetAttribute("loading", "lazy");
            if (!string.IsNullOrWhiteSpace(CssClass))
                output.Attributes.SetAttribute("class", CssClass);
            return;
        }

        var encodedSrc = Uri.EscapeDataString(Src);

        // Primary src: the /Images route with the default width & height.
        var primarySrc = $"/Images/{encodedSrc}?w={Width}&h={Height}";

        // srcset: base size + 2× retina.
        var srcSetSizes = DefaultSrcSetMultipliers
            .Select(m => (width: Width * m.w, height: Height * m.h));

        var srcSet = _imageOptimizer.BuildSrcSet(Src, srcSetSizes);

        output.Attributes.SetAttribute("src",      primarySrc);
        output.Attributes.SetAttribute("srcset",   srcSet);
        output.Attributes.SetAttribute("sizes",    $"(max-width: 600px) {Width}px, {Width}px");
        output.Attributes.SetAttribute("alt",      Alt ?? string.Empty);
        output.Attributes.SetAttribute("width",    Width);
        output.Attributes.SetAttribute("height",   Height);
        output.Attributes.SetAttribute("loading",  "lazy");
        output.Attributes.SetAttribute("decoding", "async");

        // Accessibility: onerror falls back to placeholder (Requirement 12.8).
        output.Attributes.SetAttribute(
            "onerror",
            "this.onerror=null;this.src='/images/placeholder.png'");

        if (!string.IsNullOrWhiteSpace(CssClass))
            output.Attributes.SetAttribute("class", CssClass);
    }
}
