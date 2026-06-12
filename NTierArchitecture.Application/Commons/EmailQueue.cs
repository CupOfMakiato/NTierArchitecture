using NTierArchitecture.Application.DTOs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NTierArchitecture.Application.Commons
{
    public static class EmailQueue
    {
        public static readonly Channel<EmailDTO> Channel =
            System.Threading.Channels.Channel.CreateUnbounded<EmailDTO>();
    }
}
