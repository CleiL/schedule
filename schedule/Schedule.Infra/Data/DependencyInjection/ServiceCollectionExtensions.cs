using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Schedule.Application.Interfaces;
using Schedule.Application.Services;
using Schedule.Core.Entities;
using Schedule.Core.Interfaces;
using Schedule.Infra.Data.Context;
using Schedule.Infra.Data.DependencyInjection.Configuration.Uow;
using Schedule.Infra.Repositories;

namespace Schedule.Infra.Data.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfraData(this IServiceCollection services, IConfiguration cfg)
        {
            services.Configure<DbOptions>(cfg.GetSection("ConnectionStrings"));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();

            services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

            services.AddSingleton<ISchemaInitializer, SchemaInitializer>();

            services.AddScoped<Context.SqlConnectionFactory>();

            services.AddScoped<IAuthService, AuthService>();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();

            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<IAppointmentService, AppointmentService>();

            services.AddScoped<IHealthcareRepository, HealthcareRepository>();
            services.AddScoped<IHealthcareService, HealthcareService>();

            services.AddScoped<IPatientRepository, PatientRepository>();
            services.AddScoped<IPatientService, PatientService>();

            return services;
        }
    }
}
