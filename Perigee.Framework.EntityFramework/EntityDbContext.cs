﻿namespace Perigee.Framework.EntityFramework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading;
    using System.Threading.Tasks;
    using EnsureThat;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using ModelCreation;
    using Perigee.Framework.Base.Database;
    using Perigee.Framework.Base.Entities;
    using Perigee.Framework.Services.User;
    using EntityState = Perigee.Framework.Base.Database.EntityState;
    using EfEntityState = Microsoft.EntityFrameworkCore.EntityState;


    public class EntityDbContext : DbContext, IWriteEntities
    {
        private readonly IUserService _userService;
        private readonly IAuditedEntityUpdater _auditedEntityUpdater;

        #region Construction

        public EntityDbContext(DbContextOptions<EntityDbContext> options, IUserService userService, IAuditedEntityUpdater auditedEntityUpdater) : base(options)
        {
            Ensure.Any.IsNotNull(userService, nameof(userService));
            Ensure.Any.IsNotNull(auditedEntityUpdater, nameof(auditedEntityUpdater));

            _userService = userService;
            _auditedEntityUpdater = auditedEntityUpdater;
            

        }


        #endregion


        private void SetAuditValues()
        {
            var addedEntities = ChangeTracker.Entries()
                .Where(x => x.State == EfEntityState.Added)
                .Select(x => x.Entity);

            var updatedEntities = ChangeTracker.Entries()
                .Where(x => x.State == EfEntityState.Modified)
                .Select(x => x.Entity);

            _auditedEntityUpdater.UpdateAuditFields(addedEntities, updatedEntities);

        }

        private void SetSoftDelete(EntityEntry entry)
        {
            entry.State = EfEntityState.Modified;
            ((ISoftDelete) entry.Entity).IsDeleted = true;
        }

        #region Model Creation

        public ICreateDbModel ModelCreator { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ModelCreator ??= new DefaultDbModelCreator();
            ModelCreator.Create(modelBuilder);
            base.OnModelCreating(modelBuilder);
        }

        #endregion

        #region Queries

        public IQueryable<TEntity> EagerLoad<TEntity>(IQueryable<TEntity> query,
            Expression<Func<TEntity, object>> expression) where TEntity : Entity
        {
            // Include will eager load data into the query
            if (query != null && expression != null) query = query.Include(expression);
            return query;
        }

        public new IQueryable<TEntity> Query<TEntity>() where TEntity : Entity
        {
            // AsNoTracking returns entities that are not attached to the DbContext
            return new EntitySet<TEntity>(Set<TEntity>().AsNoTracking(), this);
        }

        #endregion

        #region Commands

        public TEntity Get<TEntity>(object firstKeyValue, params object[] otherKeyValues) where TEntity : Entity
        {
            if (firstKeyValue == null) throw new ArgumentNullException(nameof(firstKeyValue));
            var keyValues = new List<object> {firstKeyValue};
            if (otherKeyValues != null) keyValues.AddRange(otherKeyValues);
            return Set<TEntity>().Find(keyValues.ToArray());
        }

        public Task<TEntity> GetAsync<TEntity>(object firstKeyValue, CancellationToken cancellationToken, params object[] otherKeyValues)
            where TEntity : Entity
        {
            if (firstKeyValue == null) throw new ArgumentNullException(nameof(firstKeyValue));
            var keyValues = new List<object> {firstKeyValue};
            if (otherKeyValues != null) keyValues.AddRange(otherKeyValues);
            return Set<TEntity>().FindAsync(keyValues.ToArray()).AsTask();
        }

        public IQueryable<TEntity> Get<TEntity>() where TEntity : Entity
        {
            return new EntitySet<TEntity>(Set<TEntity>(), this);
        }

        public void Create<TEntity>(TEntity entity) where TEntity : Entity
        {
            if (Entry(entity).State == EfEntityState.Detached) Set<TEntity>().Add(entity);
        }

        public new void Update<TEntity>(TEntity entity) where TEntity : Entity
        {
            var entry = Entry(entity);
            if (entry.State != EfEntityState.Added)
                entry.State = EfEntityState.Modified;
        }

        public void Delete<TEntity>(TEntity entity) where TEntity : Entity
        {
            if (Entry(entity).State != EfEntityState.Deleted)
                Set<TEntity>().Remove(entity);
        }

        public void Reload<TEntity>(TEntity entity) where TEntity : Entity
        {
            Entry(entity).Reload();
        }

        public Task ReloadAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : Entity
        {
            return Entry(entity).ReloadAsync(cancellationToken);
        }

        public new void Attach<TEntity>(TEntity entity) where TEntity : Entity
        {
            if (Entry(entity).State == EfEntityState.Detached)
                Set<TEntity>().Attach(entity);
        }

        public EntityState GetState<TEntity>(TEntity entity) where TEntity : Entity
        {
            var internalEntityState = MapToInternal(Entry(entity).State);
            return internalEntityState;
        }
        
        public void SetEntityState<TEntity>(TEntity entity, EntityState state) where TEntity : Entity
        {
            Entry(entity).State = MapToEf(state);
        }

        private EfEntityState MapToEf(EntityState state)
        {
            var efEntityState = (EfEntityState)Enum.Parse(typeof(EfEntityState), state.ToString());
            return efEntityState;
        }

        private EntityState MapToInternal(EfEntityState state)
        {
            var internalState = (EntityState)Enum.Parse(typeof(EntityState), state.ToString());
            return internalState;
        }

        #endregion

        #region UnitOfWork

        public void DiscardChanges()
        {
            foreach (var entry in ChangeTracker.Entries().Where(x => x != null))
                switch (entry.State)
                {
                    case EfEntityState.Added:
                        entry.State = EfEntityState.Detached;
                        break;
                    case EfEntityState.Modified:
                        entry.State = EfEntityState.Unchanged;
                        break;
                    case EfEntityState.Deleted:
                        entry.Reload();
                        break;
                }
        }

        public Task DiscardChangesAsync(CancellationToken cancellationToken)
        {
            var reloadTasks = new List<Task>();
            foreach (var entry in ChangeTracker.Entries().Where(x => x != null))
                switch (entry.State)
                {
                    case EfEntityState.Added:
                        entry.State = EfEntityState.Detached;
                        break;
                    case EfEntityState.Modified:
                        entry.State = EfEntityState.Unchanged;
                        break;
                    case EfEntityState.Deleted:
                        reloadTasks.Add(entry.ReloadAsync(cancellationToken));
                        break;
                }

            return Task.WhenAll(reloadTasks);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            foreach (var entry in ChangeTracker.Entries().Where(x => x.State == EfEntityState.Deleted))
            {
                if (entry.State == EfEntityState.Deleted && entry.Entity is ISoftDelete) SetSoftDelete(entry);

            }

            SetAuditValues();

            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            return SaveChangesAsync(CancellationToken.None).Result;
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        #endregion
    }
}
