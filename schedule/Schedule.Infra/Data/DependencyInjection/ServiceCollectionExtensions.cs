using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Schedule.Core.Entities;
using Schedule.Core.Interfaces;
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

            services.AddScoped<Context.SqlConnectionFactory>();

            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IAppointmentRepository, AppointmentRepository>();

            services.AddScoped<IHealthcareRepository, HealthcareRepository>();

            services.AddScoped<IPatientRepository, PatientRepository>();
            return services;
        }
    }
}
