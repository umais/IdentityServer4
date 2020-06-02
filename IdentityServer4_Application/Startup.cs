using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServer4_Application
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<IISOptions>(options => {
                options.AutomaticAuthentication = false;
            
            });
            services.AddMvc(option => option.EnableEndpointRouting = false);
 
             var config = new ConfigurationBuilder()
                           .SetBasePath(Directory.GetCurrentDirectory())
                           .AddJsonFile("appsettings.json", false)
                           .Build();
            string connectionString = config.GetSection("ConnectionString").Value;

            var migrationassembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddIdentityServer()
                .AddDeveloperSigningCredential()

        //             .AddInMemoryApiResources(Config.Apis)
        //.AddInMemoryClients(Config.Clients);
        
        .AddConfigurationStore(options =>
        {
            options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
                                         sql => sql.MigrationsAssembly(migrationassembly));


        })
         .AddOperationalStore(options =>
         {
             options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
                                          sql => sql.MigrationsAssembly(migrationassembly));


         }
         
        )
         
         
            ;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            InitializeIdentityServerData(app);

            app.UseIdentityServer();
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }

        private void InitializeIdentityServerData(IApplicationBuilder app)
        {
            using (var servicescope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                servicescope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = servicescope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                    context.Database.Migrate();

                //Seed The Data
                if(!context.Clients.Any())
                {
                    foreach(var client in Config.Clients)
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                }
                context.SaveChanges();

                if (!context.ApiResources.Any())
                {
                    foreach (var api in Config.Apis)
                    {
                        context.ApiResources.Add(api.ToEntity());
                    }
                }
                context.SaveChanges();

            }
        }
    }
}
