using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiceMvc.Data
{
    public interface IDbInitializer
    {
        void Initialize();
    }
}
