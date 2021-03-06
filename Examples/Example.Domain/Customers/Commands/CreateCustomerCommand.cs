﻿namespace Example.Domain.Customers.Commands
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using EnsureThat;
    using Entities;
    using Perigee.Framework.Base.Database;
    using Perigee.Framework.Base.Services;
    using Perigee.Framework.Base.Transactions;

    public class CreateCustomerCommand : BaseCreateEntityCommand<Customer>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public int? AddressId { get; set; }
    }

    public class HandleCreateCustomerCommand : IHandleCommand<CreateCustomerCommand>
    {
        private readonly IWriteEntities _db;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public HandleCreateCustomerCommand(IWriteEntities db, IUserService userService, IMapper mapper)
        {
            Ensure.Any.IsNotNull(db, nameof(db));
            Ensure.Any.IsNotNull(userService, nameof(userService));

            _db = db;
            _userService = userService;
            _mapper = mapper;
        }

        public Task Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
        {
            var cust = _mapper.Map<CreateCustomerCommand, Customer>(command);
            cust.ManagedBy = _userService.ClaimsIdentity.Name;

            _db.Create(cust);
            command.CreatedEntity = cust;


            return Task.CompletedTask;
        }
    }

}