using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MiResiliencia.Models
{
    public class MiResilienciaContext : IdentityDbContext
    {

        public DbSet<Company> Companies { get; set; }
        public DbSet<Layer> Layers { get; set; }
        public DbSet<LayerStyle> LayerStyles { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<File> Files { get; set; }

        public DbSet<CompanyUser> CompanyUsers { get; set; }
        public DbSet<CompanyAdmin> CompanyAdmins { get; set; }

        public DbSet<Models.Objectparameter> Objektparameter { get; set; }
        public DbSet<MiResiliencia.Models.PostGISHatObjektparameter> PostGISHatObjektparameter { get; set; }
        public DbSet<MiResiliencia.Models.NatHazard> NatHazards { get; set; }
        public DbSet<MiResiliencia.Models.ObjectparameterPerProcess> ObjektparameterProProzess { get; set; }
        public DbSet<MiResiliencia.Models.ObjectClass> ObjektKlassen { get; set; }
        public DbSet<MiResiliencia.Models.MappedObject> MappedObjects { get; set; }
        public DbSet<MiResiliencia.Models.IKClasses> IntensitaetsKlassen { get; set; }

        public DbSet<MiResiliencia.Models.Intensity> Intensities { get; set; }
        public DbSet<MiResiliencia.Models.ProjectState> ProjectStates { get; set; }
        public DbSet<MiResiliencia.Models.ProtectionMeasure> ProtectionMeasurements { get; set; }
        public DbSet<MiResiliencia.Models.ResilienceFactor> ResilienceFactors { get; set; }
        public DbSet<MiResiliencia.Models.ResilienceValues> ResilienceValues { get; set; }
        public DbSet<MiResiliencia.Models.ResilienceWeight> ResilienceWeights { get; set; }

        public DbSet<MiResiliencia.Models.PrA> PrAs { get; set; }
        public DbSet<MiResiliencia.Models.WillingnessToPay> WillingnessToPays { get; set; }
        public DbSet<MiResiliencia.Models.DamageExtent> DamageExtents { get; set; }
        public DbSet<MiResiliencia.Models.Standard_PrA> StandardPrAs { get; set; }


        public DbSet<ObjectparameterHasResilienceFactor> ObjectparameterHasResilienceFactors { get; set; }
        public DbSet<MiResiliencia.Models.LayersEnabledByUser> LayersEnabledByUsers { get; set; }

        public DbSet<MiResiliencia.Models.Help> Helps { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                //.AddUserSecrets(Assembly.GetEntryAssembly(), true)      // <--- take user secrets from startup project
                .AddEnvironmentVariables()
                .Build();

            Console.WriteLine("Context log");
            Console.WriteLine(configuration.GetSection("Test").Value);
            Console.WriteLine(configuration.GetSection("Environment").GetChildren().ToList().ToString());

            string dbname = configuration.GetSection("Environment").GetSection("DB").Value;
            string host = configuration.GetSection("Environment").GetSection("DBHost").Value;

            bool splitBehavior = configuration.GetSection("Environment").GetSection("SplittingBehavior").Value.ToLower() == "true";

            optionsBuilder
                .UseLazyLoadingProxies()
                //.ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.DetachedLazyLoadingWarning))
                .UseNpgsql(
                    "Host=" + configuration.GetSection("Environment").GetSection("DBHost").Value + "; Database=" + configuration.GetSection("Environment").GetSection("DB").Value + ";Username=" + configuration.GetSection("Environment").GetSection("DBUser").Value + ";Password=" + configuration.GetSection("Environment").GetSection("DBPassword").Value + ";Include Error Detail=true;",
                    options =>
                    {
                        options.UseNetTopologySuite();
                        options.EnableRetryOnFailure();     //TODO: needed? https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency

                        if (splitBehavior)
                            options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        else
                            options.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                    }
                 )
                .EnableSensitiveDataLogging()
              ;
            base.OnConfiguring(optionsBuilder);
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("public");
            modelBuilder.HasPostgresExtension("postgis");

            modelBuilder.Entity<Project>().Property(g => g.geometry).HasColumnType("geometry(POLYGON, 3857)");
            modelBuilder.Entity<Intensity>().Property(g => g.geometry).HasColumnType("geometry(MULTIPOLYGON, 3857)");
            modelBuilder.Entity<DamageExtent>().Property(g => g.geometry).HasColumnType("geometry(geometry, 3857)");
            modelBuilder.Entity<MappedObject>().Property(g => g.geometry).HasColumnType("geometry(geometry, 3857)");
            modelBuilder.Entity<ProtectionMeasure>().Property(g => g.geometry).HasColumnType("geometry(MULTIPOLYGON, 3857)");



            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.SetTableName(entityType.DisplayName());

            }

            modelBuilder.Entity<CompanyAdmin>()
               .HasKey(pg => new { pg.AdminRefId, pg.UserRefId });
            modelBuilder.Entity<ApplicationUser>()
                .HasMany<CompanyAdmin>(s => s.IsCompanyAdminOf)
                .WithOne(c => c.User)
                .HasForeignKey(m => m.AdminRefId);
            modelBuilder.Entity<Company>()
                .HasMany<CompanyAdmin>(s => s.AdminUsers)
                .WithOne(c => c.Company)
                .HasForeignKey(m => m.UserRefId);

            modelBuilder.Entity<CompanyUser>()
                 .HasKey(pg => new { pg.CompanyUserRefId, pg.UserRefId });
            modelBuilder.Entity<ApplicationUser>()
                .HasMany<CompanyUser>(s => s.IsCompanyUserOf)
                .WithOne(c => c.User)
                .HasForeignKey(m => m.CompanyUserRefId);
            modelBuilder.Entity<Company>()
                .HasMany<CompanyUser>(s => s.CompanyUsers)
                .WithOne(c => c.Company)
                .HasForeignKey(m => m.UserRefId);

            modelBuilder.Entity<ObjectparameterHasResilienceFactor>()
                 .HasKey(pg => new { pg.Objectparameter_ID, pg.ResilienceFactor_ID });
            modelBuilder.Entity<ResilienceFactor>()
                .HasMany<ObjectparameterHasResilienceFactor>(s => s.ObjectparameterHasResilienceFactor)
                .WithOne(c => c.ResilienceFactor)
                .HasForeignKey(m => m.ResilienceFactor_ID);
            modelBuilder.Entity<Objectparameter>()
                .HasMany<ObjectparameterHasResilienceFactor>(s => s.ResilienceFactors)
                .WithOne(c => c.Objectparameter)
                .HasForeignKey(m => m.Objectparameter_ID);

            modelBuilder.Entity<LayersEnabledByUser>()
                 .HasKey(pg => new { pg.UserSettingsId, pg.LayersId });
            modelBuilder.Entity<Layer>()
                .HasMany<LayersEnabledByUser>(s => s.EnabledByUser)
                .WithOne(c => c.Layer)
                .HasForeignKey(m => m.LayersId);
            modelBuilder.Entity<UserSettings>()
                .HasMany<LayersEnabledByUser>(s => s.EnabledLayers)
                .WithOne(c => c.UserSettings)
                .HasForeignKey(m => m.UserSettingsId);

            modelBuilder.Entity<DamageExtent>()
                .HasOne(m=>m.Intensity)
                .WithMany(de => de.DamageExtents).HasForeignKey(m=>m.IntensityId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<DamageExtent>()
                .HasOne(m => m.MappedObject)
                .WithMany(de => de.DamageExtents).HasForeignKey(m => m.MappedObjectId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<PrA>()
                .HasOne(m => m.IKClasses)
                .WithMany(de => de.PrAs).HasForeignKey(m => m.IKClassesId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<PrA>()
                .HasOne(m => m.NatHazard)
                .WithMany(de => de.PrAs).HasForeignKey(m => m.NatHazardId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Intensity>().HasOne(m => m.NatHazard).WithMany(m => m.Intensities).HasForeignKey(m => m.NatHazardID);
            modelBuilder.Entity<Intensity>().HasOne(m => m.IKClasses).WithMany(m => m.Intensities).HasForeignKey(m => m.IKClassesID);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(m => m.MainCompany);

            modelBuilder.Entity<ProtectionMeasure>().HasOne(m => m.Project).WithOne(w => w.ProtectionMeasure).HasForeignKey<ProtectionMeasure>(q => q.ProjectID);

            modelBuilder.Entity<DamageExtent>().HasKey(de => new { de.MappedObjectId, de.IntensityId });
            modelBuilder.Entity<PrA>().HasKey(de => new { de.NatHazardId, de.IKClassesId, de.ProjectId });
            modelBuilder.Entity<Standard_PrA>().HasKey(de => new { de.NatHazardId, de.IKClassesId});



            // add first user in DB
            //var pwdHasher = new PasswordHasher<ApplicationUser>();
            //var user1 = new ApplicationUser
            //{
            //    FirstName = "Firstname",
            //    LastName = "Lastname",
            //    UserName = "email123@geotest.ch",
            //    Email = "email123@geotest.ch",
            //    NormalizedUserName= "email123@geotest.ch".ToUpper(),
            //    NormalizedEmail = "email123@geotest.ch".ToUpper(),
            //    PhoneNumber = "+41 77 888 99 00",
            //    Id = "4220a6f4-9044-4c0a-9430-8d0dec88a7bd"
            //};
            //user1.PasswordHash = pwdHasher.HashPassword(user1, "Password");
            //modelBuilder.Entity<ApplicationUser>().HasData(user1);


            base.OnModelCreating(modelBuilder);



        }
        
    }
}
