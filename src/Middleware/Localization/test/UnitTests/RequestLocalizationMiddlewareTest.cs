// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Extensions.Localization;

public class RequestLocalizationMiddlewareTest
{
    [Theory]
    [InlineData("zh-Hans-CN", "zh")]
    [InlineData("zh-Hans", "zh")]
    [InlineData("zh-CN", "zh")]
    [InlineData("zh-Hant-TW", "zh")]
    [InlineData("zh-Hant", "zh")]
    [InlineData("zh-TW", "zh")]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-Hans-CN", "zh-Hans")]
    [InlineData("zh-Hant-TW", "zh-Hant")]
    [InlineData("zh-TW", "zh-Hant")]
    public async Task RequestLocalizationMiddleware_ShouldFallBackToParentCultures_RegradlessOfHyphenSeparatorCheck(string requestedCulture, string parentCulture)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    var supportedCultures = new[] { "ar", "en", parentCulture };

                    app.UseRequestLocalization(options =>
                    {
                        options.AddSupportedCultures(supportedCultures)
                            .AddSupportedUICultures(supportedCultures)
                            .AddInitialRequestCultureProvider(new CookieRequestCultureProvider
                            {
                                CookieName = "Preferences"
                            });
                    });

                    app.Run(async context =>
                    {
                        var requestCulture = context.Features.Get<IRequestCultureFeature>();

                        Assert.Equal(parentCulture, requestCulture.RequestCulture.Culture.Name);
                        Assert.Equal(parentCulture, requestCulture.RequestCulture.UICulture.Name);

                        await Task.CompletedTask;
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();

            client.DefaultRequestHeaders.Add("Cookie", new CookieHeaderValue("Preferences", $"c={requestedCulture}|uic={requestedCulture}").ToString());

            var response = await client.GetAsync(string.Empty);

            response.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task RequestLocalizationMiddleware_StrictMode_SupportedCookie_Works()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.UseRequestLocalization(options =>
                        {
                            // set up strict-mode: no fallback to default
                            options.FallBackToDefaultCulture = false;

                            // support only ar-SA
                            var supported = new[] { "ar-SA" };
                            options.AddSupportedCultures(supported)
                                   .AddSupportedUICultures(supported)
                                   .AddInitialRequestCultureProvider(new CookieRequestCultureProvider
                                   {
                                       CookieName = "Preferences"
                                   });
                        });

                        app.Run(context =>
                        {
                            var rc = context.Features.Get<IRequestCultureFeature>()!.RequestCulture;
                            Assert.Equal("ar-SA", rc.Culture.Name);
                            Assert.Equal("ar-SA", rc.UICulture.Name);
                            return Task.CompletedTask;
                        });
                    });
            })
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();
        var client = server.CreateClient();

        // client has exactly the supported culture
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("ar-SA"));
        client.DefaultRequestHeaders.Add(HeaderNames.Cookie,
            new CookieHeaderValue("Preferences", cookieValue).ToString());

        // Act: should succeed
        var response = await client.GetAsync(string.Empty);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task RequestLocalizationMiddleware_StrictMode_UnsupportedCookie_Throws()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.UseRequestLocalization(options =>
                        {
                            options.FallBackToDefaultCulture = false;

                            // support only ar-SA
                            var supported = new[] { "ar-SA" };
                            options.AddSupportedCultures(supported)
                                   .AddSupportedUICultures(supported)
                                   .AddInitialRequestCultureProvider(new CookieRequestCultureProvider
                                   {
                                       CookieName = "Preferences"
                                   });
                        });

                        app.Run(context => Task.CompletedTask);
                    });
            })
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();
        var client = server.CreateClient();

        // client has an unsupported culture
        client.DefaultRequestHeaders.Add(HeaderNames.Cookie,
            new CookieHeaderValue("Preferences", "c=xx|uic=yy").ToString());

        // Act & Assert: strict mode should throw
        await Assert.ThrowsAsync<RequestCultureNotSupportedException>(
            () => client.GetAsync(string.Empty));
    }
}
