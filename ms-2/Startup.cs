using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MassTransit;
using GreenPipes;

namespace ms_2
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMassTransit(x =>
            {
                x.AddConsumer<OrderConsumer>();

                x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    cfg.UseHealthCheck(provider);

                    cfg.Host("rabbitmq://192.168.1.8");

                    cfg.ReceiveEndpoint("order-queue", ep =>
                    {
                        ep.PrefetchCount = 16;
                        ep.UseMessageRetry(r => r.Interval(2, 100));

                        ep.ConfigureConsumer<OrderConsumer>(provider);
                    });

                }));
            });

            services.AddMassTransitHostedService();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
