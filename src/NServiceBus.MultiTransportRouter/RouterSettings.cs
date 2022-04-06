﻿using System.Collections.Generic;

public class RouterSettings
{
    public List<Transport> Transports { get; set; }

    public class Transport
    {
        public string Name { get; set; }
        public int Concurrency { get; set; }
    }
}