﻿namespace Perigee.Framework.EntityFramework
{
    using Autofac;
    using Microsoft.EntityFrameworkCore;
    using ModelCreation;
    using Perigee.Framework.Base;
    using Perigee.Framework.Base.Database;
    using Perigee.Framework.Services.User;

    /// <summary>
    /// Registers the EntityDbContext and access interfaces IUnitOfWork, IReadEntities and IWriteEntities.
    /// <remarks>This should be registered by instance rather than type.
    /// <example>
    /// var dbContextOptions = new DbContextOptions<EntityDbContext>(...);
    /// builder.RegisterModule(new EntityDbContext(dbContextOptions));
    /// </example></remarks>
    /// </summary>
    public class EntityFrameworkModule : Module
    {

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DefaultAuditedEntityUpdater>().As<IAuditedEntityUpdater>().InstancePerLifetimeScope();

            // If the calling code hasn't already registered a custom 
            // ICreateDbModel then register the internal DefaultDbModelCreator
            builder.RegisterType<DefaultDbModelCreator>()
                .IfNotRegistered(typeof(ICreateDbModel))
                .As<ICreateDbModel>();

            builder.Register(c =>
                    new TransientEntityDbContext(
                        c.Resolve<DbContextOptions<EntityDbContext>>(),
                        c.Resolve<IRecordAuthority>(),
                        c.Resolve<IAuditedEntityUpdater>())
                    {
                        ModelCreator = c.Resolve<ICreateDbModel>()
                    })
                //.AsImplementedInterfaces() // Doing this will reimplement IWriteEntities, IReadEntites and IUnitOfWork causing a clash...so DON'T!
                .As<ITransientContext>()
                .InstancePerDependency();

            // Expecting IUnitOfWork, IReadEntities and IWriteEntities to be registered with this call
            builder.Register(c =>
                    new EntityDbContext(
                        c.Resolve<DbContextOptions<EntityDbContext>>(),
                        c.Resolve<IRecordAuthority>(),
                        c.Resolve<IAuditedEntityUpdater>())
                    {
                        ModelCreator = c.Resolve<ICreateDbModel>()
                    })
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();



        }
    }
}