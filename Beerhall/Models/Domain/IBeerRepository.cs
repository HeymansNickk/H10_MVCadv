using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Beerhall.Models.Domain
{
    public interface IBeerRepository
    {
        IEnumerable<Beer> GetAll();
        Beer GetBy(int beerId);
    }
}
