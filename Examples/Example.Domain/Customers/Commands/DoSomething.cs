﻿namespace Example.Domain.Customers.Commands
{
    using System.Threading;
    using System.Threading.Tasks;
    using Entities;
    using Perigee.Framework.Base.Database;
    using Perigee.Framework.Base.Transactions;

    public class DoSomethingCommand : BaseCreateEntityCommand<Customer>, IDefineCommand
    {
        public string DoodadWhatsit { get; set; }
    }

    public class DoSomethingHandler : IHandleCommand<DoSomethingCommand>
    {
        private readonly IWriteEntities _db;

        public DoSomethingHandler(IWriteEntities db)
        {
            _db = db;
        }

        public Task Handle(DoSomethingCommand command, CancellationToken cancellationToken)
        {
            //_db.Create(new Customer {FirstName = command.DoodadWhatsit});

            return Task.CompletedTask;
        }
    }
}