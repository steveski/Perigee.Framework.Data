﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExampleRestApi.Controllers
{
    using System.Threading;
    using AutoMapper;
    using Example.Domain.Customers.Commands;
    using Example.Domain.Customers.Queries;
    using ExampleRestApi.Contract;
    using Perigee.Framework.Base.Transactions;

    [ApiController]
    [Route("[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly IProcessQueries _queries;
        private readonly IProcessCommands _commands;
        private readonly IMapper _mapper;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(IProcessQueries queries, IProcessCommands commands, ILogger<CustomerController> logger)
        {
            _queries = queries;
            _commands = commands;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> AddCustomer([FromBody] CustomerDto customer, CancellationToken cancellationToken)
        {
            var command = new CreateCustomerCommand
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                EmailAddress = customer.EmailAddress
            };

            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _commands.Execute(command, cancellationToken).ConfigureAwait(false);
            if (command.CreatedEntity == null) return BadRequest();

            var contract = new CustomerDto
            {
                Id = command.CreatedEntity.Id,
                FirstName = command.CreatedEntity.FirstName,
                LastName = command.CreatedEntity.LastName,
                EmailAddress = command.CreatedEntity.EmailAddress
            };

            return CreatedAtAction(
                "GetCustomer",
                new { id = command.CreatedEntity.Id },
                contract);
        }

        // GET: api/customer
        [HttpGet]
        public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
        {
            var customers = await _queries.Execute(new CustomersBy(), cancellationToken).ConfigureAwait(false);

            var contract = customers.Select(customer => new CustomerDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                EmailAddress = customer.EmailAddress
            });

            return Ok(contract);
        }

        // GET: api/customer/5
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetCustomer([FromRoute] int id, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var query = new CustomersBy { Id = id };
            var customers = await _queries.Execute(query, cancellationToken).ConfigureAwait(false);
            
            var customer = customers.FirstOrDefault();
            if (customer == null) return NotFound();

            var contract = new CustomerDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                EmailAddress = customer.EmailAddress
            };

            return Ok(contract);
        }



    }
}
