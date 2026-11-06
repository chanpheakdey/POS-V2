namespace PosWebApp.Resources;

/// <summary>
/// Marker class for IStringLocalizer&lt;SharedResource&gt; injection.
/// The localization framework uses this type to locate the correct .resx files
/// (SharedResource.en-US.resx / SharedResource.km-KH.resx) under the Resources/ folder.
/// Requirement 13.1, 13.9
/// </summary>
public sealed class SharedResource
{
    // Intentionally empty — used only as the T in IStringLocalizer<T>.
}
