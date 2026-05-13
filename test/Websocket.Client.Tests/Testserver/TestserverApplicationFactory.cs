using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Websocket.Client.Tests.TestServer
{
    public class TestServerApplicationFactory<TStartup> : WebApplicationFactory<TStartup>
        where TStartup : class
    {
        protected override IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureWebHostDefaults(builder =>
                {
                    builder
                        .UseContentRoot(AppContext.BaseDirectory)
                        .UseStartup<TStartup>();
                });

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(AppContext.BaseDirectory);
        }
    }
}
