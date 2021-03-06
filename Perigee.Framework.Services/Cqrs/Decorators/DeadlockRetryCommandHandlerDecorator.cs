﻿namespace Perigee.Framework.Services.Cqrs.Decorators
{
    using System;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using Perigee.Framework.Base.Database;
    using Perigee.Framework.Base.Transactions;

    public class DeadlockRetryCommandHandlerDecorator<TCommand> : IHandleCommand<TCommand>
        where TCommand : IDefineCommand
    {
        private readonly IUnitOfWork _db;
        private readonly IHandleCommand<TCommand> _decorated;

        public DeadlockRetryCommandHandlerDecorator(IHandleCommand<TCommand> decorated, IUnitOfWork db)
        {
            _decorated = decorated;
            _db = db;
        }

        public async Task Handle(TCommand command, CancellationToken cancellationToken)
        {
            await HandleWithRetry(command, 5, cancellationToken).ConfigureAwait(false);
        }

        private async Task HandleWithRetry(TCommand command, int retries, CancellationToken cancellationToken)
        {
            try
            {
                await _decorated.Handle(command, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (retries <= 0 || !IsDeadlockException(ex))
                    throw;

                Thread.Sleep(300);
                await HandleWithRetry(command, retries - 1, cancellationToken).ConfigureAwait(false);
            }
        }

        private static bool IsDeadlockException(Exception ex)
        {
            return ex is DbException
                   && ex.Message.Contains("deadlock", StringComparison.InvariantCultureIgnoreCase)
                ? true
                : ex.InnerException == null
                    ? false
                    : IsDeadlockException(ex.InnerException);
        }
    }
}