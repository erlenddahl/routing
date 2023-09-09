using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RoadNetworkRouting.Config;
using RoadNetworkRouting.Service;
using System.IO;
using System.Text.Json.Serialization;

namespace RoutingApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var networkFile = "";
            SkeletonConfig skeletonConfig = null;
            if (Directory.Exists(@"data\networks\road\2023-01-09"))
            {
                networkFile = @"data\networks\road\2023-01-09\network.bin";
                //networkFile = @"data\networks\road\2023-01-09\network_skeleton.bin";
                //networkFile = @"data\networks\road\2023-01-09\network_three_islands.bin";
                //skeletonConfig = new SkeletonConfig() { LinkDataDirectory = @"data\networks\road\2023-01-09\geometries" };
            }
            else if (configuration != null)
            {
                networkFile = configuration.GetValue<string>("RoadNetworkLocation");
                skeletonConfig = new SkeletonConfig() { LinkDataDirectory = configuration.GetValue<string>("RoadNetworkLinkLocation") };
            }

            FullRoutingService.Initialize(networkFile, skeletonConfig);
        }

        public IConfiguration Configuration { get; }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "RoutingAPI", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RoutingAPI v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(o => o.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
