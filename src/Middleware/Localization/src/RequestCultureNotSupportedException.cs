// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Localization;

/// <summary>
/// Thrown only when no supported cultures could be determined
/// and <see cref="RequestLocalizationOptions.FallBackToDefaultCulture"/> is <c>false</c>.
/// </summary>
/// <remarks>
/// This exception indicates that the middleware was unable to match any of the
/// incoming culture values to the <see cref="RequestLocalizationOptions.SupportedCultures"/>
/// or <see cref="RequestLocalizationOptions.SupportedUICultures"/>, and default fallback
/// behavior has been disabled.
/// </remarks>
public class RequestCultureNotSupportedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestCultureNotSupportedException"/> class.
    /// </summary>
    public RequestCultureNotSupportedException()
        : base(Resources.Exception_RequestCultureNotSupported)
    {
    }
}
