﻿namespace Example.Domain.Customers.Commands
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Entities;
    using Perigee.Framework.Base.Database;
    using Perigee.Framework.Base.Transactions;

    public class CreateCustomerCommandWithTransientDbContext : BaseCommand, IDefineCommand
    {
        //public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }

        public Customer Customer { get; internal set; }

    }

    public class HandleCreateCustomerCommandWithTransientDbContext : IHandleCommand<CreateCustomerCommandWithTransientDbContext>
    {
        private readonly ITransientContext _db;

        public HandleCreateCustomerCommandWithTransientDbContext(ITransientContext db)
        {
            _db = db;
        }

        public async Task Handle(CreateCustomerCommandWithTransientDbContext command, CancellationToken cancellationToken)
        {
            var cust = new Customer
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                EmailAddress = command.EmailAddress
            };

            _db.Create(cust);
            command.Customer = cust;
            
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    //public class SomeService
    //{
    //    private BlockingCollection<int> messageQueue = new BlockingCollection<int>();

    //    public void ProcessIncommingMessages(CancellationToken ct)
    //    {
    //        while (true)
    //        {
    //            var message = messageQueue.Take(ct);
    //            Console.Write(message);

    //        }
    //    }

    //    public void SubmitMessageForProcessing(int message)
    //    {
    //        messageQueue.Add(message);
    //    }

    //}
}