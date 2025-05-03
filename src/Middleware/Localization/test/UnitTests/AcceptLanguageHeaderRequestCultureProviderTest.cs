// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Localization;

public class AcceptLanguageHeaderRequestCultureProviderTest
{
    [Fact]
    public async Task GetFallbackLanguage_ReturnsFirstNonNullCultureFromSupportedCultureList()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalization(new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en-US"),
                        SupportedCultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-SA"),
                                new CultureInfo("en-US")
                        }
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("ar-SA", requestCulture.Culture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("jp,ar-SA,en-US");
            var count = client.DefaultRequestHeaders.AcceptLanguage.Count;
            var response = await client.GetAsync(string.Empty);
            Assert.Equal(3, count);
        }
    }

    [Fact]
    public async Task GetFallbackLanguage_ReturnsFromSupportedCulture_AcceptLanguageListContainsSupportedCultures()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalization(new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("fr-FR"),
                        SupportedCultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-SA"),
                                new CultureInfo("en-US")
                        }
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("ar-SA", requestCulture.Culture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,ar-SA,en-US");
            var count = client.DefaultRequestHeaders.AcceptLanguage.Count;
            var response = await client.GetAsync(string.Empty);
        }
    }

    [Fact]
    public async Task GetFallbackLanguage_ReturnsDefault_AcceptLanguageListDoesnotContainSupportedCultures()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalization(new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("fr-FR"),
                        SupportedCultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-SA"),
                                new CultureInfo("af-ZA")
                        }
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;
                        Assert.Equal("fr-FR", requestCulture.Culture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,ar-MA,en-US");
            var count = client.DefaultRequestHeaders.AcceptLanguage.Count;
            var response = await client.GetAsync(string.Empty);
            Assert.Equal(3, count);
        }
    }

    [Fact]
    public async Task OmitDefaultRequestCultureShouldNotThrowNullReferenceException_And_ShouldGetTheRightCulture()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRequestLocalization(new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en-US"),
                        SupportedCultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-YE")
                        },
                        SupportedUICultures = new List<CultureInfo>
                        {
                                new CultureInfo("ar-YE")
                        }
                    });
                    app.Run(context =>
                    {
                        var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                        var requestCulture = requestCultureFeature.RequestCulture;

                        Assert.Equal("ar-YE", requestCulture.Culture.Name);
                        Assert.Equal("ar-YE", requestCulture.UICulture.Name);
                        return Task.FromResult(0);
                    });
                });
            }).Build();

        await host.StartAsync();

        using (var server = host.GetTestServer())
        {
            var client = server.CreateClient();
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,ar-YE,en-US");
            var count = client.DefaultRequestHeaders.AcceptLanguage.Count;
            var response = await client.GetAsync(string.Empty);
            Assert.Equal(3, count);
        }
    }

    [Fact]
    public async Task GetFallbackLanguage_StrictMode_SupportedHeader_Works()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.UseRequestLocalization(new RequestLocalizationOptions
                        {
                            DefaultRequestCulture = new RequestCulture("en-US"),
                            SupportedCultures = new List<CultureInfo>
                            {
                                    new CultureInfo("fr-FR")
                            },
                            SupportedUICultures = new List<CultureInfo>
                            {
                                    new CultureInfo("fr-FR")
                            },
                            RequestCultureProviders = new List<IRequestCultureProvider>
                            {
                                    new AcceptLanguageHeaderRequestCultureProvider()
                            },
                            FallBackToDefaultCulture = false
                        });
                        app.Run(context =>
                        {
                            var requestCulture = context.Features
                                .Get<IRequestCultureFeature>()!
                                .RequestCulture;
                            Assert.Equal("fr-FR", requestCulture.Culture.Name);
                            Assert.Equal("fr-FR", requestCulture.UICulture.Name);
                            return Task.CompletedTask;
                        });
                    });
            })
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr-FR");

        // Act: should not throw
        var response = await client.GetAsync(string.Empty);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFallbackLanguage_StrictMode_UnsupportedHeader_Throws()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.UseRequestLocalization(new RequestLocalizationOptions
                        {
                            DefaultRequestCulture = new RequestCulture("en-US"),
                            SupportedCultures = new List<CultureInfo>
                            {
                                    new CultureInfo("ar-YE")
                            },
                            SupportedUICultures = new List<CultureInfo>
                            {
                                    new CultureInfo("ar-YE")
                            },
                            FallBackToDefaultCulture = false
                        });
                        app.Run(context => Task.CompletedTask);
                    });
            })
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("unsupported");

        // Act & Assert: strict mode should throw
        await Assert.ThrowsAsync<RequestCultureNotSupportedException>(
            () => client.GetAsync(string.Empty));
    }
}
