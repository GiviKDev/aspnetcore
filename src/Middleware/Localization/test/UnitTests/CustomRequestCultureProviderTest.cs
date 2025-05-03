// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Localization;

public class CustomRequestCultureProviderTest
{
    [Fact]
    public async Task CustomRequestCultureProviderThatGetsCultureInfoFromUrl()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    var options = new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en-US"),
                        SupportedCultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar")
                        },
                        SupportedUICultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar")
                        }
                    };
                    options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(context =>
                    {
                        var culture = GetCultureInfoFromUrl(context, options.SupportedCultures);
                        var requestCulture = new ProviderCultureResult(culture);
                        return Task.FromResult(requestCulture);
                    }));
                    app.UseRequestLocalization(options);
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("ar", requestCulture.Culture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            var response = await client.GetAsync("/ar/page");
        }
    }

    private static string GetCultureInfoFromUrl(HttpContext context, IList<CultureInfo> supportedCultures)
    {
        var currentCulture = "en";
        var segments = context.Request.Path.Value.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 1 && segments[0].Length == 2)
        {
            currentCulture = segments[0];
        }

        return currentCulture;
    }

    [Fact]
    public async Task CustomRequestCultureProvider_StrictMode_SupportedCulture_Works()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        var options = new RequestLocalizationOptions
                        {
                            DefaultRequestCulture = new RequestCulture("en-US"),
                            SupportedCultures = new List<CultureInfo> { new CultureInfo("ar") },
                            SupportedUICultures = new List<CultureInfo> { new CultureInfo("ar") },
                            FallBackToDefaultCulture = false
                        };
                        options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(context =>
                        {
                            var culture = GetCultureInfoFromUrl(context, options.SupportedCultures);
                            return Task.FromResult(new ProviderCultureResult(culture, culture));
                        }));
                        app.UseRequestLocalization(options);
                        app.Run(context =>
                        {
                            var rc = context.Features.Get<IRequestCultureFeature>()!.RequestCulture;
                            Assert.Equal("ar", rc.Culture.Name);
                            Assert.Equal("ar", rc.UICulture.Name);
                            return Task.CompletedTask;
                        });
                    });
            })
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();
        var client = server.CreateClient();

        // Act: using supported "ar" segment
        var response = await client.GetAsync("/ar/page");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CustomRequestCultureProvider_StrictMode_UnsupportedCulture_Throws()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        var options = new RequestLocalizationOptions
                        {
                            DefaultRequestCulture = new RequestCulture("en-US"),
                            SupportedCultures = new List<CultureInfo> { new CultureInfo("ar") },
                            SupportedUICultures = new List<CultureInfo> { new CultureInfo("ar") },
                            FallBackToDefaultCulture = false
                        };
                        options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(context =>
                        {
                            var culture = GetCultureInfoFromUrl(context, options.SupportedCultures);
                            return Task.FromResult(new ProviderCultureResult(culture, culture));
                        }));
                        app.UseRequestLocalization(options);
                        app.Run(context => Task.CompletedTask);
                    });
            })
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();
        var client = server.CreateClient();

        // Act & Assert: using unsupported "xx" segment
        await Assert.ThrowsAsync<RequestCultureNotSupportedException>(
            () => client.GetAsync("/xx/page"));
    }
}
