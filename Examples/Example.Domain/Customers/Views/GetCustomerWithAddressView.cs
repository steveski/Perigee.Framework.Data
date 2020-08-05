﻿namespace Example.Domain.Customers.Views
{
    using System;
    using System.Linq.Expressions;
    using Example.Domain.Addresses.Views;
    using Example.Entities;

    public class GetCustomerWithAddressView
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string EmailAddress { get; set; }

        public AddressView Address { get; set; }


        public static Expression<Func<Customer, GetCustomerWithAddressView>> Projector = cust => new GetCustomerWithAddressView
        {
            Id = cust.Id,
            FirstName = cust.FirstName,
            LastName = cust.LastName,
            EmailAddress = cust.EmailAddress,

            Address = cust.Address == null ? null : new AddressView
            {
                Id = cust.Address.Id,
                Street = cust.Address.Street,
                Suburb = cust.Address.Suburb,
                PostalCode = cust.Address.PostalCode,
                State = cust.Address.State,
                Country = cust.Address.Country

            }

        };


    }
}