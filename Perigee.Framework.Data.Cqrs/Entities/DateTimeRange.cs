﻿namespace Perigee.Cqrs.Base.Entities
{
    using System;

    public class DateTimeRange
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}